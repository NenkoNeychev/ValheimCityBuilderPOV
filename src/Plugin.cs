using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilderPOV
{
    [BepInPlugin(Id, "CityBuilder POV", "0.3.0")]
    [BepInDependency("rusty.valheimcitybuilder", "1.2.2")]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string Id = "local.citybuilder.pov";
        internal static Plugin I;
        internal Bridge B;
        internal readonly InteractionState State = new InteractionState();
        internal object Mode;
        internal bool ToolScope;
        bool ready, faulted;
        Harmony harmony;
        ConfigEntry<Key> menuKey, inspectKey, selectKey, orderKey;
        ConfigEntry<float> reach;
        ConfigEntry<bool> hints;
        ConfigEntry<bool> nativeTools, clickBuildings;
        ConfigEntry<float> rotationStep, fineRotationStep;
        Player lastPlayer;
        ZNet lastNet;
        int tab;
        Vector2 scroll;
        string notice = "";
        float noticeUntil;
        readonly List<MethodInfo> uiChecks = new List<MethodInfo>();
        internal bool Live => ready && !faulted && Mode != null && Player.m_localPlayer != null
            && !Player.m_localPlayer.IsDead() && !B.Active;
        internal bool Blocking => Live && State.BlocksActions;
        internal bool MenuOpen => Live && State.MenuOpen;

        void Awake()
        {
            I = this;
            menuKey = Config.Bind("Controls", "BuildMenu", Key.F6, "Open settlement management, research, world, fleet and storage.");
            inspectKey = Config.Bind("Controls", "ManageBuilding", Key.H, "Manage the owned building under your crosshair.");
            selectKey = Config.Bind("Controls", "SelectSquad", Key.J, "Select your aimed soldier's squad. Shift adds another squad.");
            orderKey = Config.Bind("Controls", "OrderSquad", Key.K, "Order selected soldiers to the aimed ground or hostile target.");
            reach = Config.Bind("Placement", "Reach", 40f, new ConfigDescription("Maximum aim distance from your Viking, in metres.", new AcceptableValueRange<float>(5f, 80f)));
            hints = Config.Bind("Interface", "ShowHints", true, "Show controls and placement status.");
            nativeTools = Config.Bind("Controls", "NativeHammerAndHoe", true, "Add settlement plans to the normal hammer and hoe build menus.");
            clickBuildings = Config.Bind("Controls", "ClickBuildings", true, "Left-click an owned settlement building to manage it when not placing a piece. Right-click closes menus.");
            rotationStep = Config.Bind("Placement", "RotationStep", 22.5f, new ConfigDescription("Mouse-wheel rotation step in degrees.", new AcceptableValueRange<float>(1f, 90f)));
            fineRotationStep = Config.Bind("Placement", "FineRotationStep", 5f, new ConfigDescription("Hold Shift for this rotation step.", new AcceptableValueRange<float>(1f, 45f)));
            try
            {
                B = new Bridge();
                foreach (var spec in new[] { "InventoryGui:IsVisible", "Menu:IsVisible", "Minimap:IsOpen", "TextInput:IsVisible", "StoreGui:IsVisible", "Console:IsVisible", "Hud:IsPieceSelectionVisible", "Hud:InRadial", "UnifiedPopup:IsVisible", "ZNet:IsPasswordDialogShowing" })
                {
                    var parts = spec.Split(':');
                    var method = AccessTools.Method(typeof(Player).Assembly.GetType(parts[0], true), parts[1], Type.EmptyTypes);
                    if (method == null || !method.IsStatic || method.ReturnType != typeof(bool)) throw new MissingMethodException(spec);
                    uiChecks.Add(method);
                }
                harmony = new Harmony(Id);
                Patch(B.Method("AnnoMode", "Update"), null, nameof(AfterTownUpdate));
                Patch(B.Method("AnnoMode", "SetActive"), nameof(BeforeCityToggle));
                Patch(B.Method("AnnoMode", "MouseGround"), nameof(AimGround));
                Patch(B.Method("AnnoMode", "MouseOverUI"), nameof(NotOverUi));
                Patch(B.Method("In", "get_LeftDown"), nameof(ToolLeft));
                Patch(B.Method("In", "get_RightDown"), nameof(ToolRight));
                Patch(AccessTools.Method(typeof(Player), "SetControls"), nameof(FilterActions));
                Patch(AccessTools.Method(typeof(Player), "UpdatePlacement"), nameof(FilterHammer));
                Patch(AccessTools.Method(typeof(PlayerController), "TakeInput"), nameof(FilterInput));
                Patch(AccessTools.Method(typeof(GameCamera), "UpdateCamera"), nameof(FreezeMenuCamera));
                Patch(AccessTools.Method(typeof(GameCamera), "UpdateMouseCapture"), nameof(MenuCursor));
                InstallNativeHooks();
                ready = true;
                Logger.LogInfo("CityBuilder POV 0.3.0 ready for CityBuilder 1.2.2. Hammer/hoe integration enabled; F6 opens town menus.");
            }
            catch (Exception ex) { Fail(ex); }
        }

        void Patch(MethodInfo original, string prefix = null, string postfix = null)
        {
            if (original == null) throw new MissingMethodException("Required game method not found");
            harmony.Patch(original, prefix == null ? null : new HarmonyMethod(typeof(Plugin), prefix),
                postfix == null ? null : new HarmonyMethod(typeof(Plugin), postfix));
        }
        internal static bool Pressed(Key key) => Keyboard.current != null && key != Key.None && Keyboard.current[key].wasPressedThisFrame;
        static bool LeftHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        static bool RightHeld => Mouse.current != null && Mouse.current.rightButton.isPressed;
        bool ExternalUi()
        {
            if (!Application.isFocused) return true;
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            foreach (var method in uiChecks) if ((bool)method.Invoke(null, null)) return true;
            return false;
        }
        internal void Tell(string text) { notice = text; noticeUntil = Time.unscaledTime + 5f; }
        void Fail(Exception ex)
        {
            Logger.LogError("CityBuilder POV disabled safely: " + ex);
            try { Reset(true); } catch (Exception cleanup) { Logger.LogWarning(cleanup); }
            faulted = true;
            try { RemoveCatalog(); } catch (Exception cleanup) { Logger.LogWarning(cleanup); }
            harmony?.UnpatchSelf();
        }
        void Reset(bool clearSelection)
        {
            try
            {
                if (Mode is UnityEngine.Object obj && obj != null && B != null && !B.Active)
                {
                    if (State.ToolActive) B.Call("AnnoMode", "RemoteEndTool", Mode);
                    if (clearSelection && (State.ToolActive || State.MenuOpen)) B.Call("AnnoMode", "ClearSelection", Mode);
                    B.Call("AnnoMode", "RemoteWorldUi", Mode, "close", 0);
                }
            }
            finally
            {
                bool cursorOwned = State.MenuOpen;
                State.Reset(); ToolScope = false;
                nativeBound = null; nativeQueued = null; nativePickerWasOpen = false;
                if (cursorOwned && Player.m_localPlayer != null && Application.isFocused)
                { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            }
        }
        void OnDisable() { if (B != null) Reset(true); RemoveCatalog(); }
        void OnDestroy() { if (B != null) Reset(true); RemoveCatalog(); harmony?.UnpatchSelf(); if (I == this) I = null; }
        void OnApplicationFocus(bool focus) { if (!focus && B != null) Reset(true); }

        static void AfterTownUpdate(object __instance)
        {
            if (I == null || !I.ready || I.faulted || !I.enabled) return;
            try { I.Tick(__instance); } catch (Exception ex) { I.Fail(ex); }
        }
        void Tick(object mode)
        {
            if (!ReferenceEquals(Mode, mode) || lastPlayer != Player.m_localPlayer || lastNet != ZNet.instance)
            {
                Reset(true); Mode = mode; lastPlayer = Player.m_localPlayer; lastNet = ZNet.instance;
                B.Call("AnnoMode", "ClearSelection", Mode);
            }
            if (!Live) { Reset(true); return; }
            EnsureCatalog();
            // The native picker is a handoff, not a world/focus reset. Keep its pending selection.
            if (Hud.IsPieceSelectionVisible())
            {
                if (State.ToolActive) StopTool();
                nativePickerWasOpen = true;
                return;
            }
            if (ExternalUi()) { Reset(true); return; }
            if (nativePickerWasOpen)
            {
                nativePickerWasOpen = false;
                nativeQueued = SelectedEntry();
            }
            SyncNativeSelection();
            State.Release(Time.frameCount, LeftHeld, RightHeld);
            if (Pressed(menuKey.Value))
            {
                if (State.MenuOpen) CloseMenu();
                else OpenMenu(2);
                return;
            }
            if (State.MenuOpen)
            {
                if ((bool)B.Get("AnnoMode", "mapOpen", Mode)) B.Call("AnnoMode", "TickMapZoom", Mode);
                if (Pressed(Key.Escape) || (!State.WaitingForRelease && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)) CloseMenu();
                return;
            }
            if (State.ToolActive)
            {
                if (B.Tool(Mode) == 0) { State.StopTool(Time.frameCount); return; }
                if (Pressed(Key.Escape)) { StopTool(); return; }
                if (nativeBound != null && !State.WaitingForRelease && ZInput.GetButtonDown("BuildMenu"))
                {
                    StopTool(); Hud.instance.TogglePieceSelection(); return;
                }
                RotatePreview();
                ToolScope = true;
                try
                {
                    int tool = B.Tool(Mode);
                    if (tool == 1) B.Call("AnnoMode", "UpdatePlacement", Mode);
                    else if (tool == 2) B.Call("AnnoMode", "UpdateRoad", Mode);
                    else if (tool == 6) B.Call("AnnoMode", "UpdateWall", Mode);
                    else { StopTool(); Tell("Choose a building, wall, or road plan."); }
                }
                finally { ToolScope = false; }
                if (B.Tool(Mode) == 0) State.StopTool(Time.frameCount);
                return;
            }
            if (SelectedEntry() != null && ZInput.GetButtonDown("BuildMenu")) { Hud.instance.TogglePieceSelection(); return; }
            if (Pressed(inspectKey.Value)) Inspect();
            else if (Pressed(selectKey.Value)) SelectSquad();
            else if (Pressed(orderKey.Value)) Order();
        }
        void OpenMenu(int selectedTab)
        {
            if (State.ToolActive) B.Call("AnnoMode", "RemoteEndTool", Mode);
            nativeBound = null; nativeQueued = null;
            B.Call("AnnoMode", "RemoteWorldUi", Mode, "close", 0);
            B.Call("PlayerWorker", "Stop", null);
            State.OpenMenu(Time.frameCount); tab = selectedTab; scroll = Vector2.zero;
        }
        void CloseMenu()
        {
            B.Call("AnnoMode", "RemoteWorldUi", Mode, "close", 0);
            B.Set("AnnoMode", "storageOpen", false, Mode);
            State.CloseMenu(Time.frameCount); GUIUtility.keyboardControl = 0;
        }
        void StopTool() { B.Call("AnnoMode", "RemoteEndTool", Mode); State.StopTool(Time.frameCount); nativeBound = null; }
        void StartTool(string method, object definition)
        {
            B.Call("AnnoMode", method, Mode, definition);
            if (B.Tool(Mode) == 1 || B.Tool(Mode) == 2 || B.Tool(Mode) == 6)
            {
                B.Set("AnnoMode", "placeYaw", Player.m_localPlayer.transform.eulerAngles.y, Mode);
                State.StartTool(Time.frameCount);
            }
        }
        Camera Camera() => B.Call("AnnoMode", "Cam", null) as Camera;
        bool Hit(out RaycastHit hit)
        {
            hit = default(RaycastHit);
            var camera = Camera(); var player = Player.m_localPlayer;
            if (camera == null || player == null) return false;
            var ghost = B.Get("AnnoMode", "ghost", Mode) as GameObject;
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            var hits = Physics.RaycastAll(ray, reach.Value + Vector3.Distance(camera.transform.position, player.transform.position), ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var candidate in hits)
            {
                if (candidate.collider == null) continue;
                var tr = candidate.collider.transform;
                if (tr.IsChildOf(player.transform) || (ghost != null && tr.IsChildOf(ghost.transform))) continue;
                if (!InteractionState.WithinReach((candidate.point - player.transform.position).sqrMagnitude, reach.Value)) return false;
                hit = candidate; return true; // The first visible obstruction wins; do not select through walls.
            }
            return false;
        }
        bool Ground(out Vector3 point)
        {
            point = Vector3.zero;
            if (!Hit(out var hit)) return false;
            int mask = (int)B.Call("Util", "get_GroundMask", null);
            if ((mask & (1 << hit.collider.gameObject.layer)) == 0) return false;
            point = hit.point; return true;
        }
        bool TryInspect()
        {
            if (Hit(out var hit))
            {
                var building = B.Call("AnnoMode", "BuildingOf", null, hit.collider.gameObject);
                if (building is UnityEngine.Object obj && obj != null)
                { B.Call("AnnoMode", "RemoteSelect", Mode, building); OpenMenu(2); return true; }
            }
            return false;
        }
        void Inspect()
        {
            if (TryInspect()) return;
            Tell("Aim at one of your settlement buildings, then press " + inspectKey.Value + ".");
        }
        bool OwnSoldier(object soldier)
        {
            if (!(soldier is Component component) || component == null || !(bool)B.Call("Soldier", "get_Valid", soldier)) return false;
            var barracks = B.Call("Soldier", "Barracks", soldier) as Component;
            return barracks != null && !(bool)B.Get("AnnoBuilding", "Foreign", barracks)
                && Convert.ToInt64(B.Call("AnnoBuilding", "get_Colony", barracks)) == Convert.ToInt64(B.Call("Col", "get_Me", null));
        }
        void SelectSquad()
        {
            if (Hit(out var hit))
            {
                var soldier = hit.collider.GetComponentInParent(B.Type("Soldier"));
                if (OwnSoldier(soldier))
                { B.Call("AnnoMode", "SelectSquadOf", Mode, soldier); Tell("Squad selected. Aim and press " + orderKey.Value + " to move or attack."); return; }
            }
            Tell("Aim at one of your soldiers, then press " + selectKey.Value + ".");
        }
        void Order()
        {
            var squad = (IList)B.Get("AnnoMode", "selSoldiers", Mode);
            for (int n = squad.Count - 1; n >= 0; --n) if (!OwnSoldier(squad[n])) squad.RemoveAt(n);
            if (squad.Count == 0) { Tell("Select your squad with " + selectKey.Value + " first."); return; }
            if (!Hit(out var hit)) { Tell("Aim at a nearby destination or hostile target."); return; }
            var target = hit.collider.GetComponentInParent<Character>();
            if (target != null && target != Player.m_localPlayer && (bool)B.Call("Military", "Hostile", null, target))
            { B.Call("Military", "Order", null, squad, hit.point, target); Tell("Attack order issued."); }
            else if (Ground(out var point))
            { B.Call("Military", "Order", null, squad, point, null); Tell("Move order issued."); }
            else Tell("Aim at open ground or a hostile target.");
        }

        void OnGUI()
        {
            if (!Live || !enabled || !Application.isFocused) return;
            try
            {
                if (State.MenuOpen) DrawMenu();
                else if (hints.Value)
                {
                    string text = State.ToolActive
                        ? (B.Tool(Mode) == 2 || B.Tool(Mode) == 6 ? "Road/wall: click start, then end. Right-click: " + (nativeBound != null ? "build picker" : "cancel") : "Left-click place | Wheel / R rotate | Shift: fine rotation | T design | Right-click " + (nativeBound != null ? "build picker" : "cancel"))
                        : menuKey.Value + " Town menus | Left-click house: manage | " + inspectKey.Value + " Manage | " + selectKey.Value + " Squad | " + orderKey.Value + " Order";
                    GUI.Box(new Rect(16, Screen.height - 96, Mathf.Min(790, Screen.width - 32), 70), text + "\n" + (Time.unscaledTime < noticeUntil ? notice : State.ToolActive ? Convert.ToString(B.Get("AnnoMode", "hoverText", Mode)) : ""));
                    if (State.ToolActive) GUI.Label(new Rect(Screen.width / 2f - 6, Screen.height / 2f - 12, 24, 24), "+");
                }
            }
            catch (Exception ex) { Fail(ex); }
        }
        void DrawCatalogMenu()
        {
            // Construction lives exclusively in the native hammer/hoe picker.
            GUILayout.BeginArea(new Rect(16, 62, 360, Mathf.Min(300, Screen.height - 90)), GUI.skin.box);
            GUILayout.Label("SETTLEMENT MANAGEMENT");
            int[] destinations = { 2, 4, 5, 6, 7 };
            string[] labels = { "Manage", "Research", "World", "Fleet", "Storage" };
            GUILayout.BeginHorizontal();
            for (int n = 0; n < destinations.Length; n++)
                if (GUILayout.Button(labels[n]))
                {
                    tab = destinations[n]; scroll = Vector2.zero;
                    B.Call("AnnoMode", "RemoteWorldUi", Mode, tab == 4 ? "research" : tab == 5 ? "map" : "close", 0);
                }
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            if (tab == 6) DrawFleetList();
            else GUILayout.Label("Build with your hammer or hoe. Choose Categories in the normal build picker for Town plans or Valheim pieces.\n\nLeft-click a house to manage it. Right-click closes this panel.");
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close (right-click)")) CloseMenu();
            GUILayout.EndArea();
            if (tab == 2 && State.MenuOpen)
            {
                B.Call("UI", "Begin", null);
                try { B.Call("AnnoMode", "DrawPanel", Mode); B.Call("AnnoMode", "DrawTooltip", Mode); }
                finally { B.Call("UI", "End", null); }
            }
        }

        static void BeforeCityToggle(bool __0)
        {
            if (I == null || !I.ready || !__0) return;
            try { I.Reset(true); } catch (Exception ex) { I.Fail(ex); }
        }
        static bool AimGround(ref Vector3 __0, ref bool __result)
        {
            if (I == null || !I.ToolScope) return true;
            __result = I.Ground(out __0);
            if (!__result)
            {
                I.B.Call("Roads", "HidePreview", null);
                I.B.Call("AnnoMode", "HideWallLine", I.Mode);
                I.B.Call("GridOverlay", "Hide", null);
                I.B.Set("AnnoMode", "hoverText", "Aim at visible ground within " + I.reach.Value + " m.", I.Mode);
            }
            return false;
        }
        static bool NotOverUi(ref bool __result) { if (I == null || !I.ToolScope) return true; __result = false; return false; }
        static bool ToolLeft(ref bool __result)
        { if (I == null || !I.ToolScope) return true; __result = I.State.CanPlace && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; return false; }
        static bool ToolRight(ref bool __result)
        { if (I == null || !I.ToolScope) return true; __result = !I.State.WaitingForRelease && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame; return false; }
        static void FilterActions(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold)
        {
            if (I != null && __instance == Player.m_localPlayer 
                && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && I.CanClickBuilding())
            {
                try { I.TryInspect(); } catch (Exception ex) { I.Fail(ex); }
            }
            if (I == null || !I.Blocking || __instance != Player.m_localPlayer) return;
            attack = attackHold = secondaryAttack = secondaryAttackHold = block = blockHold = false;
        }
        static void FilterHammer(Player __instance, ref bool takeInput)
        { if (I != null && __instance == Player.m_localPlayer && (I.Blocking || I.SelectedEntry() != null)) takeInput = false; }
        static bool FilterInput(PlayerController __instance, ref bool __result)
        { if (I == null || !I.MenuOpen || __instance.GetComponent<Player>() != Player.m_localPlayer) return true; __result = false; return false; }
        static bool FreezeMenuCamera() => I == null || !I.MenuOpen;
        static bool MenuCursor()
        {
            if (I == null || !I.MenuOpen) return true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return false;
        }
    }
}
