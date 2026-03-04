# 雲端檔案管理模擬系統 (Blazor WebAssembly)

本專案為以 `Blazor WebAssembly` 實作的雲端檔案管理模擬系統，示範樹狀目錄顯示、遍歷演算法（Visitor）、大小計算、依副檔名搜尋、Tag 管理與自訂 XML 匯出，以及操作日誌（造訪路徑 / traversal log）展示。

---

## 核心功能
- 樹狀目錄呈現（遞迴元件：`Shared/FileNode.razor`）
- 空間統計（`Models/Visitors.cs` → `SizeCalculator`）
- 進階搜尋（`Models/Visitors.cs` → `FileSearcher`，依副檔名）
- 自訂 XML 匯出（`Models/FileSystemXmlSerializer.cs`）
- 執行日誌（造訪路徑 / traversal log，顯示於頁面右側 Console）

---

## 已實作之進階功能（Bonus）
- 排序：可依「名稱 / 大小 / 副檔名」升/降序（在工具列按鈕）  
- 編輯：複製 / 貼上 / 刪除（單一項目）  
- 標籤（Tag）：支援多重標籤，預設三種顏色
  - `Urgent` → 紅色（Bootstrap `bg-danger`）
  - `Work`   → 藍色（Bootstrap `bg-primary`）
  - `Personal` → 綠色（Bootstrap `bg-success`）
- 狀態管理：Undo / Redo（以 Command Pattern 實作）

---

## 主要檔案與職責
- `Models/FileSystem.cs`：模型（`FileSystemItem`／`Folder`／`WordFile`／`ImageFile`／`TextFile`），含 `Tags` 屬性與 `Accept`。  
- `Models/Visitors.cs`：Visitor 演算法（`SizeCalculator`、`FileSearcher`），支援 logging callback。  
- `Shared/FileNode.razor`：遞迴 UI（顯示檔案、標籤 badge、選取、展開）。  
- `Pages/Home.razor`：工具列、命令管理（Undo/Redo）、呼叫 Visitor 與 XML 匯出、顯示 Console。  
- `Models/FileSystemXmlSerializer.cs`：自訂 XML 序列化（標籤正規化、保留中文）。

---

## UI 操作說明（按鈕對應）
- Undo / Redo：回復或重做最後一個命令（新增、刪除、標籤、排序）。  
- Sort（名稱 / 大小 / 副檔名）：對當前選取之資料夾（或其 parent）排序。  
- Copy / Paste：複製整個節點（含子節點），貼到選取資料夾或選取項目的父資料夾。  
- Delete：刪除選取項目（支援 Undo）。  
- Tag buttons：切換 Urgent / Work / Personal（三色）；可套多重標籤。
- 計算總大小
- 搜尋
- 匯出XML
---

## Visitor / 造訪路徑
- 在 Console 中顯示。
