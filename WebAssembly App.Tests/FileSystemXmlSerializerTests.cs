using CloudFileManager.Models;
using Xunit;

namespace WebAssembly_App.Tests;

public class FileSystemXmlSerializerTests
{
    [Fact]
    public void SerializeCustom_SingleRootFolder_ShouldUseFolderNameAsRootElement()
    {
        var root = new Folder { Name = "根目錄_Root" };
        root.Children.Add(new WordFile { Name = "需求規格書.docx", PageCount = 10, SizeKB = 500 });

        var xml = FileSystemXmlSerializer.SerializeCustom(new[] { root });

        Assert.Contains("<根目錄_Root>", xml);
        Assert.Contains("</根目錄_Root>", xml);
        Assert.Contains("<需求規格書_docx>", xml);
    }
}