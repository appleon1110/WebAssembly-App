using System.Collections.Generic;

namespace CloudFileManager.Models
{
    public static class FileSystemCloner
    {
        public static FileSystemItem Clone(FileSystemItem src)
        {
            if (src is Folder f)
            {
                var nf = new Folder { Name = f.Name, SizeKB = f.SizeKB, CreatedDate = f.CreatedDate };
                nf.Tags = new List<string>(f.Tags ?? new());
                foreach (var c in f.Children) nf.Children.Add(Clone(c));
                return nf;
            }

            if (src is WordFile w)
            {
                return new WordFile
                {
                    Name = w.Name,
                    SizeKB = w.SizeKB,
                    CreatedDate = w.CreatedDate,
                    PageCount = w.PageCount,
                    Tags = new List<string>(w.Tags ?? new())
                };
            }

            if (src is ImageFile i)
            {
                return new ImageFile
                {
                    Name = i.Name,
                    SizeKB = i.SizeKB,
                    CreatedDate = i.CreatedDate,
                    Width = i.Width,
                    Height = i.Height,
                    Tags = new List<string>(i.Tags ?? new())
                };
            }

            if (src is TextFile t)
            {
                return new TextFile
                {
                    Name = t.Name,
                    SizeKB = t.SizeKB,
                    CreatedDate = t.CreatedDate,
                    Encoding = t.Encoding,
                    Tags = new List<string>(t.Tags ?? new())
                };
            }

            return new TextFile
            {
                Name = src.Name,
                SizeKB = src.SizeKB,
                CreatedDate = src.CreatedDate,
                Tags = new List<string>(src.Tags ?? new())
            };
        }
    }
}