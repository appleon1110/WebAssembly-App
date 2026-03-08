using CloudFileManager.Models;
using Xunit;

namespace WebAssembly_App.Tests;

public class CopyTests
{
    [Fact(DisplayName = "Copy: 深拷貝後修改副本不影響原物件")]
    public void Clone_ShouldBeDeepCopy()
    {
        var source = new Folder { Name = "Root" };
        source.Children.Add(new TextFile { Name = "a.txt", SizeKB = 1 });

        var clone = FileSystemCloner.Clone(source); // 建議抽出公用 Cloner

        ((TextFile)((Folder)clone).Children[0]).Name = "changed.txt";

        Assert.Equal("a.txt", ((TextFile)source.Children[0]).Name);
    }
}