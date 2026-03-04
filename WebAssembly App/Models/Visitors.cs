// Models/Visitors.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudFileManager.Models
{
    // 1. 計算總容量 (現在支援 logging with isMatch flag)
    public class SizeCalculator : IFileSystemVisitor
    {
        public double TotalSize { get; private set; }

        private readonly Action<string, bool>? _log;
        private readonly Stack<string> _path = new();

        public SizeCalculator(Action<string, bool>? log = null)
        {
            _log = log;
        }

        public void Visit(Folder folder)
        {
            _path.Push(folder.Name);
            _log?.Invoke($"Visiting: {string.Join(" -> ", _path.Reverse())}", false);
            foreach (var child in folder.Children) child.Accept(this);
            _path.Pop();
        }

        public void Visit(WordFile file)
        {
            _log?.Invoke($"Visiting: {string.Join(" -> ", _path.Reverse())} -> {file.Name}", false);
            TotalSize += file.SizeKB;
        }

        public void Visit(ImageFile file)
        {
            _log?.Invoke($"Visiting: {string.Join(" -> ", _path.Reverse())} -> {file.Name}", false);
            TotalSize += file.SizeKB;
        }

        public void Visit(TextFile file)
        {
            _log?.Invoke($"Visiting: {string.Join(" -> ", _path.Reverse())} -> {file.Name}", false);
            TotalSize += file.SizeKB;
        }
    }

    // 2. 副檔名搜尋 (現在支援 logging with isMatch flag)
    public class FileSearcher : IFileSystemVisitor
    {
        private readonly string _ext;
        public List<string> FoundPaths { get; } = new();
        private readonly Stack<string> _pathStack = new();
        private readonly Action<string, bool>? _log;

        public FileSearcher(string extension, Action<string, bool>? log = null)
        {
            _ext = extension?.ToLower() ?? "";
            _log = log;
        }

        public void Visit(Folder folder)
        {
            _pathStack.Push(folder.Name);
            _log?.Invoke($"Visiting: {string.Join(" -> ", _pathStack.Reverse())}", false);
            foreach (var child in folder.Children) child.Accept(this);
            _pathStack.Pop();
        }

        public void Visit(WordFile file)
        {
            _log?.Invoke($"Visiting: {string.Join(" -> ", _pathStack.Reverse())} -> {file.Name}", false);
            CheckFile(file.Name);
        }

        public void Visit(ImageFile file)
        {
            _log?.Invoke($"Visiting: {string.Join(" -> ", _pathStack.Reverse())} -> {file.Name}", false);
            CheckFile(file.Name);
        }

        public void Visit(TextFile file)
        {
            _log?.Invoke($"Visiting: {string.Join(" -> ", _pathStack.Reverse())} -> {file.Name}", false);
            CheckFile(file.Name);
        }

        private void CheckFile(string fileName)
        {
            if (fileName.ToLower().EndsWith(_ext))
            {
                var fullPath = string.Join("/", _pathStack.Reverse()) + "/" + fileName;
                FoundPaths.Add(fullPath);
                // 傳 isMatch = true
                _log?.Invoke($"[符合] {fullPath}", true);
            }
        }
    }
}