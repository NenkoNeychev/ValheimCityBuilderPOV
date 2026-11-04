using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace CityBuilderPOV
{
    // Reference the installed dependency at runtime; no upstream code/assets are bundled.
    internal sealed class Bridge
    {
        internal readonly Assembly Assembly;
        readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
        readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        internal Bridge()
        {
            Assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetType("ValheimAnno.Plugin") != null);
            var version = (string)Field("Plugin", "Version").GetRawConstantValue();
            if (version != "1.2.2") throw new NotSupportedException("CityBuilder POV requires CityBuilder 1.2.2; found " + version);
            foreach (var name in new[] { "Update", "UpdatePlacement", "UpdateRoad", "UpdateWall", "SelectWall", "RemoteWorldUi", "get_WorldWindowOpen", "DrawWorldWindows", "DrawTopBar", "DrawTooltip", "TickMapZoom", "HideWallLine", "DrawUi", "OpenSite", "OpenShip", "OpenMerchant", "RemoteEndTool", "ClearSelection", "DrawPanel", "Select", "SelectRoad", "RemoteSelect", "SelectSquadOf", "BuildingOf", "Cam", "MouseGround", "MouseOverUI", "SetActive" })
                Method("AnnoMode", name);
            foreach (var name in new[] { "Active", "tool", "ghost", "selSoldiers", "hoverText", "uiTyping", "placeYaw", "mapOpen", "storageOpen", "uiNext", "uiRects", "tip", "tipDef" }) Field("AnnoMode", name);
            Method("UI", "Begin", 0); Method("UI", "End", 0);
            Method("Military", "Order"); Method("Military", "Hostile");
            Method("In", "get_LeftDown"); Method("In", "get_RightDown");
            Method("Defs", "Unlocked"); Method("Economy", "CostOf");
            Field("Defs", "All"); Field("Roads", "All"); Field("Walls", "All");
            Method("Walls", "Unlocked"); Method("Economy", "Has", 1);
            Method("Defs", "CostText"); Method("In", "Pressed");
            Method("Icons", "Piece"); Method("Icons", "Item");
            Method("Ships", "get_All"); Method("Ships", "StatusText");
            foreach (var name in new[] { "Name", "Desc", "Anchor" }) Field("BDef", name);
            foreach (var name in new[] { "Name", "Desc", "Icon", "CostItem", "CostPerTile" }) Field("Roads+RDef", name);
            foreach (var name in new[] { "Name", "Desc", "Icon", "Cost" }) Field("Walls+WDef", name);
            Field("ShipData", "Id"); Field("ShipData", "Name");
        }
        internal Type Type(string name) => Assembly.GetType("ValheimAnno." + name, true);
        internal FieldInfo Field(string type, string name)
        {
            string key = type + "." + name;
            if (!fields.TryGetValue(key, out var field))
            {
                field = AccessTools.Field(Type(type), name) ?? throw new MissingFieldException(key);
                fields.Add(key, field);
            }
            return field;
        }
        internal MethodInfo Method(string type, string name, int count = -1)
        {
            string key = type + "." + name + ":" + count;
            if (!methods.TryGetValue(key, out var method))
            {
                var matches = Type(type).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .Where(m => m.Name == name && (count < 0 || m.GetParameters().Length == count)).ToArray();
                if (matches.Length != 1) throw new MissingMethodException(key + " must resolve uniquely");
                methods.Add(key, method = matches[0]);
            }
            return method;
        }
        internal object Call(string type, string name, object target, params object[] args) => Method(type, name, args.Length).Invoke(target, args);
        internal object Get(string type, string name, object target = null) => Field(type, name).GetValue(target);
        internal void Set(string type, string name, object value, object target = null) => Field(type, name).SetValue(target, value);
        internal bool Active => (bool)Get("AnnoMode", "Active");
        internal int Tool(object mode) => Convert.ToInt32(Get("AnnoMode", "tool", mode));
    }
}
