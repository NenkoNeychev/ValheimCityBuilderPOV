param(
    [string]$ReferenceDirectory = (Join-Path $PSScriptRoot '..\Valheim-V-runner\refs'),
    [string]$Compiler = (Join-Path $PSScriptRoot '..\Valheim-V-runner\toolchain\roslyn-4.8.0\tasks\net472\csc.exe')
)
$ErrorActionPreference = 'Stop'
$ReferenceDirectory = (Resolve-Path -LiteralPath $ReferenceDirectory).Path
$Compiler = (Resolve-Path -LiteralPath $Compiler).Path
$outDir = Join-Path $PSScriptRoot 'bin\Release'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$options = @('/nologo','/target:library','/nostdlib+','/langversion:7.2','/optimize+','/deterministic+','/utf8output',('/out:"'+(Join-Path $outDir 'CityBuilderPOV.dll')+'"'))
$references = @(Get-ChildItem -LiteralPath $ReferenceDirectory -Filter '*.dll' | Sort-Object Name)
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | Sort-Object Name)
$options += @($references | ForEach-Object { '/reference:"'+$_.FullName+'"' })
$options += @($sources | ForEach-Object { '"'+$_.FullName+'"' })
$response = Join-Path $outDir 'build.rsp'
[IO.File]::WriteAllLines($response, $options, [Text.UTF8Encoding]::new($false))
& $Compiler /noconfig ('@'+$response) 2>&1 | Tee-Object -FilePath (Join-Path $outDir 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Release compilation failed.' }
$manifest = [ordered]@{
    Configuration='Release'; Version='0.3.0'; Dependency='RustyProductions-ValheimCityBuilder-1.2.2'
    CompiledAtUtc=[DateTime]::UtcNow.ToString('o'); GameValidation='Not performed'
    OutputHash=(Get-FileHash -LiteralPath (Join-Path $outDir 'CityBuilderPOV.dll')).Hash
    Sources=@($sources | ForEach-Object { @{Name=$_.Name; SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash} })
    References=@($references | ForEach-Object { @{Name=$_.Name; SHA256=(Get-FileHash -LiteralPath $_.FullName).Hash} })
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'build-manifest.json') -Encoding utf8
Write-Output 'CityBuilderPOV Release compiled. This is not an in-game validation.'
