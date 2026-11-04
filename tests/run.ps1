param([string]$Compiler = (Join-Path $PSScriptRoot '..\..\Valheim-V-runner\toolchain\roslyn-4.8.0\tasks\net472\csc.exe'))
$ErrorActionPreference='Stop'
$out = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$exe=Join-Path $out 'InteractionTests.exe'
& $Compiler /nologo /noconfig /target:exe /langversion:7.2 /optimize+ ('/out:'+$exe) (Join-Path $PSScriptRoot '..\src\InteractionState.cs') (Join-Path $PSScriptRoot '..\src\CatalogGroups.cs') (Join-Path $PSScriptRoot 'InteractionTests.cs')
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
& $exe | Tee-Object -FilePath (Join-Path $out 'results.txt')
if($LASTEXITCODE -ne 0){throw 'Tests failed'}
