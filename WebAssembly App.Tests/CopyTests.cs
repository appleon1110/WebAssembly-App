using CloudFileManager.Models;
using Xunit;
using Xunit.Abstractions;

namespace WebAssembly_App.Tests;

public class CopyTests
{
    private readonly ITestOutputHelper _output;

    public CopyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Copy/Paste: 深拷貝後修改副本不影響原物件，且可貼上到目標資料夾")]
    public void Clone_AndPaste_ShouldBeDeepCopyAndIndependent()
    {
        var source = new Folder { Name = "Source" };
        source.Children.Add(new TextFile { Name = "a.txt", SizeKB = 1 });

        var target = new Folder { Name = "Target" };

        _output.WriteLine("=== 原始 Source ===");
        _output.WriteLine(DescribeFolder(source));
        _output.WriteLine("=== 貼上前 Target ===");
        _output.WriteLine(DescribeFolder(target));

        var copied = FileSystemCloner.Clone(source);     // Copy
        var pasted = FileSystemCloner.Clone(copied);     // Paste 進目標前再 clone 一份
        var addCmd = new AddCommand(target, pasted);
        addCmd.Execute();

        ((TextFile)((Folder)pasted).Children[0]).Name = "changed.txt";

        _output.WriteLine("=== 貼上後 Target ===");
        _output.WriteLine(DescribeFolder(target));
        _output.WriteLine("=== 修改貼上副本後 Source ===");
        _output.WriteLine(DescribeFolder(source));

        Assert.Equal("a.txt", ((TextFile)source.Children[0]).Name);
        Assert.Single(target.Children);
        Assert.Equal("changed.txt", ((TextFile)((Folder)target.Children[0]).Children[0]).Name);
    }

    private static string DescribeFolder(Folder folder)
    {
        var lines = new List<string> { $"[{folder.Name}]" };
        foreach (var child in folder.Children)
        {
            if (child is Folder f)
            {
                lines.Add($"- Folder: {f.Name} (Children={f.Children.Count})");
            }
            else
            {
                lines.Add($"- File: {child.Name} ({child.SizeKB}KB)");
            }
        }
        return string.Join(Environment.NewLine, lines);
    }
}