using System;
using System.Collections.Generic;

namespace FFImageLoading.Helpers.Exif
{
    internal static class JpegSegmentReader
    {
        public static IEnumerable<JpegSegment> ReadSegments(SequentialReader reader, ICollection<JpegSegmentType> segmentTypes = null)
        {
            if (!reader.IsMotorolaByteOrder)
                throw new Exception("Must be big-endian/Motorola byte order.");

            // first two bytes should be JPEG magic number
            var magicNumber = reader.GetUInt16();

            if (magicNumber != 0xFFD8)
                throw new Exception($"JPEG data is expected to begin with 0xFFD8 (ÿØ) not 0x{magicNumber:X4}");

            do
            {
                // Find the segment marker. Markers are zero or more 0xFF bytes, followed
                // by a 0xFF and then a byte not equal to 0x00 or 0xFF.
                var segmentIdentifier = reader.GetByte();
                var segmentTypeByte = reader.GetByte();

                // Read until we have a 0xFF byte followed by a byte that is not 0xFF or 0x00
                while (segmentIdentifier != 0xFF || segmentTypeByte == 0xFF || segmentTypeByte == 0)
                {
                    segmentIdentifier = segmentTypeByte;
                    segmentTypeByte = reader.GetByte();
                }

                var segmentType = (JpegSegmentType)segmentTypeByte;

                if (segmentType == JpegSegmentType.Sos)
                {
                    // The 'Start-Of-Scan' segment's length doesn't include the image data, instead would
                    // have to search for the two bytes: 0xFF 0xD9 (EOI).
                    // It comes last so simply return at this point
                    yield break;
                }

                if (segmentType == JpegSegmentType.Eoi)
                {
                    // the 'End-Of-Image' segment -- this should never be found in this fashion
                    yield break;
                }

                // next 2-bytes are <segment-size>: [high-byte] [low-byte]
                var segmentLength = (int)reader.GetUInt16();

                // segment length includes size bytes, so subtract two
                segmentLength -= 2;

                // TODO exception strings should end with periods
                if (segmentLength < 0)
                    throw new Exception("JPEG segment size would be less than zero");

                // Check whether we are interested in this segment
                if (segmentTypes == null || segmentTypes.Contains(segmentType))
                {
                    var segmentOffset = reader.Position;
                    var segmentBytes = reader.GetBytes(segmentLength);

                    yield return new JpegSegment(segmentType, segmentBytes, segmentOffset);
                }
                else
                {
                    // Some of the JPEG is truncated, so just return what data we've already gathered
                    if (!reader.TrySkip(segmentLength))
                        yield break;
                }
            }
            while (true);
        }
    }
}
