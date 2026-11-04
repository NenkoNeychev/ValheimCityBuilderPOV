# CityBuilder POV — 0.1.0 prototype

Use RustyProductions' ValheimCityBuilder 1.2.2 from the normal third-person camera. This is an independently authored companion plugin. Install it alongside the original; do not replace ValheimCityBuilder.dll.

**Status:** Release compilation and automated input/API checks passed. The plugin has not been loaded or tested inside Valheim. Use a separate test profile and a copy of a world for the first run.

## Installation

1. In your chosen test profile, install `RustyProductions-ValheimCityBuilder-1.2.2` with its BepInEx dependency.
2. Close the game. Extract `CityBuilderPOV-0.1.0-prototype.zip` into that profile's `BepInEx` directory. The archive contains `plugins/CityBuilderPOV/CityBuilderPOV.dll` and this guide.
3. Start the profile and enter your test world. Press F6. You do not need to enter F7 city view.

No deployment has been performed automatically. The archive contains only this companion, not the original mod, its assets, or Valheim assemblies. The companion checks for CityBuilder version 1.2.2 and fails closed for other versions.

## Controls

| Key | Action |
| --- | --- |
| F6 | Open/close the third-person build and management menu |
| H | Manage the owned settlement building under your crosshair |
| J | Select the squad belonging to the soldier under your crosshair |
| Shift + J | Add another squad to the selection |
| K | Order selected soldiers toward aimed ground, or attack an aimed hostile target |
| Left mouse | Place a building, or choose a road start/end |
| R | Rotate a building preview |
| T | Cycle the building's available designs |
| Right mouse | Cancel placement; for roads cancel the current segment, then exit with another right click |
| Escape | Cancel the active tool/menu; Valheim's pause menu may also open |

All four main keys and the aim distance are configurable in `BepInEx/config/local.citybuilder.pov.cfg`, generated on first load. Default aim distance is 40 metres measured from your Viking. Keyboard/mouse controls are the initial target; controller bindings are not implemented.

## Building and roads

F6 → Buildings → select an unlocked building. Aim at visible ground to see the original construction preview. Walk and turn normally, rotate with R, choose a design with T, and click to place. The original mod handles costs, progression, construction, collision checks, coastal rules, and settlement separation. Warehouses, huts, and the other unlocked buildings use the original plans, not individual vanilla hammer pieces.

F6 → Roads → choose a road type. Click the start point, walk/aim toward the destination, and click the end point. Continue clicking to extend the road. The original road preview, grid routing, material charges, and road network registration are reused. The companion does not add a second road or economy system. Terrain-area painting tools are not exposed in this first version.

Menus release the cursor and temporarily stop character input and camera movement. Closing them restores ordinary gameplay. During placement, normal movement remains available while weapon attacks, blocking, and vanilla hammer placement are suppressed. Menu clicks must be released before a new placement click is accepted. The companion never enables CityBuilder's overhead camera, ghost mode, or damage immunity.

## Management and soldiers

Aim at your building and press H to open the original management panel. Its existing upgrades, production information, storage, and training controls remain in use. Foreign towns cannot be selected through this route.

Aim at your soldier and press J, then aim at a destination or hostile target and press K. For the squad management panel, open F6 → Manage after selecting the squad. The original panel provides its existing actions, including return to barracks and dismissal. Dead or foreign soldiers are filtered out before issuing an order.

The original F7 view remains available and takes control cleanly when activated. The prototype does not yet provide dedicated third-person screens for global diplomacy, research, or ship routes. It does not recognize arbitrary hand-built houses as colony buildings, change multiplayer town ownership, or change the original terrain/daylight/economy settings.

## Build and checks

Run `build.ps1` from this project. Defaults use the existing sibling V-Runner workspace only as a read-only compiler/reference source. Override `-Compiler` and `-ReferenceDirectory` for another installation. No original CityBuilder DLL is needed at compile time; it is a required runtime dependency.

`tests/run.ps1` checks production input-state transitions and reach boundaries. `tests/verify-contract.ps1 -ModDll <CityBuilder DLL> -ReferenceDirectory <game reference folder>` checks the required binary methods and fields without executing the original mod. Neither test is an in-game validation.

Release output and hashes are in `bin/Release`. DEBUG is not defined. No V-Runner code was modified; Full/Light variants do not apply to this separate companion project.

## First in-game acceptance run

- Confirm the BepInEx log contains `CityBuilder POV 0.1.0 ready` and no patch errors.
- Open F6 while walking. Confirm the normal camera stays behind the player, the menu is clickable, and closing it restores looking/movement.
- Select the first warehouse; verify menu click does not place it immediately. Aim, rotate, place, and confirm the original resource deduction and construction behavior.
- Place a hut. Open H management, inspect/upgrade when eligible, and confirm no changes to foreign towns.
- Draw a road from the warehouse to the hut. Check preview, cost, worker connectivity, continued segments, cancel, and persistence after relog.
- Aim at the sky/outside reach/behind an obstruction. Check no placement or stale road preview occurs.
- With a vanilla hammer equipped, confirm a click creates only the selected colony building/road. With a weapon equipped, confirm placement does not attack/block.
- Train soldiers using the barracks panel. J-select, K-move, K-attack a hostile, and use Manage to return them to barracks. Try a foreign squad and confirm it is rejected.
- Test inventory/chat/map/pause, focus loss, death, logout/rejoin, and F7 handoff while a preview/menu is open. Confirm cleanup and ordinary controls afterwards.
- Repeat in a two-player test world. Check original ownership and remote visibility, costs, and save persistence.

Original mod: https://thunderstore.io/c/valheim/p/RustyProductions/ValheimCityBuilder/
