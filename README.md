# CityBuilder POV вЂ” 0.3.0 prototype

Play RustyProductions' ValheimCityBuilder 1.2.2 from your normal third-person camera. This independently authored companion runs alongside the original mod.

**Status:** Release compilation, 36 interaction/category/rotation/reach assertions, and 177 binary API contract assertions passed. This version has not been loaded or tested inside Valheim by us. First try it in a separate test profile with a copy of a world.

## Install or update

1. Install the original ValheimCityBuilder 1.2.2 and its BepInEx dependency in your chosen profile.
2. Close Valheim. Extract `CityBuilderPOV-0.3.0-prototype.zip` into that profile's `BepInEx` folder, replacing the previous companion DLL at `plugins/CityBuilderPOV/CityBuilderPOV.dll`. Keep only one copy of the companion loaded.
3. Start the game and equip your hammer or hoe, or press F6 for the town hub. F7 is not required.

The archive includes only our companion and this guide. Do not replace the original ValheimCityBuilder.dll. The latest build is also installed in the user-designated Thunderstore V-City profile. Restart that profile after an update. The companion checks for original version 1.2.2.

## Menu cleanup in 0.3.0

The duplicate F6 construction list and upstream overlay in normal third-person play are removed. F6 retains only Manage, Research, World, Fleet and Storage. Clickable house panels remain. The original F7 view is still available explicitly.

## Controls

| Control | Action |
| --- | --- |
| Hammer + normal build picker | Categories → Town buildings / Town walls |
| Hoe + normal build picker | Categories → Town roads & terrain |
| Left-click an owned settlement house/building | Open that building's management panel when outside placement mode |
| Right-click while a town menu is open | Close it and return to your Viking |
| H | Manage the owned building under the crosshair, including while holding a build tool |
| E on a settlement piece without a normal interaction | Open management; doors and containers keep their normal E action |
| F6 | Open/close the town hub |
| Left-click during placement | Place a building or choose a road/wall/area start and end |
| Mouse wheel / R | Rotate an ordinary building preview, default 22.5 degrees |
| Shift + wheel / Shift + R | Fine rotation, default 5 degrees; Shift + R turns backwards |
| T | Cycle available building designs |
| Right-click with a hammer/hoe town plan active | Return to the normal build picker |
| Escape | Cancel tool/menu; Valheim's pause menu may also open |
| J / Shift + J | Select own soldier's squad / add another squad |
| K | Order selected soldiers to aimed ground or an aimed hostile target |

Left-click management applies to buildings recognized by CityBuilder, including its houses, within the configured reach and visible under the crosshair. Arbitrary vanilla hand-built houses are not colony buildings. Foreign town ownership remains enforced. While the hammer/hoe is in placement mode, construction takes priority; put it away to click houses or use H.

The native picker retains ordinary Valheim pieces. Settlement entries use original progression and settlement-storage costs, not the empty player-inventory requirements of their menu templates. The templates are not networked world objects and their names are not saved into learned recipes. Select an ordinary piece to return to vanilla construction.

## Buildings, roads and rotation

Aim at visible ground, walk normally, and place using the original construction preview. The original mod still validates and performs placement, charges resources, and maintains road connectivity. Houses rotate before placement with the wheel or R; Shift provides finer control. Coastal structures retain the original automatic shore alignment and dock checks. Walls follow their endpoints; road/area tools follow the original grid. This does not rotate buildings that are already placed.

Construction is exclusively in the native hammer/hoe picker. In its first tab (Categories / By usage), choose Town buildings, Town walls, Town roads & terrain, or Valheim. Categories appear when the equipped tool has available pieces in them. Native All still shows all pieces; material, recent and favorite views remain available. Ordinary pieces are grouped under Valheim without changing their resources, unlocks, or prefab data. Roads and walls use start/end placement. Default targeting reach is 40 metres from the Viking, with the first obstruction stopping the ray.

## Management and world menus

Click a house to use its original management panel, including its available production, upgrades, storage or training actions. Right-click closes the menu. F6 adds Research, World, Fleet and Storage access. The original world map opens discovered settlements and their trade, diplomacy and expedition panels. Fleet lists existing ships and opens their original cargo/route management panel. Available actions still depend on original progression and game state.

Select soldiers with J, issue orders with K, and open F6 в†’ Manage for squad actions. Invalid, dead and foreign soldiers are filtered before orders.

Menus release the cursor and stop character input/camera movement. The opening/closing click must be released before construction or combat resumes. During placement, movement remains available while attacks and vanilla placement are suppressed. The original F7 view remains available. The companion does not activate ghost mode or damage immunity.

## Configuration

`BepInEx/config/local.citybuilder.pov.cfg` is created on first load. Configure the F6/H/J/K bindings, reach, hints, `NativeHammerAndHoe`, `ClickBuildings`, `RotationStep`, and `FineRotationStep`. Keyboard/mouse is the supported control target; a complete controller scheme is not implemented.

## Build and checks

Run `build.ps1` for an optimized Release build with no DEBUG define. Defaults reuse the sibling V-Runner compiler and reference directory read-only; override `-Compiler` and `-ReferenceDirectory` as needed. This is a separate project; V-Runner Full/Light are unchanged and do not apply here.

`tests/run.ps1` checks production interaction state, release fences, rotation and reach. `tests/verify-contract.ps1 -ModDll <original DLL> -ReferenceDirectory <game reference directory>` reads binary metadata without executing the original mod. Build logs and source/output/reference hashes are in `bin/Release`; test evidence is in `tests/bin`. These checks do not validate Unity rendering, Harmony detouring, live costs or multiplayer.

## In-game acceptance checks

- Confirm `CityBuilder POV 0.3.0 ready` in the BepInEx log and no patch errors.
- Equip hammer and hoe. Open their normal pickers, select Town entries, switch back to ordinary pieces, reopen the picker, holster/re-equip, and confirm there is exactly one preview/placement.
- Rotate a house both directions across 360 degrees; verify Shift precision and T design changes. Confirm coastal plans still validate and align correctly.
- Place roads, areas and walls. Check costs, previews, connectivity, cancel and persistence after relog.
- Left-click each owned settlement building type. Check the correct panel opens without an attack; right-click closes without block/placement and restores looking/movement. Check H while holding a hammer, E on an ordinary piece, and normal doors/chests.
- Open research, world-map sites, trade/diplomacy, storage, shipyard and ship routes. Verify buttons, text fields, cursor, tooltips and close behavior.
- Check inventory/chat/map/pause, loss of focus, death, logout/rejoin, F7 handoff, and plugin disable. Confirm cleanup and ordinary controls afterward.
- Test soldier selection/move/attack and foreign ownership rejection. Repeat in two-player testing and verify remote visibility and saved state.

Original mod: https://thunderstore.io/c/valheim/p/RustyProductions/ValheimCityBuilder/
