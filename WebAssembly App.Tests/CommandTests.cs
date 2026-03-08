using CloudFileManager.Models;
using Xunit;
using Xunit.Abstractions;

namespace WebAssembly_App.Tests;

public class CommandTests
{
    private readonly ITestOutputHelper _output;

    public CommandTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "DeleteCommand: Execute 刪除，Undo 還原原位置")]
    public void DeleteCommand_ShouldExecuteAndUndo()
    {
        var folder = new Folder { Name = "Root" };
        var a = new TextFile { Name = "a.txt", SizeKB = 1 };
        var b = new TextFile { Name = "b.txt", SizeKB = 2 };
        folder.Children.Add(a);
        folder.Children.Add(b);

        _output.WriteLine("=== 刪除前結構 ===");
        _output.WriteLine(DescribeChildren(folder));

        var cmd = new DeleteCommand(folder, a, 0);
        cmd.Execute();

        _output.WriteLine("=== 刪除後結構 ===");
        _output.WriteLine(DescribeChildren(folder));

        Assert.Single(folder.Children);
        Assert.Equal("b.txt", folder.Children[0].Name);

        cmd.Undo();

        _output.WriteLine("=== Undo 還原後結構 ===");
        _output.WriteLine(DescribeChildren(folder));

        Assert.Equal(2, folder.Children.Count);
        Assert.Equal("a.txt", folder.Children[0].Name);
        Assert.Equal("b.txt", folder.Children[1].Name);
    }

    [Fact(DisplayName = "TagToggleCommand: Execute + Undo 應可切換並還原標籤")]
    public void TagToggleCommand_ExecuteAndUndo_ShouldWork()
    {
        var item = new TextFile { Name = "note.txt", SizeKB = 1, Tags = new List<string>() };

        _output.WriteLine("=== Case 1: 原本無 Tag，Execute 後應新增，Undo 後應移除 ===");
        _output.WriteLine($"Before: [{string.Join(", ", item.Tags)}]");

        var addTagCmd = new TagToggleCommand(item, "Urgent");
        addTagCmd.Execute();

        _output.WriteLine($"After Execute: [{string.Join(", ", item.Tags)}]");
        Assert.Contains("Urgent", item.Tags);

        addTagCmd.Undo();
        _output.WriteLine($"After Undo: [{string.Join(", ", item.Tags)}]");
        Assert.DoesNotContain("Urgent", item.Tags);

        _output.WriteLine("=== Case 2: 原本有 Tag，Execute 後應移除，Undo 後應還原 ===");
        item.Tags.Add("Work");
        _output.WriteLine($"Before: [{string.Join(", ", item.Tags)}]");

        var removeTagCmd = new TagToggleCommand(item, "Work");
        removeTagCmd.Execute();

        _output.WriteLine($"After Execute: [{string.Join(", ", item.Tags)}]");
        Assert.DoesNotContain("Work", item.Tags);

        removeTagCmd.Undo();
        _output.WriteLine($"After Undo: [{string.Join(", ", item.Tags)}]");
        Assert.Contains("Work", item.Tags);
    }

    [Fact(DisplayName = "Redo: 撤銷後重做應重新套用上一個命令")]
    public void Redo_ShouldReapplyLastUndoneCommand()
    {
        var folder = new Folder { Name = "Root" };
        folder.Children.Add(new TextFile { Name = "a.txt", SizeKB = 1 });

        var undo = new Stack<ICommand>();
        var redo = new Stack<ICommand>();

        void ExecuteCommand(ICommand cmd)
        {
            cmd.Execute();
            undo.Push(cmd);
            redo.Clear();
        }

        void UndoCommand()
        {
            if (!undo.Any()) return;
            var cmd = undo.Pop();
            cmd.Undo();
            redo.Push(cmd);
        }

        void RedoCommand()
        {
            if (!redo.Any()) return;
            var cmd = redo.Pop();
            cmd.Execute();
            undo.Push(cmd);
        }

        _output.WriteLine("=== 初始 ===");
        _output.WriteLine(DescribeChildren(folder));

        var addCmd = new AddCommand(folder, new TextFile { Name = "b.txt", SizeKB = 2 });
        ExecuteCommand(addCmd);

        _output.WriteLine("=== Execute(Add) 後 ===");
        _output.WriteLine(DescribeChildren(folder));
        Assert.Equal(2, folder.Children.Count);

        UndoCommand();

        _output.WriteLine("=== Undo 後 ===");
        _output.WriteLine(DescribeChildren(folder));
        Assert.Single(folder.Children);
        Assert.Equal("a.txt", folder.Children[0].Name);

        RedoCommand();

        _output.WriteLine("=== Redo 後 ===");
        _output.WriteLine(DescribeChildren(folder));
        Assert.Equal(2, folder.Children.Count);
        Assert.Equal("b.txt", folder.Children[1].Name);
    }

    public static IEnumerable<object[]> SortCases()
    {
        yield return new object[] { SortKey.Name, true, new[] { "a.docx", "b.txt", "c.png" } };
        yield return new object[] { SortKey.Name, false, new[] { "c.png", "b.txt", "a.docx" } };

        yield return new object[] { SortKey.Size, true, new[] { "a.docx", "c.png", "b.txt" } };
        yield return new object[] { SortKey.Size, false, new[] { "b.txt", "c.png", "a.docx" } };

        yield return new object[] { SortKey.Extension, true, new[] { "a.docx", "c.png", "b.txt" } };
        yield return new object[] { SortKey.Extension, false, new[] { "b.txt", "c.png", "a.docx" } };
    }

    [Theory(DisplayName = "SortCommand: 各排序條件與方向都應正確，且 Undo 可還原")]
    [MemberData(nameof(SortCases))]
    public void SortCommand_ShouldSortAndUndo(SortKey key, bool asc, string[] expectedOrder)
    {
        var folder = new Folder { Name = "Root" };
        folder.Children.Add(new TextFile { Name = "b.txt", SizeKB = 20 });
        folder.Children.Add(new WordFile { Name = "a.docx", SizeKB = 10 });
        folder.Children.Add(new ImageFile { Name = "c.png", SizeKB = 15 });

        var originalOrder = folder.Children.Select(x => x.Name).ToArray();

        _output.WriteLine($"=== 原始排序 [{key}, {(asc ? "ASC" : "DESC")}] ===");
        _output.WriteLine(DescribeChildren(folder));

        var cmd = new SortCommand(folder, key, asc, folder.Children.ToList());
        cmd.Execute();

        _output.WriteLine($"=== 排序後 [{key}, {(asc ? "ASC" : "DESC")}] ===");
        _output.WriteLine(DescribeChildren(folder));

        Assert.Equal(expectedOrder, folder.Children.Select(x => x.Name).ToArray());
        Assert.Equal(key, folder.CurrentSortKey);
        Assert.Equal(asc, folder.IsSortAscending);

        cmd.Undo();

        _output.WriteLine("=== Undo 後 ===");
        _output.WriteLine(DescribeChildren(folder));

        Assert.Equal(originalOrder, folder.Children.Select(x => x.Name).ToArray());
        Assert.Equal(SortKey.Default, folder.CurrentSortKey);
        Assert.True(folder.IsSortAscending);
    }

    private static string DescribeChildren(Folder folder)
    {
        return string.Join(
            Environment.NewLine,
            folder.Children.Select((x, i) => $"{i + 1}. {x.Name} | {x.SizeKB}KB"));
    }
}