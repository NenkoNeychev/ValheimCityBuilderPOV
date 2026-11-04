# CityBuilder POV context РІР‚вЂќ 2026-10-30

User requested third-person colony building, building management, squad commands, and road placement without F7 overhead view. Implemented independent companion 0.1.0, pinned to CityBuilder 1.2.2. V-Runner Full/Light are unrelated and were not modified or rebuilt.

Evidence source: user-supplied Downloads/RustyProductions-ValheimCityBuilder-1.2.2.zip, SHA256 CF2B89DED3CE25EDBC596637BC69FA713588962BFF79D354E0C9532415E19AB2. Original plugins/ValheimCityBuilder.dll SHA256 98F7B049701C8581F0D11D027E89F4982A70CE05A132F7F6E80055CA7B93B99A. Inspected metadata and IL without executing the original DLL. No upstream source project is provided. The independent companion uses reflection to bind original runtime APIs and Harmony for scoped input/aim hooks; it does not redistribute or rewrite the upstream DLL.

Important inspected behavior: AnnoMode.Update already runs Colony.Update/WorldTick before its Active gate. Its UpdatePlacement and UpdateRoad can be called separately, preserving native validation and costs. DrawPanel can render management UI independently of city view. The companion never assigns AnnoMode.Active or calls SetActive/SetGhostMode. Ground targeting is scoped to the tool tick. No global camera or time/weather flags are modified.

Controls: F6 menu, H building management, J squad selection (Shift adds), K move/attack; native R/T building rotation/design and mouse placement/cancel. Building list includes all unlocked original definitions. Road list excludes terrain-area tools. Manage reuses native DrawPanel. Global research/diplomacy/ship-route UI is outside this first milestone. Native F7 remains available.

Validation: 20 production interaction/reach assertions and 100 binary API contract assertions passed. Release compilation succeeded against existing supplied game references. Tests do not execute Unity, Harmony runtime detours, upstream construction, or multiplayer. No game launch/deployment occurred. See README in-game acceptance checklist. Latest final source/output/reference hashes are recorded by build.ps1 in bin/Release/build-manifest.json.

Remaining: launch in a separate game test profile with CityBuilder 1.2.2; verify runtime patch binding, menu/layout/cursor, collision masks, costs, roadmap connectivity, native management UI, soldiers, and transition cleanup. Verify actual installed game assemblies match the development APIs. Then test two-player ownership/persistence. No repository or deployment path is assigned.


## 0.2.0 update вЂ” 2026-10-30

User requested native hammer/hoe construction menus, rotation, broader gameplay menus, and specifically left-click house management/right-click closing. Implemented independent companion 0.2.0. The user described the earlier result positively; this is not recorded as a comprehensive runtime validation.

New NativeTools.cs injects menu-only Piece templates into hammer/hoe tables, with transient known-recipe copies, original unlock/cost checks, native picker handoff and proxy placement suppression. Ordinary pieces remain available. Ordinary building rotation is wheel/R 22.5 degrees, Shift fine 5 degrees. Coastal automatic alignment deliberately remains original because CoastPlacement updates validated dock geometry; walls/roads use endpoints/grid. Already-placed structures are not reoriented.

Left-click on a visible owned CityBuilder structure opens original management outside native placement mode; right-click closes menus with a release fence. H works while holding a build tool. E falls back to management only on pieces without a native Interactable. F6 includes walls/areas, original research/world/site diplomacy/trade, fleet/ship-route panels and storage. Existing squad controls remain. Menus.cs manages original UI caches while suppressing duplicate rendering. Native E consumption is retained for the complete frame.

Validation for 0.2.0: optimized Release compilation passed; 29 production state/rotation/reach assertions and 170 binary API assertions passed. No game launch, Harmony runtime validation, UI rendering test or multiplayer validation was performed. The earlier milestone limitations above describe 0.1.0 and are superseded by this section. See updated README for controls and the remaining live acceptance checks. Game deployment and GitHub destinations remain unassigned. Previous 0.1.0 release is retained.

Deployment 2026-10-30: User explicitly assigned the Thunderstore V-City profile and requested direct installation. Installed companion 0.2.0 to C:\Users\PC-Admin\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\V-City\BepInEx\plugins\CityBuilderPOV\CityBuilderPOV.dll. Previous companion backed up in C:\Users\PC-Admin\Documents\Projects\Personal\Valheim-CityBuilder-POV\deployment-backups\V-City-20261030-115825. Installed SHA256 verified: 5900CDF0E67E2AAF78B2B47E2897F3340187692205A42F962C112B53698F09E3. No Valheim process was detected before copying. No in-game validation performed. GitHub remains unassigned.


## 0.3.0 menu cleanup — 2026-10-30

User requested removal of the old menu and separation of ordinary Valheim pieces in the native picker. Removed duplicate F6 Buildings/Roads/Walls construction list; F6 is management-only. Suppressed upstream DrawUi overlay in normal third-person mode; original explicit F7 view remains available. House click/right-click behavior is unchanged.

Inspected actual BuildUi and ByUsagePieceList IL: modern picker uses usage tags, not PieceTable category labels. Added scoped ByUsagePieceList hooks providing Town buildings, Town walls, Town roads & terrain and Valheim groups only on injected hammer/hoe tables. Original piece prefab categories, recipes and saves are unchanged. Native All/material/recent/favorites remain intact; repair/remove special actions are retained. Group mappings are cleared when catalog is removed or a different tool is refreshed. No custom enum or global category mutations.

Release build passed, 36 production interaction/category assertions and 177 binary API assertions passed. No runtime validation performed by agent. User reported 0.2.0 was much better; new category layout still requires live testing. Authorized deployment destination remains V-City. Verify category switching across hammer/hoe, vanilla piece selection, Town construction, repair, house management, and F7 handoff after restarting.

Deployed 0.3.0 to assigned V-City profile; previous DLL retained at C:\Users\PC-Admin\Documents\Projects\Personal\Valheim-CityBuilder-POV\deployment-backups\V-City-20261030-120911. Installed SHA256 84B85D5D97D213CFCE85240814E3DE2ED0C07F4C30AD25D5EC776A3EB2C72949 verified. Valheim was not running at pre-deployment check.

Repository assigned by user on 2026-11-04: https://github.com/NenkoNeychev/ValheimCityBuilderPOV.git. User requested the full editable project plus DLL and individual files, excluding ZIP archives. Repository visibility is public as created by the user. Existing compiled/test outputs and unpacked historical releases are included as snapshots; no game or upstream binaries are included.
