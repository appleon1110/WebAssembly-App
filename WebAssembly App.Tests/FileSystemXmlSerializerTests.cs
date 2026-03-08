using System.Text;
using CloudFileManager.Models;
using Xunit;
using Xunit.Abstractions;

namespace WebAssembly_App.Tests;

public class FileSystemXmlSerializerTests
{
    private readonly ITestOutputHelper _output;

    public FileSystemXmlSerializerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "XML匯出: 顯示資料夾結構與輸出XML")]
    public void SerializeCustom_SingleRootFolder_ShouldUseFolderNameAsRootElement()
    {
        var root = new Folder { Name = "根目錄_Root" };

        var docs = new Folder { Name = "專案文件_Project_Docs" };
        docs.Children.Add(new WordFile { Name = "需求規格書.docx", PageCount = 10, SizeKB = 500 });
        docs.Children.Add(new TextFile { Name = "README.txt", Encoding = "UTF-8", SizeKB = 1.2 });

        root.Children.Add(docs);
        root.Children.Add(new ImageFile { Name = "架構圖.png", Width = 1920, Height = 1080, SizeKB = 2048 });

        _output.WriteLine("=== 資料夾結構 ===");
        _output.WriteLine(DescribeTree(root));

        var xml = FileSystemXmlSerializer.SerializeCustom(new[] { root });

        _output.WriteLine("=== 匯出 XML ===");
        _output.WriteLine(xml);

        Assert.Contains("<根目錄_Root>", xml);
        Assert.Contains("</根目錄_Root>", xml);
        Assert.Contains("<需求規格書_docx>", xml);
    }

    private static string DescribeTree(FileSystemItem item, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        var sb = new StringBuilder();

        if (item is Folder folder)
        {
            sb.AppendLine($"{indent}[Folder] {folder.Name}");
            foreach (var child in folder.Children)
            {
                sb.Append(DescribeTree(child, depth + 1));
            }
        }
        else
        {
            sb.AppendLine($"{indent}- [File] {item.Name} ({item.SizeKB}KB)");
        }

        return sb.ToString();
    }
}