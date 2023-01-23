using System;
using System.Collections.Generic;

namespace FFImageLoading.Helpers.Exif
{
    internal interface ITiffHandler
    {
        void SetTiffMarker(int marker);

        bool TryEnterSubIfd(int tagType);

        bool HasFollowerIfd();

        void EndingIfd();

        bool CustomProcessTag(int tagOffset, ICollection<int> processedIfdOffsets, IndexedReader reader, int tagId, int byteCount);

        bool TryCustomProcessFormat(int tagId, TiffDataFormatCode formatCode, uint componentCount, out long byteCount);

        void Warn(string message);

        void Error(string message);

        void SetByteArray(int tagId, byte[] bytes);

        void SetString(int tagId, StringValue str);

        void SetRational(int tagId, Rational rational);

        void SetRationalArray(int tagId, Rational[] array);

        void SetFloat(int tagId, float float32);

        void SetFloatArray(int tagId, float[] array);

        void SetDouble(int tagId, double double64);

        void SetDoubleArray(int tagId, double[] array);

        void SetInt8S(int tagId, sbyte int8S);

        void SetInt8SArray(int tagId, sbyte[] array);

        void SetInt8U(int tagId, byte int8U);

        void SetInt8UArray(int tagId, byte[] array);

        void SetInt16S(int tagId, short int16S);

        void SetInt16SArray(int tagId, short[] array);

        void SetInt16U(int tagId, ushort int16U);

        void SetInt16UArray(int tagId, ushort[] array);

        void SetInt32S(int tagId, int int32S);

        void SetInt32SArray(int tagId, int[] array);

        void SetInt32U(int tagId, uint int32U);

        void SetInt32UArray(int tagId, uint[] array);        
    }
}
