# One Million Coins 繁體中文化

為 One Million Coins 新增獨立的「繁體中文」語言選項，保留英文、德文、俄文與簡體中文。共 1,558 筆翻譯，以原版簡中轉換並調整臺灣常用詞。

遊戲連結：[One Million Coins — Steam 商店](https://store.steampowered.com/app/5258340/One_Million_Coins/)。

本專案公開提供原始碼與翻譯，安裝包放在 Releases；下載不需要私人專案邀請或 GitHub 登入。沿用 CoA 中文化的命名格式。

1.0.0 安裝包內 README 的私人專案存取說明已不適用；套用與還原步驟不變。

## 下載與套用

1. 從 [最新版本](https://github.com/NiceJobGuys/OneMillionCoins-Translate-zh-TW/releases/latest) 下載 `OneMillionCoins-zhTW-1.0.0.zip`，完整解壓。
2. 安裝 PowerShell 7，正常離開遊戲。
3. 在解壓後的目錄執行下列指令，將遊戲路徑改成自己的安裝位置：

```powershell
pwsh -NoProfile -File .\Install.ps1 -GameDir "D:\Games\Steam\steamapps\common\One Million Coins"
```

4. 啟動遊戲，在設定的語言選單選擇「繁體中文」。

工具只替換 `One Million Coins_Data\Managed\Assembly-CSharp.dll`，自動在安裝包的 `backups` 目錄保留使用者自己的原檔；不變更存檔或玩法。請保留安裝包及其 `backups`。

## 還原與更新

先離開遊戲，再執行：

```powershell
pwsh -NoProfile -File .\Install.ps1 -GameDir "D:\Games\Steam\steamapps\common\One Million Coins" -Restore
```

本版僅支援 `manifest.json` 列出的原檔 SHA-256。遊戲更新或驗證檔案可能覆蓋中文化；遇到不同版本，工具會停止，不會強制套用舊 DLL。請等待相容補丁，勿手動略過版本檢查。

## 原始碼與翻譯

- `translations/zh-Hant.json`：1,521 筆主表。
- `translations/zh-Hant-additions.json`：37 筆補充。
- `src/PatchTraditionalChinese.cs`：語言註冊、翻譯載入、中文字型判定與按鍵提示修補。
- `src/build.ps1`：從玩家持有的相容原版 DLL 重新建置，需要 PowerShell 7 與自行取得的 Mono.Cecil。
- `Install.ps1`：版本檢查、備份、套用與還原。
- `tests/install.tests.ps1`：在指定測試目錄驗證套用、重複執行、還原與不相容版本拒絕。

重新建置範例：

```powershell
pwsh -NoProfile -File .\src\build.ps1 -GameDir "D:\Games\Steam\steamapps\common\One Million Coins" -CecilPath "D:\Tools\Mono.Cecil.dll"
```

建置時必須保留完整的遊戲 `Managed` 目錄，不能只提供單一遊戲 DLL；產物放在 `build/Assembly-CSharp.dll`，不覆蓋現有安裝包。若修改翻譯，發布前須更新 `bin` 中的產物及 `manifest.json` 的 `patched_sha256`。

安裝包已有建置好的 DLL，使用者不需要 Mono.Cecil。Repo 不包含遊戲原版備份、完整遊戲或存檔；遊戲及原始翻譯的權利屬原權利人。本專案為非官方中文化。

## 驗證與限制

原中文化已驗證 1,558 筆翻譯、參數與標記、動態字串、語言選項、翻譯快取及字型字元涵蓋。發布版安裝工具另以隔離檔案驗證；本次未啟動遊戲檢查畫面，也未逐句人工潤稿。
