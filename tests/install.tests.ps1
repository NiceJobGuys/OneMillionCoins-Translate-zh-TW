param(
    [Parameter(Mandatory)][string]$PackageDir,
    [Parameter(Mandatory)][string]$OriginalPath,
    [Parameter(Mandatory)][string]$ScratchDir
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $ScratchDir) { throw '測試目錄必須尚未存在。' }
$managed = Join-Path $ScratchDir 'game\One Million Coins_Data\Managed'
New-Item -ItemType Directory -Path $managed -Force | Out-Null
$package = Join-Path $ScratchDir 'package'
Copy-Item -LiteralPath $PackageDir -Destination $package -Recurse
$game = Join-Path $ScratchDir 'game'
$target = Join-Path $managed 'Assembly-CSharp.dll'
Copy-Item -LiteralPath $OriginalPath -Destination $target
$manifest = Get-Content -LiteralPath "$package\manifest.json" -Raw | ConvertFrom-Json
function AssertHash([string]$expected) {
    if ((Get-FileHash -LiteralPath $target).Hash -ne $expected) { throw '測試遊戲檔案與預期不符。' }
}
& "$package\Install.ps1" -GameDir $game
AssertHash $manifest.patched_sha256
if ((Get-FileHash -LiteralPath "$package\backups\Assembly-CSharp.dll").Hash -ne $manifest.original_sha256) { throw '備份不符。' }
& "$package\Install.ps1" -GameDir $game
AssertHash $manifest.patched_sha256
& "$package\Install.ps1" -GameDir $game -Restore
AssertHash $manifest.original_sha256
& "$package\Install.ps1" -GameDir $game -Restore
AssertHash $manifest.original_sha256
[IO.File]::WriteAllText($target, 'incompatible game version')
$before = (Get-FileHash -LiteralPath $target).Hash
$rejected = $false
try { & "$package\Install.ps1" -GameDir $game } catch { $rejected = $true }
if (!$rejected) { throw '未拒絕不相容版本。' }
AssertHash $before
Copy-Item -LiteralPath $OriginalPath -Destination $target -Force
$payload = Join-Path $package 'bin\Assembly-CSharp.dll'
[IO.File]::WriteAllText($payload, 'corrupt payload')
$rejected = $false
try { & "$package\Install.ps1" -GameDir $game } catch { $rejected = $true }
if (!$rejected) { throw '未拒絕損壞的補丁。' }
AssertHash $manifest.original_sha256
Write-Output 'PASS: install, original backup, idempotence, restore, incompatible game and corrupt payload rejection.'
