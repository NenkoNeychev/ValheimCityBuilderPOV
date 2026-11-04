using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CityBuilderPOV
{
    public sealed partial class Plugin
    {
        readonly Dictionary<object, List<int>> categoryLists = new Dictionary<object, List<int>>();
        void InstallCategoryHooks()
        {
            var type = typeof(Player).Assembly.GetType("ByUsagePieceList", true);
            Patch(AccessTools.Method(type, "UpdateAvailableTags"), null, nameof(UpdateTownCategories));
            Patch(AccessTools.Method(type, "get_DisplayName"), nameof(CategoryTitle));
            Patch(AccessTools.Method(type, "get_TagCount"), nameof(CategoryCount));
            Patch(AccessTools.Method(type, "GetTagIdByIndex"), nameof(CategoryId));
            Patch(AccessTools.Method(type, "GetTagDisplayName"), nameof(CategoryName));
            Patch(AccessTools.Method(type, "GetAvailablePiecesWithTag"), nameof(CategoryPieces));
        }
        static void UpdateTownCategories(object __instance, PieceTable __0)
        {
            if (I == null) return;
            I.categoryLists.Remove(__instance);
            if (!I.Live || !I.injected.ContainsKey(__0)) return;
            var present = new HashSet<int>();
            foreach (var piece in __0.m_availablePieces)
                present.Add(I.catalog.TryGetValue(piece, out var entry) ? entry.Kind : 0);
            var groups = new List<int>();
            foreach (int kind in new[] { 1, 6, 2, 0 }) if (present.Contains(kind)) groups.Add(kind);
            I.categoryLists[__instance] = groups;
        }
        static bool CategoryTitle(object __instance, ref string __result)
        {
            if (I == null || !I.categoryLists.ContainsKey(__instance)) return true;
            __result = "Categories"; return false;
        }
        static bool CategoryCount(object __instance, ref int __result)
        {
            if (I == null || !I.categoryLists.TryGetValue(__instance, out var groups)) return true;
            __result = groups.Count; return false;
        }
        static bool CategoryId(object __instance, int __0, ref int __result)
        {
            if (I == null || !I.categoryLists.TryGetValue(__instance, out var groups)) return true;
            __result = __0 >= 0 && __0 < groups.Count ? CatalogGroups.Id(groups[__0]) : -1;
            return false;
        }
        static bool CategoryName(object __instance, int __0, ref string __result)
        {
            if (I == null || !I.categoryLists.TryGetValue(__instance, out var groups)) return true;
            __result = __0 >= 0 && __0 < groups.Count ? CatalogGroups.Name(groups[__0]) : "";
            return false;
        }
        static bool CategoryPieces(object __instance, int __0, PieceTable __1, IList<Piece> __2)
        {
            if (I == null || !I.categoryLists.ContainsKey(__instance) || !I.injected.ContainsKey(__1)) return true;
            foreach (var piece in __1.m_availablePieces)
            {
                int kind = I.catalog.TryGetValue(piece, out var entry) ? entry.Kind : 0;
                // Native repair/remove remain available as the picker's special action.
                if (piece.m_repairPiece || piece.m_removePiece || CatalogGroups.Matches(__0, kind)) __2.Add(piece);
            }
            return false;
        }
    }
}
