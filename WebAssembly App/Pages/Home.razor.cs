using System.Net;
using CloudFileManager.Models;
using Microsoft.AspNetCore.Components;

namespace WebAssembly_App.Pages;

public partial class Home
{
    private const string InitialConsolePlaceholder = "執行結果將顯示於此...";
    private List<FileSystemItem> rootItems = new();
    private string consoleOutput = InitialConsolePlaceholder;
    private string searchExt = ".docx";

    private FileSystemItem? selectedItem;
    private FileSystemItem? clipboardItem;

    // 新增：由 Home 統一管理展開狀態，讓搜尋可控制樹狀展開
    private HashSet<FileSystemItem> _expandedFolders = new();

    // 搜尋命中集合（可同時高亮多個）
    private HashSet<FileSystemItem> _matchedItems = new();

    private Stack<ICommand> _undo = new();
    private Stack<ICommand> _redo = new();

    private readonly Stack<string> _undoActionLabels = new();
    private readonly Stack<string> _redoActionLabels = new();

    // 新增：逐筆輸出控制
    private bool _isTraversing = false;
    private const int TraverseLogDelayMs = 120;

    // 監控用欄位
    private string _currentNode = "-";
    private int _visitedNodes = 0;
    private int _totalNodes = 0;
    private int _progressPercent => _totalNodes == 0 ? 0 : (int)Math.Round((double)_visitedNodes * 100 / _totalNodes);

    // 本輪 Replay 用，避免同名節點永遠選到第一個
    private HashSet<FileSystemItem> _replayVisitedItems = new();

    private Dictionary<string, Queue<FileSystemItem>> _visitingLookup = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Queue<FileSystemItem>> _searchLookup = new(StringComparer.OrdinalIgnoreCase);

    // 取得目前應操作的目錄
    private Folder? GetActiveFolder()
    {
        return selectedItem as Folder ?? FindParentFolder(selectedItem) ?? rootItems.OfType<Folder>().FirstOrDefault();
    }

    private bool IsActiveSort(SortKey key)
    {
        var folder = GetActiveFolder();
        return folder != null && folder.CurrentSortKey == key;
    }

    private bool IsActiveSortAsc()
    {
        var folder = GetActiveFolder();
        return folder != null && folder.IsSortAscending;
    }

    protected override void OnInitialized()
    {
        var root = new Folder { Name = "根目錄_Root" };

        var projectDocs = new Folder { Name = "專案文件_Project_Docs" };
        projectDocs.Children.Add(new WordFile { Name = "需求規格書.docx", PageCount = 35, SizeKB = 500 });
        projectDocs.Children.Add(new WordFile { Name = "API介面定義.docx", PageCount = 12, SizeKB = 120 });
        projectDocs.Children.Add(new ImageFile { Name = "系統架構圖.png", SizeKB = 2048, Width = 1920, Height = 1080 });

        var personal = new Folder { Name = "個人筆記_Personal_Notes" };
        personal.Children.Add(new TextFile { Name = "待辦清單.txt", SizeKB = 1, Encoding = "UTF-8" });

        var archive = new Folder { Name = "Archive_2025" };
        archive.Children.Add(new WordFile { Name = "會議記錄.docx", SizeKB = 200, PageCount = 5 });
        personal.Children.Add(archive);

        root.Children.Add(projectDocs);
        root.Children.Add(personal);
        root.Children.Add(new TextFile { Name = "README.txt", SizeKB = 0.5, Encoding = "ASCII" });

        rootItems.Add(root);
    }

    private void OnSelectItem(FileSystemItem? item)
    {
        selectedItem = item;
        StateHasChanged();
    }

    private async Task RunSizeCalc()
    {
        var entries = new List<(string Message, bool IsMatch)>();

        void LogHandler(string message, bool isMatch)
        {
            entries.Add((message, isMatch));
        }

        _isTraversing = true;
        _currentNode = "-";
        _visitedNodes = 0;
        _totalNodes = 0;

        _matchedItems.Clear();

        await InvokeAsync(StateHasChanged);

        try
        {
            var calc = new SizeCalculator((m, b) => LogHandler(m, b));
            foreach (var item in rootItems) item.Accept(calc);

            entries.Add(($"[計算完成] 系統總容量: {calc.TotalSize} KB", false));
            PrepareObserver(entries);
            BuildReplayLookup();
            await ReplayLogsAsync(entries);
        }
        finally
        {
            _isTraversing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RunSearch()
    {
        var entries = new List<(string Message, bool IsMatch)>();

        void LogHandler(string message, bool isMatch)
        {
            entries.Add((message, isMatch));
        }

        if (string.IsNullOrWhiteSpace(searchExt) || !searchExt.StartsWith("."))
        {
            AppendConsoleHtml(BuildLogHtml("❌ 請輸入有效的副檔名格式（例如：.docx）", false, "text-danger"));
            await InvokeAsync(StateHasChanged);
            return;
        }

        _isTraversing = true;
        _currentNode = "-";
        _visitedNodes = 0;
        _totalNodes = 0;

        _matchedItems.Clear();
        selectedItem = null;

        await InvokeAsync(StateHasChanged);

        try
        {
            var searcher = new FileSearcher(searchExt, (m, b) => LogHandler(m, b));
            foreach (var item in rootItems) item.Accept(searcher);

            entries.Add(($"[搜尋結果: {searchExt}]", false));
            if (searcher.FoundPaths.Any())
            {
                foreach (var p in searcher.FoundPaths) entries.Add(($"[找到] {p}", false));
                entries.Add(($"找到 {searcher.FoundPaths.Count} 項", false));
            }
            else
            {
                entries.Add(("找不到符合的檔案", false));
            }

            PrepareObserver(entries);
            BuildReplayLookup();
            await ReplayLogsAsync(entries);
        }
        finally
        {
            _isTraversing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ReplayLogsAsync(List<(string Message, bool IsMatch)> entries)
    {
        _replayVisitedItems.Clear();

        foreach (var (message, isMatch) in entries)
        {
            UpdateObserverFromMessage(message);
            ApplyTraversalFocusFromMessage(message);
            ApplySearchFocusFromMessage(message);

            AppendConsoleHtml(BuildLogHtml(message, isMatch));
            await InvokeAsync(StateHasChanged);
            await Task.Delay(TraverseLogDelayMs);
        }
    }

    private static bool TryExtractTraversalPath(string message, out string path)
    {
        path = string.Empty;

        const string folderPrefix = "搜尋目錄:";
        const string filePrefix = "掃描檔案:";

        if (message.StartsWith(folderPrefix, StringComparison.Ordinal))
        {
            path = message[folderPrefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(path);
        }

        if (message.StartsWith(filePrefix, StringComparison.Ordinal))
        {
            path = message[filePrefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(path);
        }

        return false;
    }

    private void ApplyTraversalFocusFromMessage(string message)
    {
        if (!TryExtractTraversalPath(message, out var path)) return;
        if (!_visitingLookup.TryGetValue(path, out var q) || q.Count == 0) return;

        var item = q.Dequeue();
        ExpandAncestors(item);
        selectedItem = item;
    }

    private void ApplySearchFocusFromMessage(string message)
    {
        if (!TryExtractMatchedPath(message, out var path)) return;
        if (!_searchLookup.TryGetValue(path, out var q) || q.Count == 0) return;

        var item = q.Dequeue();
        ExpandAncestors(item);
        selectedItem = item;
        _matchedItems.Add(item);
    }

    private static bool TryExtractMatchedPath(string message, out string path)
    {
        path = string.Empty;

        const string matchPrefix = "[符合]";

        // 與監控同步：遍歷當下命中就立刻觸发
        if (message.StartsWith(matchPrefix, StringComparison.Ordinal))
        {
            path = message[matchPrefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(path);
        }

        return false;
    }


    private static string BuildLogHtml(string message, bool isMatch, string? extraCssClass = null)
    {
        var encoded = WebUtility.HtmlEncode(message);
        var classAttr = string.IsNullOrWhiteSpace(extraCssClass) ? "" : $" class=\"{extraCssClass}\"";

        if (isMatch)
            return $"<div{classAttr}><span class=\"text-success\">{encoded}</span></div>";

        return $"<div{classAttr}>{encoded}</div>";
    }

    private void RunExportXml()
    {
        try
        {
            var xml = FileSystemXmlSerializer.SerializeCustom(rootItems);
            var encodedXml = WebUtility.HtmlEncode(xml);
            AppendConsoleHtml($"<div><pre>{encodedXml}</pre></div>");
        }
        catch (Exception ex)
        {
            var err = WebUtility.HtmlEncode($"[匯出失敗] {ex.Message}");
            AppendConsoleHtml($"<div>{err}</div>");
        }
    }

    private void AppendConsoleHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return;

        if (consoleOutput == InitialConsolePlaceholder)
            consoleOutput = string.Empty;

        consoleOutput += html;
    }

    private void Copy()
    {
        if (selectedItem == null)
        {
            LogCommand("複製失敗(未選取)");
            return;
        }

        clipboardItem = FileSystemCloner.Clone(selectedItem);
        LogCommand($"複製項目({selectedItem.Name})");
    }

    private void Paste()
    {
        if (clipboardItem == null)
        {
            LogCommand("貼上失敗(剪貼簿為空)");
            return;
        }

        var targetFolder = selectedItem as Folder ?? FindParentFolder(selectedItem) ?? rootItems.OfType<Folder>().FirstOrDefault();
        if (targetFolder == null)
        {
            LogCommand("貼上失敗(找不到目標資料夾)");
            return;
        }

        var clone = FileSystemCloner.Clone(clipboardItem);
        var cmd = new AddCommand(targetFolder, clone);

        var actionLabel = $"貼上項目({clone.Name})";
        ExecuteCommand(cmd, actionLabel);
        LogCommand(actionLabel);
    }

    private void DeleteSelected()
    {
        if (selectedItem == null)
        {
            LogCommand("刪除失敗(未選取)");
            return;
        }

        var parent = FindParentFolder(selectedItem);
        if (parent == null)
        {
            LogCommand("刪除失敗(找不到父層)");
            return;
        }

        var index = parent.Children.IndexOf(selectedItem);
        if (index < 0)
        {
            LogCommand("刪除失敗(索引不存在)");
            return;
        }

        var deletingName = selectedItem.Name;
        var cmd = new DeleteCommand(parent, selectedItem, index);
        ExecuteCommand(cmd, "刪除項目");
        selectedItem = null;

        LogCommand($"刪除項目({deletingName})");
    }

    private void ToggleTagOnSelected(string tag)
    {
        if (selectedItem == null)
        {
            LogCommand($"標籤操作失敗({tag})");
            return;
        }

        var hadTag = selectedItem.Tags?.Contains(tag) == true;
        var actionLabel = hadTag ? $"移除標籤({tag})" : $"貼上標籤({tag})";

        var cmd = new TagToggleCommand(selectedItem, tag);
        ExecuteCommand(cmd, actionLabel);
        LogCommand(actionLabel);
    }

    private void SortSelected(SortKey key, bool ascending)
    {
        Folder? folder = selectedItem as Folder;
        if (folder == null) folder = FindParentFolder(selectedItem) ?? rootItems.OfType<Folder>().FirstOrDefault();
        if (folder == null)
        {
            LogCommand("排序失敗(找不到資料夾)");
            return;
        }

        var oldOrder = folder.Children.ToList();
        var cmd = new SortCommand(folder, key, ascending, oldOrder);

        var sortText = ToSortText(key);
        var directionText = ascending ? "遞增" : "遞減";
        var actionLabel = $"{sortText} 排序{directionText}";

        ExecuteCommand(cmd, actionLabel);
        LogCommand(actionLabel);
    }

    // 計算系統中標籤數量（給工具列上的徽章）
    private int CountTag(string tag)
    {
        int count = 0;
        foreach (var root in rootItems)
        {
            CountTagRecursive(root, tag, ref count);
        }
        return count;
    }

    private void CountTagRecursive(FileSystemItem item, string tag, ref int count)
    {
        if (item.Tags?.Contains(tag) == true) count++;

        if (item is Folder f)
        {
            foreach (var c in f.Children)
            {
                CountTagRecursive(c, tag, ref count);
            }
        }
    }

    private void ExecuteCommand(ICommand cmd, string actionLabel)
    {
        cmd.Execute();
        _undo.Push(cmd);
        _redo.Clear();

        _undoActionLabels.Push(actionLabel);
        _redoActionLabels.Clear();

        StateHasChanged();
    }

    private void Undo()
    {
        if (!_undo.Any()) return;

        var cmd = _undo.Pop();
        cmd.Undo();
        _redo.Push(cmd);

        var actionLabel = _undoActionLabels.Count > 0 ? _undoActionLabels.Pop() : "操作";
        _redoActionLabels.Push(actionLabel);

        LogUndo(actionLabel);
        StateHasChanged();
    }

    private void Redo()
    {
        if (!_redo.Any()) return;

        var cmd = _redo.Pop();
        cmd.Execute();
        _undo.Push(cmd);

        var actionLabel = _redoActionLabels.Count > 0 ? _redoActionLabels.Pop() : "操作";
        _undoActionLabels.Push(actionLabel);

        LogRedo(actionLabel);
        StateHasChanged();
    }

    private Folder? FindParentFolder(FileSystemItem? target)
    {
        if (target == null) return null;
        foreach (var root in rootItems)
        {
            var found = FindParentRecursive(root as Folder, target);
            if (found != null) return found;
        }
        return null;
    }

    private Folder? FindParentRecursive(Folder? current, FileSystemItem target)
    {
        if (current == null) return null;
        for (int i = 0; i < current.Children.Count; i++)
        {
            if (ReferenceEquals(current.Children[i], target)) return current;
            if (current.Children[i] is Folder f)
            {
                var inner = FindParentRecursive(f, target);
                if (inner != null) return inner;
            }
        }
        return null;
    }

    private void PrepareObserver(List<(string Message, bool IsMatch)> entries)
    {
        _visitedNodes = 0;
        _totalNodes = entries.Count(e => TryExtractTraversalPath(e.Message, out _));
        _currentNode = "-";
    }

    private void UpdateObserverFromMessage(string message)
    {
        if (!TryExtractTraversalPath(message, out var path)) return;

        _visitedNodes++;
        _currentNode = ExtractNodeName(path);
    }

    private static string ExtractNodeName(string traversalPath)
    {
        if (string.IsNullOrWhiteSpace(traversalPath)) return "-";

        var parts = traversalPath.Split("->", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? traversalPath : parts[^1];
    }

    private void BuildReplayLookup()
    {
        _visitingLookup.Clear();
        _searchLookup.Clear();

        foreach (var item in rootItems)
        {
            BuildReplayLookupRecursive(item, "", "");
        }
    }

    private void BuildReplayLookupRecursive(FileSystemItem item, string visitingParent, string slashParent)
    {
        var visitingPath = string.IsNullOrEmpty(visitingParent) ? item.Name : $"{visitingParent} -> {item.Name}";
        Enqueue(_visitingLookup, visitingPath, item);

        var slashPath = string.IsNullOrEmpty(slashParent) ? item.Name : $"{slashParent}/{item.Name}";
        if (item is not Folder) Enqueue(_searchLookup, slashPath, item);

        if (item is Folder f)
        {
            foreach (var child in f.Children)
            {
                BuildReplayLookupRecursive(child, visitingPath, slashPath);
            }
        }
    }

    private static void Enqueue(Dictionary<string, Queue<FileSystemItem>> map, string key, FileSystemItem item)
    {
        if (!map.TryGetValue(key, out var q))
        {
            q = new Queue<FileSystemItem>();
            map[key] = q;
        }
        q.Enqueue(item);
    }

    private void ExpandAncestors(FileSystemItem item)
    {
        if (item is Folder f) _expandedFolders.Add(f);

        var parent = FindParentFolder(item);
        while (parent != null)
        {
            _expandedFolders.Add(parent);
            parent = FindParentFolder(parent);
        }
    }

    private static string ToSortText(SortKey key) => key switch
    {
        SortKey.Name => "名稱",
        SortKey.Size => "大小",
        SortKey.Extension => "類型",
        _ => "預設"
    };

    private void LogCommand(string message)
    {
        AppendConsoleHtml(BuildLogHtml($"[Command]執行 {message}", false));
    }

    private void LogUndo(string message)
    {
        AppendConsoleHtml(BuildLogHtml($"[Undo]恢復 {message}", false, "text-warning"));
    }

    private void LogRedo(string message)
    {
        AppendConsoleHtml(BuildLogHtml($"[Redo]重做 {message}", false, "text-info"));
    }
}