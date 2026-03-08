using CloudFileManager.Models;
using Xunit;

namespace WebAssembly_App.Tests;

public class CommandTests
{
    [Fact(DisplayName = "DeleteCommand: Execute 刪除，Undo 還原原位置")]
    public void DeleteCommand_ShouldExecuteAndUndo()
    {
        var folder = new Folder { Name = "Root" };
        var a = new TextFile { Name = "a.txt", SizeKB = 1 };
        var b = new TextFile { Name = "b.txt", SizeKB = 2 };
        folder.Children.Add(a);
        folder.Children.Add(b);

        var cmd = new DeleteCommand(folder, a, 0);
        cmd.Execute();

        Assert.Single(folder.Children);
        Assert.Equal("b.txt", folder.Children[0].Name);

        cmd.Undo();

        Assert.Equal(2, folder.Children.Count);
        Assert.Equal("a.txt", folder.Children[0].Name);
        Assert.Equal("b.txt", folder.Children[1].Name);
    }

    public static IEnumerable<object[]> SortCases()
    {
        yield return new object[] { SortKey.Name, true,  new[] { "a.docx", "b.txt", "c.png" } };
        yield return new object[] { SortKey.Name, false, new[] { "c.png", "b.txt", "a.docx" } };

        yield return new object[] { SortKey.Size, true,  new[] { "a.docx", "c.png", "b.txt" } };
        yield return new object[] { SortKey.Size, false, new[] { "b.txt", "c.png", "a.docx" } };

        yield return new object[] { SortKey.Extension, true,  new[] { "a.docx", "c.png", "b.txt" } };
        yield return new object[] { SortKey.Extension, false, new[] { "b.txt", "c.png", "a.docx" } };
    }

    [Theory(DisplayName = "SortCommand: 各排序條件與方向都應正確，且 Undo 可還原")]
    [MemberData(nameof(SortCases))]
    public void SortCommand_ShouldSortAndUndo(
        SortKey key,
        bool asc,
        string[] expectedOrder)
    {
        var folder = new Folder { Name = "Root" };
        folder.Children.Add(new TextFile { Name = "b.txt", SizeKB = 20 });
        folder.Children.Add(new WordFile { Name = "a.docx", SizeKB = 10 });
        folder.Children.Add(new ImageFile { Name = "c.png", SizeKB = 15 });

        var originalOrder = folder.Children.Select(x => x.Name).ToArray();

        var cmd = new SortCommand(folder, key, asc, folder.Children.ToList());
        cmd.Execute();

        Assert.Equal(expectedOrder, folder.Children.Select(x => x.Name).ToArray());
        Assert.Equal(key, folder.CurrentSortKey);
        Assert.Equal(asc, folder.IsSortAscending);

        cmd.Undo();

        Assert.Equal(originalOrder, folder.Children.Select(x => x.Name).ToArray());
        Assert.Equal(SortKey.Default, folder.CurrentSortKey);
        Assert.True(folder.IsSortAscending);
    }
}