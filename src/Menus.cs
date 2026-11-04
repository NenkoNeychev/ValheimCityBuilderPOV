using System;
using System.Collections;
using UnityEngine;

namespace CityBuilderPOV
{
    public sealed partial class Plugin
    {
        void DrawMenu()
        {
            // DrawUi is suppressed while we own the menu; maintain its per-frame caches.
            if (Event.current.type == EventType.Repaint)
            {
                ((IList)B.Get("AnnoMode", "uiNext")).Clear();
                ((IList)B.Get("AnnoMode", "uiRects")).Clear();
            }
            B.Set("AnnoMode", "tip", null, Mode);
            B.Set("AnnoMode", "tipDef", null, Mode);
            bool world = (bool)B.Call("AnnoMode", "get_WorldWindowOpen", Mode);
            if (world || tab == 7)
            {
                B.Call("UI", "Begin", null);
                try
                {
                    if (world) B.Call("AnnoMode", "DrawWorldWindows", Mode);
                    else
                    {
                        B.Set("AnnoMode", "storageOpen", true, Mode);
                        B.Call("AnnoMode", "DrawTopBar", Mode);
                    }
                    B.Call("AnnoMode", "DrawTooltip", Mode);
                }
                finally { B.Call("UI", "End", null); }
                DrawMenuFooter();
                return;
            }
            // The upstream window's close button returns to the hub rather than leaving an empty tab.
            if (tab == 4 || tab == 5) tab = 2;
            DrawCatalogMenu();
        }
        void DrawMenuFooter()
        {
            var rect = new Rect(Mathf.Max(8, (Screen.width - 420) / 2f), Screen.height - 46, Mathf.Min(420, Screen.width - 16), 38);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Town menus"))
            {
                B.Call("AnnoMode", "RemoteWorldUi", Mode, "close", 0);
                B.Set("AnnoMode", "storageOpen", false, Mode);
                tab = 2;
            }
            if (GUILayout.Button("Close (right-click)")) CloseMenu();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        void DrawFleetList()
        {
            int count = 0;
            foreach (var ship in (IEnumerable)B.Call("Ships", "get_All", null))
            {
                count++;
                int id = (int)B.Get("ShipData", "Id", ship);
                string name = Convert.ToString(B.Get("ShipData", "Name", ship));
                if (string.IsNullOrEmpty(name)) name = "Ship " + id;
                string status = Convert.ToString(B.Call("Ships", "StatusText", null, ship));
                if (GUILayout.Button(name + "\n" + status, GUILayout.MinHeight(48))) B.Call("AnnoMode", "OpenShip", null, id);
            }
            if (count == 0) GUILayout.Label("No ships yet. Build a harbor and shipyard with your hammer, then open the shipyard's management panel to launch a ship.");
            GUILayout.Label("Use the ship panel to choose trading partners, configure cargo, and manage its route. The world map opens discovered settlements, trade, diplomacy and expeditions.");
        }
    }
}
