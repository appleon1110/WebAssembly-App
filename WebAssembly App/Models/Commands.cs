using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudFileManager.Models
{
    public interface ICommand
    {
        void Execute();
        void Undo();
    }

    public class AddCommand : ICommand
    {
        private readonly Folder _parent;
        private readonly FileSystemItem _item;

        public AddCommand(Folder parent, FileSystemItem item)
        {
            _parent = parent;
            _item = item;
        }

        public void Execute() => _parent.Children.Add(_item);

        public void Undo() => _parent.Children.Remove(_item);
    }

    public class DeleteCommand : ICommand
    {
        private readonly Folder _parent;
        private readonly FileSystemItem _item;
        private readonly int _index;

        public DeleteCommand(Folder parent, FileSystemItem item, int index)
        {
            _parent = parent;
            _item = item;
            _index = index;
        }

        public void Execute()
        {
            if (_index <= _parent.Children.Count - 1) _parent.Children.RemoveAt(_index);
            else _parent.Children.Remove(_item);
        }

        public void Undo()
        {
            if (_index <= _parent.Children.Count) _parent.Children.Insert(_index, _item);
            else _parent.Children.Add(_item);
        }
    }

    public class TagToggleCommand : ICommand
    {
        private readonly FileSystemItem _item;
        private readonly string _tag;
        private readonly bool _wasPresent;

        public TagToggleCommand(FileSystemItem item, string tag)
        {
            _item = item;
            _tag = tag;
            _wasPresent = item.Tags?.Contains(tag) == true;
        }

        public void Execute()
        {
            if (_item.Tags == null) _item.Tags = new List<string>();
            if (_wasPresent) _item.Tags.Remove(_tag);
            else if (!_item.Tags.Contains(_tag)) _item.Tags.Add(_tag);
        }

        public void Undo()
        {
            if (_item.Tags == null) _item.Tags = new List<string>();
            if (_wasPresent)
            {
                if (!_item.Tags.Contains(_tag)) _item.Tags.Add(_tag);
            }
            else
            {
                _item.Tags.Remove(_tag);
            }
        }
    }

    public class SortCommand : ICommand
    {
        private readonly Folder _folder;
        private readonly List<FileSystemItem> _oldOrder;
        private readonly SortKey _key;
        private readonly bool _asc;

        private readonly SortKey _oldSortKey;
        private readonly bool _oldSortAsc;

        public SortCommand(Folder folder, SortKey key, bool asc, List<FileSystemItem> oldOrder)
        {
            _folder = folder;
            _key = key;
            _asc = asc;
            _oldOrder = new List<FileSystemItem>(oldOrder);

            _oldSortKey = folder.CurrentSortKey;
            _oldSortAsc = folder.IsSortAscending;
        }

        public void Execute()
        {
            IEnumerable<FileSystemItem> sorted = _folder.Children;
            switch (_key)
            {
                case SortKey.Name:
                    sorted = _asc
                        ? _folder.Children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                        : _folder.Children.OrderByDescending(c => c.Name, StringComparer.OrdinalIgnoreCase);
                    break;
                case SortKey.Size:
                    sorted = _asc
                        ? _folder.Children.OrderBy(c => c.SizeKB)
                        : _folder.Children.OrderByDescending(c => c.SizeKB);
                    break;
                case SortKey.Extension:
                    sorted = _asc
                        ? _folder.Children.OrderBy(c => Path.GetExtension(c.Name))
                        : _folder.Children.OrderByDescending(c => Path.GetExtension(c.Name));
                    break;
            }

            _folder.Children = sorted.ToList();
            _folder.CurrentSortKey = _key;
            _folder.IsSortAscending = _asc;
        }

        public void Undo()
        {
            _folder.Children = new List<FileSystemItem>(_oldOrder);
            _folder.CurrentSortKey = _oldSortKey;
            _folder.IsSortAscending = _oldSortAsc;
        }
    }
}