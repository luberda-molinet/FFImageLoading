using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FFImageLoading.Helpers.Exif
{
    internal class ExifTiffHandler : DirectoryTiffHandler
    {
        public ExifTiffHandler(List<Directory> directories) : base(directories)
        { }

        public override void SetTiffMarker(int marker)
        {
            const int standardTiffMarker = 0x002A;
            const int olympusRawTiffMarker = 0x4F52; // for ORF files
            const int olympusRawTiffMarker2 = 0x5352; // for ORF files
            // const int panasonicRawTiffMarker = 0x0055; // for RAW, RW2, and RWL files

            switch (marker)
            {
                case standardTiffMarker:
                case olympusRawTiffMarker:  // Todo: implement an IFD0
                case olympusRawTiffMarker2: // Todo: implement an IFD0
                    PushDirectory(new ExifIfd0Directory());
                    break;
                //case panasonicRawTiffMarker:
                    //PushDirectory(new PanasonicRawIfd0Directory());
                    //break;
                default:
                    throw new Exception($"Unexpected TIFF marker: 0x{marker:X}");
            }
        }

        public override bool TryEnterSubIfd(int tagId)
        {
            if (tagId == ExifDirectoryBase.TagSubIfdOffset)
            {
                PushDirectory(new ExifSubIfdDirectory());
                return true;
            }

            if (CurrentDirectory is ExifIfd0Directory)
            {
                if (tagId == ExifIfd0Directory.TagExifSubIfdOffset)
                {
                    PushDirectory(new ExifSubIfdDirectory());
                    return true;
                }
                if (tagId == ExifIfd0Directory.TagGpsInfoOffset)
                {
                    //PushDirectory(new GpsDirectory());
                    PushDirectory(new ExifIfd0Directory());
                    return true;
                }
            }
            else if (CurrentDirectory is ExifSubIfdDirectory)
            {
                if (tagId == ExifSubIfdDirectory.TagInteropOffset)
                {
                    PushDirectory(new ExifIfd0Directory());
                    return true;
                }
            }
        
            return false;
        }

        public override bool HasFollowerIfd()
        {
            // In Exif, the only known 'follower' IFD is the thumbnail one, however this may not be the case.
            // UPDATE: In multipage TIFFs, the 'follower' IFD points to the next image in the set
            if (CurrentDirectory is ExifIfd0Directory)
            {
                return true;
            }

            // This should not happen, as Exif doesn't use follower IFDs apart from that above.
            // NOTE have seen the CanonMakernoteDirectory IFD have a follower pointer, but it points to invalid data.
            return false;
        }

        public override bool CustomProcessTag(int tagOffset, ICollection<int> processedIfdOffsets, IndexedReader reader, int tagId, int byteCount)
        {
            // Some 0x0000 tags have a 0 byteCount. Determine whether it's bad.
            if (tagId == 0)
            {
                if (CurrentDirectory.ContainsTag(tagId))
                {
                    // Let it go through for now. Some directories handle it, some don't.
                    return false;
                }

                // Skip over 0x0000 tags that don't have any associated bytes. No idea what it contains in this case, if anything.
                if (byteCount == 0)
                    return true;
            }

            return false;
        }

        public override bool TryCustomProcessFormat(int tagId, TiffDataFormatCode formatCode, uint componentCount, out long byteCount)
        {
            if ((ushort)formatCode == 13u)
            {
                byteCount = 4 * componentCount;
                return true;
            }

            // an unknown (0) formatCode needs to be potentially handled later as a highly custom directory tag
            if (formatCode == 0)
            {
                byteCount = 0;
                return true;
            }

            byteCount = default(int);
            return false;
        }

        private static void ProcessBinary(Directory directory, int tagValueOffset, IndexedReader reader, int byteCount, bool issigned = true, int arrayLength = 1)
        {
            // expects signed/unsigned int16 (for now)
            var byteSize = issigned ? sizeof(short) : sizeof(ushort);

            // 'directory' is assumed to contain tags that correspond to the byte position unless it's a set of bytes
            for (var i = 0; i < byteCount; i++)
            {
                if (directory.HasTagName(i))
                {
                    // only process this tag if the 'next' integral tag exists. Otherwise, it's a set of bytes
                    if (i < byteCount - 1 && directory.HasTagName(i + 1))
                    {
                        if (issigned)
                            directory.Set(i, reader.GetInt16(tagValueOffset + i * byteSize));
                        else
                            directory.Set(i, reader.GetUInt16(tagValueOffset + i * byteSize));
                    }
                    else
                    {
                        // the next arrayLength bytes are a multi-byte value
                        if (issigned)
                        {
                            var val = new short[arrayLength];
                            for (var j = 0; j < val.Length; j++)
                                val[j] = reader.GetInt16(tagValueOffset + (i + j) * byteSize);
                            directory.Set(i, val);
                        }
                        else
                        {
                            var val = new ushort[arrayLength];
                            for (var j = 0; j < val.Length; j++)
                                val[j] = reader.GetUInt16(tagValueOffset + (i + j) * byteSize);
                            directory.Set(i, val);
                        }

                        i += arrayLength - 1;
                    }
                }

            }
        }
    }
}
