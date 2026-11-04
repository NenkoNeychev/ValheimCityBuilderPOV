using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityBuilderPOV
{
    public sealed partial class Plugin
    {
        sealed class Entry
        {
            internal Piece Piece;
            internal object Definition;
            internal int Kind; // 1 building, 2 road, 6 wall
            internal string Type => Kind == 1 ? "BDef" : Kind == 2 ? "Roads+RDef" : "Walls+WDef";
            internal string Select => Kind == 1 ? "Select" : Kind == 2 ? "SelectRoad" : "SelectWall";
        }
        readonly Dictionary<Piece, Entry> catalog = new Dictionary<Piece, Entry>();
        readonly Dictionary<PieceTable, List<GameObject>> injected = new Dictionary<PieceTable, List<GameObject>>();
        GameObject catalogRoot;
        ObjectDB catalogDb;
        Entry nativeQueued, nativeBound;
        bool nativePickerWasOpen;
        bool refreshingCatalog;
        int consumedUseFrame = -1;
        static readonly FieldInfo VanillaGhost = AccessTools.Field(typeof(Player), "m_placementGhost");
        static readonly MethodInfo RefreshPieces = AccessTools.Method(typeof(Player), "UpdateAvailablePiecesList");

        void InstallNativeHooks()
        {
            InstallCategoryHooks();
            Patch(AccessTools.Method(typeof(PieceTable), "UpdateAvailable"), nameof(PrepareCatalog));
            Patch(AccessTools.Method(typeof(Player), "SetSelectedPiece", new[] { typeof(Vector2Int) }), null, nameof(NativeSelected));
            Patch(AccessTools.Method(typeof(Player), "SetupPlacementGhost"), nameof(NoProxyGhost));
            Patch(AccessTools.Method(typeof(Player), "TryPlacePiece"), nameof(NoProxyPlace));
            Patch(AccessTools.Method(typeof(Player), "HaveRequirements", new[] { typeof(Piece), typeof(Player.RequirementMode) }), nameof(NativeRequirements));
            Patch(AccessTools.Method(typeof(ZInput), "GetMouseScrollWheel"), nameof(NoPlacementZoom));
            Patch(AccessTools.Method(typeof(ZInput), "GetButtonDown", new[] { typeof(string) }), null, nameof(ContextButtons));
            Patch(B.Method("In", "Pressed"), nameof(OwnRotationKey));
            Patch(B.Method("AnnoMode", "DrawUi"), nameof(HideUnderlyingTownUi));
            foreach (string name in new[] { "OpenSite", "OpenShip", "OpenMerchant" }) Patch(B.Method("AnnoMode", name), null, nameof(ContextOpened));
            if (VanillaGhost == null || RefreshPieces == null) throw new MissingMemberException("Native placement bridge is unavailable");
        }
        Entry SelectedEntry()
        {
            var player = Player.m_localPlayer;
            if (player == null || !ready || faulted || !nativeTools.Value) return null;
            Piece selected = player.GetSelectedPiece();
            return selected != null && catalog.TryGetValue(selected, out var entry) ? entry : null;
        }
        void EnsureCatalog()
        {
            if (!nativeTools.Value) { if (catalogRoot != null) RemoveCatalog(); return; }
            if (ObjectDB.instance == null || ZNetScene.instance == null) return;
            if (catalogRoot != null && catalogDb == ObjectDB.instance) return;
            RemoveCatalog();
            catalogDb = ObjectDB.instance;
            var hammer = catalogDb.GetItemPrefab("Hammer");
            var hoe = catalogDb.GetItemPrefab("Hoe");
            if (hammer == null || hoe == null) return;
            var hammerTable = hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
            var hoeTable = hoe.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces;
            if (hammerTable == null || hoeTable == null) return;
            catalogRoot = new GameObject("CityBuilderPOV_MenuTemplates");
            catalogRoot.SetActive(false);
            DontDestroyOnLoad(catalogRoot);
            foreach (var def in (IEnumerable)B.Get("Defs", "All")) AddEntry(hammerTable, def, 1, Piece.PieceCategory.BuildingWorkbench);
            foreach (var def in (IEnumerable)B.Get("Walls", "All")) AddEntry(hammerTable, def, 6, Piece.PieceCategory.BuildingWorkbench);
            foreach (var def in (IEnumerable)B.Get("Roads", "All")) AddEntry(hoeTable, def, 2, Piece.PieceCategory.Misc);
            refreshingCatalog = true;
            try { RefreshPieces.Invoke(Player.m_localPlayer, null); }
            finally { refreshingCatalog = false; }
        }
        void AddEntry(PieceTable table, object def, int kind, Piece.PieceCategory category)
        {
            var entry = new Entry { Definition = def, Kind = kind };
            var go = new GameObject("CBPOV_Menu_" + catalog.Count);
            go.transform.SetParent(catalogRoot.transform, false);
            entry.Piece = go.AddComponent<Piece>();
            entry.Piece.m_name = "Town: " + Convert.ToString(B.Get(entry.Type, "Name", def));
            entry.Piece.m_category = table.m_categories.Contains(category) ? category : table.m_categories.Count > 0 ? table.m_categories[0] : Piece.PieceCategory.Misc;
            entry.Piece.m_resources = new Piece.Requirement[0];
            entry.Piece.m_canRotate = true;
            entry.Piece.m_canBeRemoved = false;
            entry.Piece.m_enabled = Unlocked(entry);
            string iconName = kind == 1 ? Convert.ToString(B.Get("BDef", "Anchor", def)) : Convert.ToString(B.Get(entry.Type, "Icon", def));
            if (!string.IsNullOrEmpty(iconName))
            {
                entry.Piece.m_icon = B.Call("Icons", "Piece", null, iconName) as Sprite;
                if (entry.Piece.m_icon == null) entry.Piece.m_icon = B.Call("Icons", "Item", null, iconName) as Sprite;
            }
            if (entry.Piece.m_icon == null) entry.Piece.m_icon = B.Call("Icons", "Item", null, kind == 2 ? "Hoe" : "Hammer") as Sprite;
            UpdateDescription(entry);
            catalog.Add(entry.Piece, entry);
            if (!injected.TryGetValue(table, out var list)) injected.Add(table, list = new List<GameObject>());
            list.Add(go); table.m_pieces.Add(go);
        }
        bool Unlocked(Entry entry)
        {
            return entry.Kind == 2 || (bool)B.Call(entry.Kind == 1 ? "Defs" : "Walls", "Unlocked", null, entry.Definition);
        }
        object Cost(Entry entry)
        {
            if (entry.Kind == 1) return B.Call("Economy", "CostOf", null, entry.Definition);
            if (entry.Kind == 6) return B.Get("Walls+WDef", "Cost", entry.Definition);
            string item = B.Get(entry.Type, "CostItem", entry.Definition) as string;
            int amount = (int)B.Get(entry.Type, "CostPerTile", entry.Definition);
            var costs = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(item) && amount > 0) costs[item] = amount;
            return costs;
        }
        void UpdateDescription(Entry entry)
        {
            entry.Piece.m_description = Convert.ToString(B.Get(entry.Type, "Desc", entry.Definition))
                + "\n\nSettlement storage cost" + (entry.Kind == 2 ? " per new tile" : entry.Kind == 6 ? " per section" : "") + ": "
                + Convert.ToString(B.Call("Defs", "CostText", null, Cost(entry)))
                + "\n" + (entry.Kind == 1 ? "Wheel: rotate. Shift: fine rotation. T: design." : "Choose start and end points.");
        }
        void RemoveCatalog()
        {
            categoryLists.Clear();
            foreach (var pair in injected)
            {
                if (pair.Key == null) continue;
                foreach (var go in pair.Value) pair.Key.m_pieces.Remove(go);
            }
            bool hadEntries = injected.Count > 0;
            injected.Clear(); // Cleanup refresh must not reenter our table injection hook.
            // Drop references from native cached lists before destroying menu-only templates.
            if (hadEntries && Player.m_localPlayer != null && RefreshPieces != null && !refreshingCatalog)
            {
                refreshingCatalog = true;
                try { RefreshPieces.Invoke(Player.m_localPlayer, null); }
                finally { refreshingCatalog = false; }
            }
            catalog.Clear(); nativeQueued = nativeBound = null;
            if (catalogRoot != null) Destroy(catalogRoot);
            catalogRoot = null; catalogDb = null;
        }
        void SyncNativeSelection()
        {
            var selected = SelectedEntry();
            if (nativeBound != null && selected != nativeBound) StopTool();
            var queued = nativeQueued;
            nativeQueued = null;
            if (queued != null && queued == selected && !State.MenuOpen)
            {
                if (queued == nativeBound && State.ToolActive) return;
                if (!Unlocked(queued)) { Tell("This settlement plan is not unlocked yet."); return; }
                if (State.ToolActive) StopTool();
                StartTool(queued.Select, queued.Definition);
                if (State.ToolActive) nativeBound = queued;
            }
        }
        void RotatePreview()
        {
            if (B.Tool(Mode) != 1 || State.WaitingForRelease) return;
            float wheel = Mouse.current == null ? 0 : Mouse.current.scroll.ReadValue().y;
            int direction = wheel > 0 ? 1 : wheel < 0 ? -1 : 0;
            bool shift = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            if (Pressed(Key.R)) direction += shift ? -1 : 1;
            if (direction == 0) return;
            float yaw = Convert.ToSingle(B.Get("AnnoMode", "placeYaw", Mode));
            B.Set("AnnoMode", "placeYaw", InteractionState.Rotate(yaw, direction, shift ? fineRotationStep.Value : rotationStep.Value), Mode);
        }
        bool CanClickBuilding()
        {
            return Live && enabled && clickBuildings.Value && !State.BlocksActions && !ExternalUi()
                && !Player.m_localPlayer.InPlaceMode();
        }

        static void PrepareCatalog(PieceTable __instance, ref HashSet<string> knownRecipies)
        {
            if (I == null || !I.ready || !I.enabled || I.faulted || !I.injected.ContainsKey(__instance)) return;
            try
            {
                var copy = new HashSet<string>(knownRecipies);
                foreach (var go in I.injected[__instance])
                {
                    var piece = go.GetComponent<Piece>();
                    var entry = I.catalog[piece];
                    piece.m_enabled = I.Unlocked(entry);
                    I.UpdateDescription(entry);
                    copy.Add(piece.m_name);
                }
                knownRecipies = copy; // Never write synthetic recipe names into the player's save.
            }
            catch (Exception ex) { I.Fail(ex); }
        }
        static void NativeSelected(Player __instance)
        {
            if (I == null || !I.Live || __instance != Player.m_localPlayer) return;
            I.nativeQueued = I.SelectedEntry();
        }
        static bool NoProxyGhost(Player __instance)
        {
            if (I == null || __instance != Player.m_localPlayer || I.SelectedEntry() == null) return true;
            var old = VanillaGhost.GetValue(__instance) as GameObject;
            if (old != null) Destroy(old);
            VanillaGhost.SetValue(__instance, null);
            I.nativeQueued = I.SelectedEntry();
            return false;
        }
        static bool NoProxyPlace(Piece piece, ref bool __result)
        {
            if (I == null || piece == null || !I.catalog.ContainsKey(piece)) return true;
            __result = false; return false; // Menu entries must never become world/network objects.
        }
        static bool NativeRequirements(Piece piece, ref bool __result)
        {
            if (I == null || piece == null || !I.catalog.TryGetValue(piece, out var entry)) return true;
            try { __result = I.Unlocked(entry) && (bool)I.B.Call("Economy", "Has", null, I.Cost(entry)); }
            catch (Exception ex) { __result = false; I.Fail(ex); }
            return false;
        }
        static bool NoPlacementZoom(ref float __result)
        { if (I == null || !I.Live || !I.State.ToolActive) return true; __result = 0; return false; }
        static bool OwnRotationKey(Key __0, ref bool __result)
        { if (I == null || !I.ToolScope || __0 != Key.R) return true; __result = false; return false; }
        static bool HideUnderlyingTownUi() => I == null || !I.enabled || !I.Live;
        static void ContextButtons(string name, ref bool __result)
        {
            if (!__result || I == null || !I.Live || !I.enabled) return;
            if (name == "Hide" && I.State.ToolActive) { __result = false; return; }
            if (name != "Use") return;
            if (I.consumedUseFrame == Time.frameCount) { __result = false; return; }
            if (I.State.BlocksActions || I.ExternalUi()) return;
            try
            {
                // Keep doors, containers and other normal E interactions intact.
                if (I.Hit(out var hit) && hit.collider.GetComponentInParent(typeof(Interactable)) == null && I.TryInspect())
                { I.consumedUseFrame = Time.frameCount; __result = false; }
            }
            catch (Exception ex) { I.Fail(ex); }
        }
        static void ContextOpened()
        {
            if (I == null || !I.Live || !I.enabled) return;
            try
            {
                if (I.State.ToolActive) I.StopTool();
                I.State.OpenMenu(Time.frameCount); I.tab = 2;
            }
            catch (Exception ex) { I.Fail(ex); }
        }
    }
}
