# 雲端檔案管理模擬系統 (Blazor WebAssembly)

本專案以 `Blazor WebAssembly` 實作，主題為「雲端檔案管理系統」。


## 🚀 快速檢視

- 🌐 **Web Demo**：<https://appleon1110.github.io/WebAssembly-App/>
- ✅ **CI / Automated Tests**：<https://github.com/appleon1110/WebAssembly-App/actions/workflows/ci.yml>
[![CI](https://github.com/appleon1110/WebAssembly-App/actions/workflows/ci.yml/badge.svg)](https://github.com/appleon1110/WebAssembly-App/actions/workflows/ci.yml)
- Latest test console log: https://raw.githubusercontent.com/appleon1110/WebAssembly-App/reports/test/latest.log
- Latest test TRX: https://raw.githubusercontent.com/appleon1110/WebAssembly-App/reports/test/latest.trx
  
| Web 展示 | 自動化測試 |
|---|---|
| <img width="1747" height="825" alt="image" src="https://github.com/user-attachments/assets/5b0248bc-5f68-4e33-984f-803918df7da5" />|<img width="1026" height="999" alt="image" src="https://github.com/user-attachments/assets/50549e5a-c66a-45ab-ba59-b50517da2e37" />|

重點示範：

- 樹狀結構（Composite）
- 遍歷演算法（Visitor）
- 操作命令與回復（Command / Undo / Redo）
- 監控日誌與進度顯示（Traverse Log + Observer UI）
- 自訂 XML 匯出
- 自動化測試（xUnit + GitHub Actions）

---

## 1. 專案核心功能

- 樹狀目錄呈現（遞迴元件：`Shared/FileNode.razor`）
- 計算總大小（`SizeCalculator`）
- 依副檔名搜尋（`FileSearcher`）
- 標籤管理（Urgent / Work / Personal）
- 複製、貼上、刪除
- 排序（名稱 / 大小 / 類型）
- XML 匯出
- Undo / Redo

---

## 2. 設計模式使用說明

### 2.1 Composite（檔案樹）
`FileSystemItem` 為基底，`Folder` 可包含子節點，`WordFile/ImageFile/TextFile` 為葉節點。  
可用統一介面遞迴操作整棵樹。

### 2.2 Visitor（遍歷）
`SizeCalculator` 與 `FileSearcher` 透過 `Accept/Visit` 遍歷。  
每造訪節點都產生 `Visiting: ...` 日誌，用於證明遍歷順序。

### 2.3 Command（可回復操作）
新增、刪除、標籤切換、排序都封裝為 Command，支援 Undo / Redo。  
命令已抽離至 `Models/Commands.cs`，便於單元測試。

---

## 3. 監控（Observer-like UI）如何達成

在 `Home.razor` 中，執行搜尋/計算時會：

1. 建立 log entries（來自 Visitor callback）
2. 計算總節點數（`PrepareObserver`）
3. 逐筆播放日誌（`ReplayLogsAsync`）
4. 每筆更新：
   - 目前節點 `_currentNode`
   - 已訪問數 `_visitedNodes`
   - 百分比 `_progressPercent`
5. 透過 `StateHasChanged` + `Task.Delay` 形成即時監控效果

---

## 4. 主要檔案職責

- `Models/FileSystem.cs`：模型與 `SortKey`
- `Models/Visitors.cs`：`SizeCalculator` / `FileSearcher`
- `Models/FileSystemXmlSerializer.cs`：自訂 XML 序列化
- `Models/Commands.cs`：`Add/Delete/TagToggle/Sort` 命令
- `Models/FileSystemCloner.cs`：深拷貝（Copy/Paste）
- `Shared/FileNode.razor`：遞迴目錄 UI
- `Pages/Home.razor`：主頁面、命令管理、監控 UI、Console 顯示
- `Pages/Home.razor.css`：頁面樣式與比例配置
- `WebAssembly App.Tests/*`：自動化測試

---

## 5. UI 操作對應

- `計算總大小`：遍歷整棵樹並累加大小
- `副檔名搜尋`：遍歷並比對副檔名
- `匯出 XML`：輸出目前樹狀資料
- `Undo / Redo`：回復/重做操作
- `排序`：對目前資料夾進行排序
- `Tag`：切換標籤

### 5.1 Home 工具列 RWD（本次更新）

針對 `Pages/Home.razor` 的上方工具列，已新增手機版（`max-width: 576px`）排版優化：

- 手機版固定為四行顯示（每種類型功能一行）：
  1. `重做 / 撤銷`
  2. `複製 / 貼上 / 刪除`
  3. `排序`
  4. `標籤`
- 排序區前新增功能符號（`↕️`）。
- 手機版**保留功能符號顯示**（避免看不出功能）。
- 手機版僅隱藏工具列分隔線（`toolbar-divider`），不隱藏功能符號。
- 手機版關閉桌面按鈕拼接效果，避免換行時邊框與圓角破版。

相關檔案：
- `Pages/Home.razor`
- `Pages/Home.razor.css`

### 5.2 搜尋/計算時的樹狀同步與同名檔修正

針對 `Pages/Home.razor` 與 `Shared/FileNode.razor`，已補強遍歷時的 UI 同步行為：

- `計算總大小`：
  - 依 `Visiting:` 日誌逐筆移動焦點（動畫感）
  - 會自動展開祖先資料夾

- `副檔名搜尋`：
  - 依 `[符合]` 日誌逐筆移動焦點
  - 會自動展開祖先資料夾
  - 保留多筆命中高亮標記（`HighlightedItems`）

- 修正同名檔案問題：
  - 新增 replay lookup（`_visitingLookup` / `_searchLookup`）
  - 改用 queue 逐筆對應實際節點，避免同名時永遠命中第一個

- 修正重複搜尋同條件失效問題：
  - 每次執行前重建 lookup（`BuildReplayLookup()`）
  - 避免 queue 被前一次 `Dequeue()` 用盡

相關檔案：
- `Pages/Home.razor`
- `Shared/FileNode.razor`
- `Models/Visitors.cs`

---



## 6. 執行環境

- .NET 8
- Blazor WebAssembly
- C# 12

---

## 7. Automated Testing

本專案已加入 `xUnit` 單元測試，並透過 GitHub Actions 自動執行。

### 測試分類（13 項）

#### A. Visitor 測試（2）
1. `SizeCalculator: Root/Sub 結構總容量應為 121.5KB`
2. `FileSearcher: 搜尋 .docx 應找到 2 筆且保留遍歷順序`

#### B. Command 測試（9）
1. `DeleteCommand: Execute 刪除，Undo 還原原位置`
2. `TagToggleCommand: Execute + Undo 應可切換並還原標籤`
3. `Redo: 撤銷後重做應重新套用上一個命令`
4. `SortCommand ...`（Name ASC）
5. `SortCommand ...`（Name DESC）
6. `SortCommand ...`（Size ASC）
7. `SortCommand ...`（Size DESC）
8. `SortCommand ...`（Extension ASC）
9. `SortCommand ...`（Extension DESC）

#### C. Copy / Clone 測試（1）
1. `Copy/Paste: 深拷貝後修改副本不影響原物件，且可貼上到目標資料夾`

#### D. XML Serializer 測試（1）
1. `XML匯出: 顯示資料夾結構與輸出XML`

### CI 會做什麼（`.github/workflows/ci.yml`）

1. 還原與建置 solution
2. 執行 `dotnet test "WebAssembly App.Tests/WebAssembly App.Tests.csproj"`
3. 輸出詳細 log（`console;verbosity=detailed`）
4. 產生 `trx` 測試報告
5. 上傳 Artifact（`test-results`）
6. 在 PR 顯示 `Unit Test Report`


