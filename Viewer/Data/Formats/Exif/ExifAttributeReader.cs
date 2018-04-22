﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.IO;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Exif;
using Directory = MetadataExtractor.Directory;

namespace Viewer.Data.Formats.Exif
{
    public class ExifMetadata
    {
        private readonly IReadOnlyList<Directory> _directories;

        public JpegSegment Segment { get; }

        public ExifMetadata(JpegSegment segment, IReadOnlyList<Directory> directories)
        {
            Segment = segment;
            _directories = directories;
        }

        public T GetDirectoryOfType<T>() where T : class
        {
            return _directories?.OfType<T>().FirstOrDefault();
        }
    }

    public class ExifAttributeReader : IAttributeReader
    {
        private ExifMetadata _exif;

        private IList<IExifAttributeParser> _tags;

        private int _index;

        /// <summary>
        /// Create exif attribute reader from parsed exif segment.
        /// </summary>
        /// <param name="exif">Parsed exif segment</param>
        /// <param name="tags">List of tags to read from the exif</param>
        public ExifAttributeReader(ExifMetadata exif, IList<IExifAttributeParser> tags)
        {
            _exif = exif;
            _tags = tags;
        }

        public Attribute Read()
        {
            while (_index < _tags.Count)
            {
                var tag = _tags[_index++];
                var attr = tag.Parse(_exif);
                if (attr != null)
                    return attr;
            }

            return null;
        }

        public void Dispose()
        {
        }
    }

    [Export(typeof(IAttributeReaderFactory))]
    public class ExifAttributeReaderFactory : IAttributeReaderFactory
    {
        private readonly IList<IExifAttributeParser> _tags;

        private const string ExifHeader = "Exif\0\0";

        public ExifAttributeReaderFactory()
        {
            _tags = new List<IExifAttributeParser>
            {
                // image metadata
                new ExifAttributeParser<ExifIfd0Directory>("ImageWidth", ExifIfd0Directory.TagImageWidth, AttributeType.Int),
                new ExifAttributeParser<ExifIfd0Directory>("ImageHeight", ExifIfd0Directory.TagImageHeight, AttributeType.Int),
                new ExifAttributeParser<ExifSubIfdDirectory>("DateTaken", ExifIfd0Directory.TagDateTimeOriginal, AttributeType.DateTime),
                new ExifAttributeParser<ExifIfd0Directory>("orientation", ExifIfd0Directory.TagOrientation, AttributeType.Int),
                new ThumbnaiExifAttributeParser("thumbnail"),

                // camera metadata
                new ExifAttributeParser<ExifIfd0Directory>("CameraModel", ExifIfd0Directory.TagModel, AttributeType.String),
                new ExifAttributeParser<ExifIfd0Directory>("CameraMaker", ExifIfd0Directory.TagMake, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("ExposureTime", ExifIfd0Directory.TagExposureTime, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("FStop", ExifIfd0Directory.TagFNumber, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("ExposureBias", ExifIfd0Directory.TagExposureBias, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("FocalLength", ExifIfd0Directory.TagFocalLength, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("MaxAperture", ExifIfd0Directory.TagMaxAperture, AttributeType.String),
            };
        }

        public IAttributeReader CreateFromSegments(FileInfo file, IEnumerable<JpegSegment> segments)
        {
            var exifReader = new ExifReader();
            foreach (var segment in segments)
            {
                if (IsExifSegment(segment))
                {
                    var directories = exifReader.Extract(new ByteArrayReader(segment.Bytes, ExifHeader.Length));
                    return new ExifAttributeReader(new ExifMetadata(segment, directories), _tags);
                }
            }
            
            return new ExifAttributeReader(new ExifMetadata(null, null), _tags);
        }

        private bool IsExifSegment(JpegSegment segment)
        {
            return segment.Type == JpegSegmentType.App1 &&
                   Encoding.UTF8.GetString(segment.Bytes, 0, ExifHeader.Length) == ExifHeader;
        }
    }
}
