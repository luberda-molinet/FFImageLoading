using System;

namespace FFImageLoading.Helpers.Exif
{
    internal enum TiffDataFormatCode : ushort
    {
        Int8U = 1,
        String = 2,
        Int16U = 3,
        Int32U = 4,
        RationalU = 5,
        Int8S = 6,
        Undefined = 7,
        Int16S = 8,
        Int32S = 9,
        RationalS = 10,
        Single = 11,
        Double = 12
    }

    /// <summary>An enumeration of data formats used by the TIFF specification.</summary>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    internal sealed class TiffDataFormat
    {
        public static readonly TiffDataFormat Int8U = new TiffDataFormat("BYTE", TiffDataFormatCode.Int8U, 1);
        public static readonly TiffDataFormat String = new TiffDataFormat("STRING", TiffDataFormatCode.String, 1);
        public static readonly TiffDataFormat Int16U = new TiffDataFormat("USHORT", TiffDataFormatCode.Int16U, 2);
        public static readonly TiffDataFormat Int32U = new TiffDataFormat("ULONG", TiffDataFormatCode.Int32U, 4);
        public static readonly TiffDataFormat RationalU = new TiffDataFormat("URATIONAL", TiffDataFormatCode.RationalU, 8);
        public static readonly TiffDataFormat Int8S = new TiffDataFormat("SBYTE", TiffDataFormatCode.Int8S, 1);
        public static readonly TiffDataFormat Undefined = new TiffDataFormat("UNDEFINED", TiffDataFormatCode.Undefined, 1);
        public static readonly TiffDataFormat Int16S = new TiffDataFormat("SSHORT", TiffDataFormatCode.Int16S, 2);
        public static readonly TiffDataFormat Int32S = new TiffDataFormat("SLONG", TiffDataFormatCode.Int32S, 4);
        public static readonly TiffDataFormat RationalS = new TiffDataFormat("SRATIONAL", TiffDataFormatCode.RationalS, 8);
        public static readonly TiffDataFormat Single = new TiffDataFormat("SINGLE", TiffDataFormatCode.Single, 4);
        public static readonly TiffDataFormat Double = new TiffDataFormat("DOUBLE", TiffDataFormatCode.Double, 8);

        public static TiffDataFormat FromTiffFormatCode(TiffDataFormatCode tiffFormatCode)
        {
            switch (tiffFormatCode)
            {
                case TiffDataFormatCode.Int8U: return Int8U;
                case TiffDataFormatCode.String: return String;
                case TiffDataFormatCode.Int16U: return Int16U;
                case TiffDataFormatCode.Int32U: return Int32U;
                case TiffDataFormatCode.RationalU: return RationalU;
                case TiffDataFormatCode.Int8S: return Int8S;
                case TiffDataFormatCode.Undefined: return Undefined;
                case TiffDataFormatCode.Int16S: return Int16S;
                case TiffDataFormatCode.Int32S: return Int32S;
                case TiffDataFormatCode.RationalS: return RationalS;
                case TiffDataFormatCode.Single: return Single;
                case TiffDataFormatCode.Double: return Double;
            }

            return null;
        }

        public string Name { get; }
        public int ComponentSizeBytes { get; }
        public TiffDataFormatCode TiffFormatCode { get; }

        private TiffDataFormat(string name, TiffDataFormatCode tiffFormatCode, int componentSizeBytes)
        {
            Name = name;
            TiffFormatCode = tiffFormatCode;
            ComponentSizeBytes = componentSizeBytes;
        }

        public override string ToString() => Name;
    }
}
