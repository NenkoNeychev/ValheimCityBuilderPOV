param([Parameter(Mandatory)][string]$ModDll, [Parameter(Mandatory)][string]$ReferenceDirectory)
$ErrorActionPreference='Stop'
$script:checks=0
function Assert([bool]$Condition,[string]$Message) {
    if(-not $Condition){throw $Message}; $script:checks++
}
function Inspect([string]$Path,[hashtable]$Methods,[hashtable]$Fields) {
    $stream=[IO.File]::OpenRead($Path)
    $pe=[System.Reflection.PortableExecutable.PEReader]::new($stream)
    $md=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    try {
        $types=@{}
        foreach($handle in $md.TypeDefinitions){
            $td=$md.GetTypeDefinition($handle);$name=$md.GetString($td.Name)
            if(-not $types.ContainsKey($name) -or $td.GetDeclaringType().IsNil){$types[$name]=$td}
        }
        foreach($name in $Methods.Keys) {
            Assert ($types.ContainsKey($name)) "Type missing: $name"
            $td=$types[$name]
            foreach($spec in $Methods[$name]) {
                $parts=$spec.Split(':');$methodName=$parts[0];$count=[int]$parts[1]
                $found=@(foreach($mh in $td.GetMethods()){
                    $m=$md.GetMethodDefinition($mh)
                    if($md.GetString($m.Name) -eq $methodName){
                        $pars=@(foreach($ph in $m.GetParameters()){$p=$md.GetParameter($ph);if($p.SequenceNumber -gt 0){$md.GetString($p.Name)}})
                        if($pars.Count -eq $count){$m}
                    }
                })
                $expected=if($parts.Length -gt 2){[int]$parts[2]}else{1}
                Assert ($found.Count -eq $expected) "Expected $expected matches for $name.$methodName with $count parameters, found $($found.Count)"
            }
        }
        foreach($name in $Fields.Keys) {
            Assert ($types.ContainsKey($name)) "Field owner type missing: $name"
            $actual=@(foreach($fh in $types[$name].GetFields()){$md.GetString($md.GetFieldDefinition($fh).Name)})
            foreach($field in $Fields[$name]){Assert ($actual -contains $field) "Field missing: $name.$field"}
        }
        if($Methods.ContainsKey('Player')) {
            foreach($mh in $types['Player'].GetMethods()) {
                $m=$md.GetMethodDefinition($mh)
                if($md.GetString($m.Name) -ne 'SetControls'){continue}
                $names=@(foreach($ph in $m.GetParameters()){$md.GetString($md.GetParameter($ph).Name)})
                foreach($n in @('attack','attackHold','secondaryAttack','secondaryAttackHold','block','blockHold')) {
                    Assert ($names -contains $n) "Harmony action parameter missing: $n"
                }
            }
        }
    } finally {$pe.Dispose();$stream.Dispose()}
}
Inspect $ModDll @{
    AnnoMode=@('Update:0','UpdatePlacement:0','UpdateRoad:0','UpdateWall:0','SelectWall:1','RemoteWorldUi:2','get_WorldWindowOpen:0','DrawWorldWindows:0','DrawTopBar:0','DrawTooltip:0','TickMapZoom:0','HideWallLine:0','DrawUi:1','OpenSite:1','OpenShip:1','OpenMerchant:0','RemoteEndTool:0','ClearSelection:0','DrawPanel:0','Select:1','SelectRoad:1','RemoteSelect:1','SelectSquadOf:1','BuildingOf:1','Cam:0','MouseGround:1','MouseOverUI:0','SetActive:1')
    UI=@('Begin:0','End:0'); Military=@('Order:3','Hostile:1')
    In=@('get_LeftDown:0','get_RightDown:0','Pressed:1'); Defs=@('Unlocked:1','CostText:1'); Economy=@('CostOf:1','Has:1'); Icons=@('Piece:1','Item:1'); Walls=@('Unlocked:1'); Ships=@('get_All:0','StatusText:1')
    Soldier=@('get_Valid:0','Barracks:0'); AnnoBuilding=@('get_Colony:0'); Col=@('get_Me:0')
    Util=@('get_GroundMask:0'); PlayerWorker=@('Stop:0'); Roads=@('HidePreview:0'); GridOverlay=@('Hide:0')
} @{
    Plugin=@('Version'); AnnoMode=@('Active','tool','ghost','selSoldiers','hoverText','uiTyping','placeYaw','mapOpen','storageOpen','uiNext','uiRects','tip','tipDef')
    Defs=@('All'); BDef=@('Name','Desc','Anchor'); Walls=@('All'); WDef=@('Name','Desc','Icon','Cost'); ShipData=@('Id','Name'); Roads=@('All'); RDef=@('Area','Name','Desc','Icon','CostItem','CostPerTile'); AnnoBuilding=@('Foreign')
}
Inspect (Join-Path $ReferenceDirectory 'assembly_valheim.dll') @{
    Player=@('SetControls:12','UpdatePlacement:2','SetSelectedPiece:1:2','GetSelectedPiece:0','SetupPlacementGhost:0','TryPlacePiece:1','HaveRequirements:2','UpdateAvailablePiecesList:0'); PieceTable=@('UpdateAvailable:4'); ByUsagePieceList=@('UpdateAvailableTags:1','get_DisplayName:0','get_TagCount:0','GetTagIdByIndex:1','GetTagDisplayName:1','GetAvailablePiecesWithTag:3'); PlayerController=@('TakeInput:1')
    GameCamera=@('UpdateCamera:1','UpdateMouseCapture:0')
    InventoryGui=@('IsVisible:0'); Menu=@('IsVisible:0'); Minimap=@('IsOpen:0'); TextInput=@('IsVisible:0')
    StoreGui=@('IsVisible:0'); Console=@('IsVisible:0'); Hud=@('IsPieceSelectionVisible:0','InRadial:0','TogglePieceSelection:0')
    UnifiedPopup=@('IsVisible:0'); ZNet=@('IsPasswordDialogShowing:0')
} @{ Player=@('m_placementGhost'); PieceTable=@('m_pieces','m_categories'); Piece=@('m_name','m_description','m_category','m_icon','m_enabled','m_resources','m_canRotate','m_canBeRemoved') }
Inspect (Join-Path $ReferenceDirectory 'assembly_utils.dll') @{ZInput=@('GetButtonDown:1','GetMouseScrollWheel:0')} @{}
Write-Output "$script:checks binary API contract assertions passed. Runtime detouring and game behavior remain untested."


