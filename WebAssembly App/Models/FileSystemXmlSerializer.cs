// Models/FileSystemXmlSerializer.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CloudFileManager.Models
{
    public static class FileSystemXmlSerializer
    {
        public static string SerializeCustom(IEnumerable<FileSystemItem> items)
        {
            var list = (items ?? Enumerable.Empty<FileSystemItem>()).ToList();

            if (list.Count == 1 && list[0] is Folder singleRoot)
            {
                var rootEl = SerializeItem(singleRoot);
                return rootEl.ToString();
            }

            var container = new XElement("Export");
            foreach (var item in list)
            {
                container.Add(SerializeItem(item));
            }

            return container.ToString();
        }

        private static XElement SerializeItem(FileSystemItem item)
        {
            if (item is Folder folder)
            {
                // 以 folder.Name 作為標籤（做最小正規化，不使用 XmlConvert.EncodeName）
                var tag = NormalizeName(folder.Name);
                var el = new XElement(tag);
                foreach (var child in folder.Children)
                {
                    el.Add(SerializeItem(child));
                }
                return el;
            }
            else
            {
                var fileName = item.Name ?? "";
                var ext = Path.GetExtension(fileName).TrimStart('.').Replace('.', '_');
                var baseName = Path.GetFileNameWithoutExtension(fileName);
                var tagRaw = string.IsNullOrEmpty(ext) ? baseName : $"{baseName}_{ext}";
                var tag = NormalizeName(tagRaw);

                string content = item switch
                {
                    WordFile w => $"頁數: {w.PageCount}, 大小: {FormatSize(w.SizeKB)}",
                    ImageFile i => $"解析度: {i.Width}x{i.Height}, 大小: {FormatSize(i.SizeKB)}",
                    TextFile t => $"編碼: {t.Encoding}, 大小: {FormatSize(t.SizeKB)}",
                    _ => $"大小: {FormatSize(item.SizeKB)}"
                };

                return new XElement(tag, content);
            }
        }

        // 最小正規化：保留中文與英文、數字、底線、連字號與句點；空白轉底線；若以數字開頭則加前綴 "_"
        private static string NormalizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Item";

            // 空白改為底線
            var name = raw.Replace(" ", "_");

            // 移除不允許的字元（允許 Unicode 字母、數字、底線、連字號、句點）
            name = Regex.Replace(name, @"[^\p{L}\p{N}_\-\.\uFF00-\uFFFF]", string.Empty);

            // 若第一個字為數字，加入前綴以避免 XML 名稱規則問題
            if (name.Length > 0 && char.IsDigit(name[0]))
            {
                name = "_" + name;
            }

            // 若處理後為空，回傳 Item
            return string.IsNullOrEmpty(name) ? "Item" : name;
        }

        private static string FormatSize(double sizeKB)
        {
            // 十進位換算：1 MB = 1000 KB, 1 KB = 1000 B
            if (sizeKB >= 1000)
            {
                var mb = Math.Round(sizeKB / 1000.0);
                return $"{mb}MB";
            }
            else if (sizeKB >= 1)
            {
                var kb = Math.Round(sizeKB);
                return $"{kb}KB";
            }
            else
            {
                var bytes = Math.Round(sizeKB * 1000.0);
                return $"{bytes}B";
            }
        }
    }
}