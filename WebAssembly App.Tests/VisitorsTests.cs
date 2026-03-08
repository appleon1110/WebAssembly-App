using CloudFileManager.Models;
using Xunit;

namespace WebAssembly_App.Tests;

public class VisitorsTests
{
    [Fact(DisplayName = "SizeCalculator: Root/Sub 結構總容量應為 121.5KB")]
    public void SizeCalculator_ShouldSumAllFileSizes()
    {
        var root = new Folder { Name = "Root" };
        root.Children.Add(new WordFile { Name = "a.docx", SizeKB = 100 });
        root.Children.Add(new TextFile { Name = "b.txt", SizeKB = 1.5 });

        var sub = new Folder { Name = "Sub" };
        sub.Children.Add(new ImageFile { Name = "c.png", SizeKB = 20 });
        root.Children.Add(sub);

        var calc = new SizeCalculator();
        root.Accept(calc);

        Assert.Equal(121.5, calc.TotalSize, 3);
    }

    [Fact(DisplayName = "FileSearcher: 搜尋 .docx 應找到 2 筆且保留遍歷順序")]
    public void FileSearcher_ShouldFindCaseInsensitiveExtension()
    {
        var root = new Folder { Name = "Root" };
        root.Children.Add(new WordFile { Name = "Spec.DOCX", SizeKB = 10 });
        root.Children.Add(new WordFile { Name = "Note.docx", SizeKB = 20 });
        root.Children.Add(new TextFile { Name = "Readme.txt", SizeKB = 1 });

        var searcher = new FileSearcher(".docx");
        root.Accept(searcher);

        Assert.Equal(2, searcher.FoundPaths.Count);
        Assert.Equal("Root/Spec.DOCX", searcher.FoundPaths[0]);
        Assert.Equal("Root/Note.docx", searcher.FoundPaths[1]);
    }
}