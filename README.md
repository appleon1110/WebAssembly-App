# 專案概述：雲端檔案管理模擬系統 (Blazor WASM)

本專案是一個基於 **Blazor WebAssembly** 的單頁應用程式（SPA），旨在模擬雲端檔案管理系統的核心功能。

## 核心功能

* **樹狀目錄呈現**：直觀顯示資料夾與檔案的層級結構。
* **空間統計**：精確計算目錄總檔案大小。
* **進階搜尋**：支援依副檔名進行篩選。
* **資料匯出**：自訂 XML 序列化功能，產生標準化報表。
* **執行日誌**：即時記錄（Process Logging）系統遍歷與操作過程。

---

## 主要檔案與職責分工

| 檔案名稱 | 職責說明 |
| --- | --- |
| **Models/FileSystem.cs** | 定義核心資料模型。採用 **Composite 模式**區分 Folder 與 File，並實作 `Accept` 方法支援 Visitor 模式。 |
| **Models/Visitors.cs** | 演算法實作區。包含 `SizeCalculator`（大小計算）與 `FileSearcher`（搜尋），支援 Logging 回傳機制。 |
| **Shared/FileNode.razor** | 遞迴 UI 元件。負責渲染樹狀結構，具備擴充展開/摺疊功能的潛力。 |
| **Pages/Home.razor** | 核心 UI 控制器。負責使用者互動、呼叫各類 Visitor、處理 XML 匯出及 Console 資訊顯示。 |
| **Models/FileSystemXmlSerializer.cs** | 自訂 XML 序列化工具。負責將物件轉換為標籤正規化、簡潔易讀的 XML 結構。 |

---

## 設計模式應用

### 1. Composite (組合模式)

* **結構**：`Folder` 類別包含 `List<FileSystemItem>`，可遞迴組合子資料夾或檔案。
* **優勢**：統一了單一檔案與容器資料夾的操作邏輯，利於遞迴遍歷、UI 渲染與序列化。

### 2. Visitor (訪問者模式)

* **結構**：將「演算法」（如計算大小、搜尋、日誌記錄）從「資料結構」（檔案系統）中分離。
* **優勢**：若需新增功能（例如：權限檢查），只需實作新的 `IFileSystemVisitor`，無需修改現有的模型類別。

---

## 資料流 (Data Flow)

1. **觸發操作**：使用者在 `Home.razor` 點擊功能按鈕（計算/搜尋/匯出）。
2. **初始化 Visitor**：`Home.razor` 建立對應的 Visitor 實例，並注入 **Logging Callback** 以接收過程訊息。
3. **執行遍歷**：針對 `rootItems` 呼叫 `Accept(visitor)`。Visitor 開始遞迴遍歷模型樹，執行邏輯並即時回傳執行日誌。
4. **結果呈現**：
* 計算/搜尋/匯出結果直接反映於 UI。

