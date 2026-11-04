using System;
using CityBuilderPOV;
class InteractionTests
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Main()
    {
        var s = new InteractionState();
        Check(!s.BlocksActions && !s.CanPlace, "Ordinary play is unaffected");
        s.OpenMenu(10);
        Check(s.BlocksActions && !s.CanPlace, "Menu clicks cannot place or attack");
        s.StartTool(11);
        s.Release(11, false, false);
        Check(!s.CanPlace, "Menu selection does not place in its own frame");
        s.Release(12, true, false);
        Check(!s.CanPlace, "Held menu click cannot place on following frames");
        s.Release(13, false, true);
        Check(!s.CanPlace, "Held cancel must also be released");
        s.Release(14, false, false);
        Check(s.CanPlace && s.BlocksActions, "A new click places while weapon input stays blocked");
        s.StopTool(15);
        Check(!s.CanPlace && s.BlocksActions, "Cancel cannot leak into block or hammer input");
        s.Release(16, false, true);
        Check(s.BlocksActions, "Cancel release fence remains while RMB is held");
        s.Release(17, false, false);
        Check(!s.BlocksActions, "Combat restored after release");
        s.OpenMenu(18); s.CloseMenu(19);
        Check(!s.MenuOpen && s.BlocksActions, "Closing menu cannot leak a click into combat");
        s.Release(20, false, false);
        Check(!s.BlocksActions, "Closing menu restores normal controls after release");
        s.StartTool(21); s.Reset();
        Check(!s.ToolActive && !s.BlocksActions, "Death, logout, focus loss and F7 handoff clear input ownership");
        s.OpenMenu(22); s.Reset();
        Check(!s.MenuOpen && !s.BlocksActions, "Reset also closes menus");
        Check(InteractionState.WithinReach(1600,40), "Placement at boundary allowed");
        Check(!InteractionState.WithinReach(1601,40), "Placement outside reach rejected");
        Check(!InteractionState.WithinReach(float.NaN,40), "Invalid hit rejected");
        Check(!InteractionState.WithinReach(float.PositiveInfinity,40), "Infinite hit rejected");
        Check(!InteractionState.WithinReach(-1,40), "Negative squared distance rejected");
        Check(!InteractionState.WithinReach(1,float.NaN), "Invalid reach rejected");
        Check(!InteractionState.WithinReach(1,0), "Zero reach rejected");
        Check(InteractionState.Rotate(350,1,22.5f) == 12.5f, "Clockwise rotation wraps");
        Check(InteractionState.Rotate(0,-1,22.5f) == 337.5f, "Reverse rotation wraps");
        Check(InteractionState.Rotate(45,1,5) == 50, "Fine adjustment preserves arbitrary yaw");
        Check(InteractionState.Rotate(45,0,5) == 45, "No rotation input preserves yaw");
        Check(InteractionState.Rotate(45,1,float.NaN) == 45, "Invalid step rejected");
        Check(InteractionState.Rotate(float.NaN,1,5) == 5, "Invalid yaw recovered");
        s.OpenMenu(30); s.Release(31,true,false);
        Check(s.MenuOpen && s.WaitingForRelease && !s.CanPlace, "Opening house click cannot operate placement");
        s.Release(32,false,false); s.CloseMenu(33); s.Release(34,false,true);
        Check(!s.MenuOpen && s.BlocksActions, "Right click close stays consumed until released");
        s.Release(35,false,false);
        Check(!s.BlocksActions, "House close restores normal play on release");
        Check(CatalogGroups.Matches(CatalogGroups.Id(0),0), "Valheim category contains ordinary pieces");
        Check(!CatalogGroups.Matches(CatalogGroups.Id(0),1), "Town houses excluded from Valheim category");
        Check(!CatalogGroups.Matches(CatalogGroups.Id(1),0), "Ordinary pieces excluded from Town buildings");
        Check(CatalogGroups.Matches(CatalogGroups.Id(6),6), "Town walls category matches walls");
        Check(!CatalogGroups.Matches(CatalogGroups.Id(2),6), "Roads category excludes walls");
        Check(CatalogGroups.Matches(-1,0) && CatalogGroups.Matches(-1,1), "Native All filter retains all pieces");
        Check(!CatalogGroups.Matches(0,0), "Stale native tag cannot silently select another category");
        Console.WriteLine(checks + " interaction and placement assertions passed.");
    }
}
