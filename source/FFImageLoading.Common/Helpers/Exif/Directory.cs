using System;
using System.Collections.Generic;

namespace FFImageLoading.Helpers.Exif
{
    public abstract class Directory
    {
        private readonly Dictionary<int, object> _tagMap = new Dictionary<int, object>();

        private readonly List<Tag> _definedTagList = new List<Tag>();

        private readonly List<string> _errorList = new List<string>(capacity: 4);

        private ITagDescriptor _descriptor;

        public abstract string Name { get; }

        public Directory Parent { get; internal set; }

        protected abstract bool TryGetTagName(int tagType, out string tagName);

        public bool IsEmpty => _errorList.Count == 0 && _definedTagList.Count == 0;

        public bool ContainsTag(int tagType) => _tagMap.ContainsKey(tagType);

        public IReadOnlyList<Tag> Tags => _definedTagList;

        public int TagCount => _definedTagList.Count;

        internal void SetDescriptor(ITagDescriptor descriptor)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        }

        internal void AddError(string message) => _errorList.Add(message);

        public bool HasError => _errorList.Count > 0;

        public IReadOnlyList<string> Errors => _errorList;

        internal virtual void Set(int tagType, object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!_tagMap.ContainsKey(tagType))
                _definedTagList.Add(new Tag(tagType, this));

            _tagMap[tagType] = value;
        }

        public object GetObject(int tagType)
        {
            return _tagMap.TryGetValue(tagType, out object val) ? val : null;
        }

        public string GetTagName(int tagType)
        {
            return !TryGetTagName(tagType, out string name)
                ? $"Unknown tag (0x{tagType:x4})"
                : name;
        }

        public bool HasTagName(int tagType) => TryGetTagName(tagType, out string _);

        public string GetValue(int tagType)
        {
            return _descriptor.GetDescription(tagType);
        }

        public override string ToString() => $"{Name} Directory ({_tagMap.Count} {(_tagMap.Count == 1 ? "tag" : "tags")})";
    }

    internal sealed class ErrorDirectory : Directory
    {
        public override string Name => "Error";

        public ErrorDirectory() { }

        public ErrorDirectory(string error) => AddError(error);

        protected override bool TryGetTagName(int tagType, out string tagName)
        {
            tagName = null;
            return false;
        }

        internal override void Set(int tagType, object value) => throw new NotSupportedException($"Cannot add values to {nameof(ErrorDirectory)}.");
    }
}
