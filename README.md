# 雲端檔案管理模擬系統 (Blazor WebAssembly)

本專案以 `Blazor WebAssembly` 實作，主題為「雲端檔案管理系統」。

[![Live Demo](https://img.shields.io/badge/Live-Demo-2ea44f?style=for-the-badge&logo=github)](https://appleon1110.github.io/WebAssembly-App/)

👉 線上展示：<https://appleon1110.github.io/WebAssembly-App/>

重點示範：

- 樹狀結構（Composite）
- 遍歷演算法（Visitor）
- 操作命令與回復（Command / Undo / Redo）
- 監控日誌與進度顯示（Traverse Log + Observer UI）
- 自訂 XML 匯出

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
- `Shared/FileNode.razor`：遞迴目錄 UI
- `Pages/Home.razor`：主頁面、命令管理、監控 UI、Console 顯示
- `Pages/Home.razor.css`：頁面樣式與比例配置

---

## 5. UI 操作對應

- `計算總大小`：遍歷整棵樹並累加大小
- `副檔名搜尋`：遍歷並比對副檔名
- `匯出 XML`：輸出目前樹狀資料
- `Undo / Redo`：回復/重做操作
- `排序`：對目前資料夾進行排序
- `Tag`：切換標籤

---

## 6. 作業要求對照（Traverse Log）

需求：執行「計算大小」或「搜尋」時，Console 顯示訪問節點順序。  
本專案已實作，輸出範例：

`Visiting: Root -> Project_Docs -> 需求規格書.docx`

並搭配右側監控面板顯示目前節點與進度。

---

## 7. 執行環境

- .NET 8
- Blazor WebAssembly
- C# 12
