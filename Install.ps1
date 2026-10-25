param(
    [Parameter(Mandatory)][string]$GameDir,
    [switch]$Restore
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'One Million Coins' -ErrorAction SilentlyContinue) { throw '請先正常離開 One Million Coins。' }
$manifest = Get-Content -LiteralPath "$PSScriptRoot\manifest.json" -Raw | ConvertFrom-Json
if ($manifest.original_sha256 -notmatch '^[A-F0-9]{64}$' -or $manifest.patched_sha256 -notmatch '^[A-F0-9]{64}$') { throw '補丁 manifest 格式不符。' }
$target = Join-Path $GameDir 'One Million Coins_Data\Managed\Assembly-CSharp.dll'
$payload = Join-Path $PSScriptRoot 'bin\Assembly-CSharp.dll'
$backup = Join-Path $PSScriptRoot 'backups\Assembly-CSharp.dll'
if ((Get-FileHash -LiteralPath $payload).Hash -ne $manifest.patched_sha256) { throw '補丁雜湊不符。' }
$expectedCurrent = if ($Restore) { $manifest.patched_sha256 } else { $manifest.original_sha256 }
$expectedResult = if ($Restore) { $manifest.original_sha256 } else { $manifest.patched_sha256 }
$current = (Get-FileHash -LiteralPath $target).Hash
if ($current -eq $expectedResult) { Write-Output '已是指定版本，未變更檔案。'; return }
if ($current -ne $expectedCurrent) { throw '遊戲版本不相容或另有修改，已停止操作。' }
if (!$Restore -and !(Test-Path -LiteralPath $backup)) {
    New-Item -ItemType Directory -Path (Split-Path $backup) -Force | Out-Null
    Copy-Item -LiteralPath $target -Destination $backup
}
if ((Get-FileHash -LiteralPath $backup).Hash -ne $manifest.original_sha256) { throw '原檔備份雜湊不符。' }
$source = if ($Restore) { $backup } else { $payload }
try {
    Copy-Item -LiteralPath $source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $target).Hash -ne $expectedResult) { throw '寫入後雜湊不符。' }
}
catch {
    $rollback = if ($Restore) { $payload } else { $backup }
    Copy-Item -LiteralPath $rollback -Destination $target -Force
    if ((Get-FileHash -LiteralPath $target).Hash -ne $expectedCurrent) { throw "復原未通過驗證，請保留備份並手動還原：$backup" }
    throw
}
if ($Restore) { Write-Output '已還原原版，SHA-256 驗證通過。' }
else { Write-Output '繁體中文已套用，SHA-256 驗證通過。請在遊戲內選擇「繁體中文」。' }
