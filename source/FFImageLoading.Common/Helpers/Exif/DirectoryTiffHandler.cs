using System;
using System.Collections.Generic;
using System.Linq;

namespace FFImageLoading.Helpers.Exif
{
    internal abstract class DirectoryTiffHandler : ITiffHandler
    {
        private readonly Stack<Directory> _directoryStack = new Stack<Directory>();

        protected List<Directory> Directories { get; }

        protected Directory CurrentDirectory { get; private set; }

        protected DirectoryTiffHandler(List<Directory> directories)
        {
            Directories = directories;
        }

        public void EndingIfd()
        {
            CurrentDirectory = _directoryStack.Count == 0 ? null : _directoryStack.Pop();
        }

        protected void PushDirectory(Directory directory)
        {
            // If this is the first directory, don't add to the stack
            if (CurrentDirectory != null)
            {
                _directoryStack.Push(CurrentDirectory);
                directory.Parent = CurrentDirectory;
            }
            CurrentDirectory = directory;
            Directories.Add(CurrentDirectory);
        }

        public void Warn(string message) => GetCurrentOrErrorDirectory().AddError(message);
        public void Error(string message) => GetCurrentOrErrorDirectory().AddError(message);

        private Directory GetCurrentOrErrorDirectory()
        {
            if (CurrentDirectory != null)
                return CurrentDirectory;
            var error = Directories.OfType<ErrorDirectory>().FirstOrDefault();
            if (error != null)
                return error;
            error = new ErrorDirectory();
            PushDirectory(error);
            return error;
        }

        public void SetByteArray(int tagId, byte[] bytes) => CurrentDirectory.Set(tagId, bytes);
        public void SetString(int tagId, StringValue stringValue) => CurrentDirectory.Set(tagId, stringValue);
        public void SetRational(int tagId, Rational rational) => CurrentDirectory.Set(tagId, rational);
        public void SetRationalArray(int tagId, Rational[] array) => CurrentDirectory.Set(tagId, array);
        public void SetFloat(int tagId, float float32) => CurrentDirectory.Set(tagId, float32);
        public void SetFloatArray(int tagId, float[] array) => CurrentDirectory.Set(tagId, array);
        public void SetDouble(int tagId, double double64) => CurrentDirectory.Set(tagId, double64);
        public void SetDoubleArray(int tagId, double[] array) => CurrentDirectory.Set(tagId, array);
        public void SetInt8S(int tagId, sbyte int8S) => CurrentDirectory.Set(tagId, int8S);
        public void SetInt8SArray(int tagId, sbyte[] array) => CurrentDirectory.Set(tagId, array);
        public void SetInt8U(int tagId, byte int8U) => CurrentDirectory.Set(tagId, int8U);
        public void SetInt8UArray(int tagId, byte[] array) => CurrentDirectory.Set(tagId, array);
        public void SetInt16S(int tagId, short int16S) => CurrentDirectory.Set(tagId, int16S);
        public void SetInt16SArray(int tagId, short[] array) => CurrentDirectory.Set(tagId, array);
        public void SetInt16U(int tagId, ushort int16U) => CurrentDirectory.Set(tagId, int16U);
        public void SetInt16UArray(int tagId, ushort[] array) => CurrentDirectory.Set(tagId, array);
        public void SetInt32S(int tagId, int int32S) => CurrentDirectory.Set(tagId, int32S);
        public void SetInt32SArray(int tagId, int[] array) => CurrentDirectory.Set(tagId, array);
        public void SetInt32U(int tagId, uint int32U) => CurrentDirectory.Set(tagId, int32U);
        public void SetInt32UArray(int tagId, uint[] array) => CurrentDirectory.Set(tagId, array);

        public abstract bool CustomProcessTag(int tagOffset, ICollection<int> processedIfdOffsets, IndexedReader reader, int tagId, int byteCount);

        public abstract bool TryCustomProcessFormat(int tagId, TiffDataFormatCode formatCode, uint componentCount, out long byteCount);

        public abstract bool HasFollowerIfd();

        public abstract bool TryEnterSubIfd(int tagType);

        public abstract void SetTiffMarker(int marker);
    }
}
