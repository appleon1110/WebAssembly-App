using CloudFileManager.Models;
using Xunit;
using Xunit.Abstractions;

namespace WebAssembly_App.Tests;

public class VisitorsTests
{
    private readonly ITestOutputHelper _output;

    public VisitorsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "SizeCalculator: Root/Sub 結構總容量應為 121.5KB")]
    public void SizeCalculator_ShouldSumAllFileSizes()
    {
        var root = new Folder { Name = "Root" };
        root.Children.Add(new WordFile { Name = "a.docx", SizeKB = 100 });
        root.Children.Add(new TextFile { Name = "b.txt", SizeKB = 1.5 });

        var sub = new Folder { Name = "Sub" };
        sub.Children.Add(new ImageFile { Name = "c.png", SizeKB = 20 });
        root.Children.Add(sub);

        _output.WriteLine("=== 所有文件與大小 ===");
        foreach (var (path, size) in FlattenFiles(root))
        {
            _output.WriteLine($"{path} | {size}KB");
        }

        var visitLogs = new List<string>();
        var calc = new SizeCalculator((m, _) => visitLogs.Add(m));
        root.Accept(calc);

        _output.WriteLine("=== Traversal Logs (SizeCalculator) ===");
        foreach (var log in visitLogs)
        {
            _output.WriteLine(log);
        }

        _output.WriteLine($"總容量(Actual): {calc.TotalSize}KB");
        Assert.Equal(121.5, calc.TotalSize, 3);
    }

    [Fact(DisplayName = "FileSearcher: 搜尋 .docx 應找到 2 筆且保留遍歷順序")]
    public void FileSearcher_ShouldFindCaseInsensitiveExtension()
    {
        var root = new Folder { Name = "Root" };
        root.Children.Add(new WordFile { Name = "Spec.DOCX", SizeKB = 10 });
        root.Children.Add(new WordFile { Name = "Note.docx", SizeKB = 20 });
        root.Children.Add(new TextFile { Name = "Readme.txt", SizeKB = 1 });

        _output.WriteLine("=== 搜尋前所有文件 ===");
        foreach (var (path, size) in FlattenFiles(root))
        {
            _output.WriteLine($"{path} | {size}KB");
        }

        var visitLogs = new List<string>();
        var searcher = new FileSearcher(".docx", (m, _) => visitLogs.Add(m));
        root.Accept(searcher);

        _output.WriteLine("=== Traversal Logs (FileSearcher) ===");
        foreach (var log in visitLogs)
        {
            _output.WriteLine(log);
        }

        _output.WriteLine("=== 搜尋結果 ===");
        foreach (var path in searcher.FoundPaths)
        {
            _output.WriteLine(path);
        }

        Assert.Equal(2, searcher.FoundPaths.Count);
        Assert.Equal("Root/Spec.DOCX", searcher.FoundPaths[0]);
        Assert.Equal("Root/Note.docx", searcher.FoundPaths[1]);
    }

    private static IEnumerable<(string Path, double SizeKB)> FlattenFiles(Folder root)
    {
        var stack = new Stack<(FileSystemItem Item, string Path)>();
        stack.Push((root, root.Name));

        while (stack.Count > 0)
        {
            var (item, path) = stack.Pop();

            if (item is Folder f)
            {
                for (int i = f.Children.Count - 1; i >= 0; i--)
                {
                    var child = f.Children[i];
                    stack.Push((child, $"{path}/{child.Name}"));
                }
            }
            else
            {
                yield return (path, item.SizeKB);
            }
        }
    }
}