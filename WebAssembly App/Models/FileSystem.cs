// Models/FileSystem.cs
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CloudFileManager.Models
{
    // 將 SortKey 列舉宣告在這裡，方便大家共用
    public enum SortKey { Default, Name, Size, Extension }

    // 訪問者介面
    public interface IFileSystemVisitor
    {
        void Visit(Folder folder);
        void Visit(WordFile file);
        void Visit(ImageFile file);
        void Visit(TextFile file);
    }

    // 抽象基類 (Component)
    [XmlInclude(typeof(Folder))]
    [XmlInclude(typeof(WordFile))]
    [XmlInclude(typeof(ImageFile))]
    [XmlInclude(typeof(TextFile))]
    public abstract class FileSystemItem
    {
        [XmlElement("Name")]
        public string Name { get; set; } = "";

        [XmlElement("SizeKB")]
        public double SizeKB { get; set; }

        [XmlElement("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Tags 屬性（支援多重標籤）
        [XmlArray("Tags")]
        [XmlArrayItem("Tag")]
        public List<string> Tags { get; set; } = new();

        public abstract void Accept(IFileSystemVisitor visitor);
    }

    // 目錄 (Composite)
    [XmlType("Folder")]
    public class Folder : FileSystemItem
    {
        // 🌟 新增的屬性，用來把排序狀態記憶在資料夾上
        public SortKey CurrentSortKey { get; set; } = SortKey.Default; 
        public bool IsSortAscending { get; set; } = true;

        [XmlArray("Children")]
        [XmlArrayItem("FileSystemItem")]
        public List<FileSystemItem> Children { get; set; } = new();

        public override void Accept(IFileSystemVisitor visitor) => visitor.Visit(this);
    }

    // 各種檔案 (Leafs)
    [XmlType("WordFile")]
    public class WordFile : FileSystemItem
    {
        [XmlElement("PageCount")]
        public int PageCount { get; set; }
        public override void Accept(IFileSystemVisitor visitor) => visitor.Visit(this);
    }

    [XmlType("ImageFile")]
    public class ImageFile : FileSystemItem
    {
        [XmlElement("Width")]
        public int Width { get; set; }

        [XmlElement("Height")]
        public int Height { get; set; }

        public override void Accept(IFileSystemVisitor visitor) => visitor.Visit(this);
    }

    [XmlType("TextFile")]
    public class TextFile : FileSystemItem
    {
        [XmlElement("Encoding")]
        public string Encoding { get; set; } = "UTF-8";
        public override void Accept(IFileSystemVisitor visitor) => visitor.Visit(this);
    }
}