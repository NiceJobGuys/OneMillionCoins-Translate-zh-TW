param(
    [Parameter(Mandatory)][string]$GameDir,
    [Parameter(Mandatory)][string]$CecilPath
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$manifest = Get-Content -LiteralPath "$root\manifest.json" -Raw | ConvertFrom-Json
$source = Join-Path $GameDir 'One Million Coins_Data\Managed\Assembly-CSharp.dll'
if ((Get-FileHash -LiteralPath $source).Hash -ne $manifest.original_sha256) { throw '請使用相容的原版遊戲檔案建置。' }
Add-Type -Path $CecilPath
Add-Type -Path "$PSScriptRoot\PatchTraditionalChinese.cs" -ReferencedAssemblies $CecilPath,'netstandard','System.Linq','System.Console','System.Collections'
New-Item -ItemType Directory -Path "$root\build" -Force | Out-Null
[PatchTraditionalChinese]::Build($source, "$root\translations\zh-Hant.json", "$root\translations\zh-Hant-additions.json", "$root\build\Assembly-CSharp.dll")
Get-FileHash -LiteralPath "$root\build\Assembly-CSharp.dll"
