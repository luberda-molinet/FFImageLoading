using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Config;
using FFImageLoading.Work;

namespace FFImageLoading.Svg.Platform
{
    [Preserve(AllMembers=true)]
    public class SvgDataResolver : IVectorDataResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:FFImageLoading.Svg.Platform.SvgDataResolver"/> class.
        /// Default SVG size is read from SVG file width / height attributes
        /// You can override it by specyfing vectorWidth / vectorHeight params
        /// </summary>
        /// <param name="vectorWidth">Vector width.</param>
        /// <param name="vectorHeight">Vector height.</param>
        /// <param name="useDipUnits">If set to <c>true</c> use dip units.</param>
        /// <param name="replaceStringMap">Replace string map.</param>
        public SvgDataResolver(int vectorWidth = 0, int vectorHeight = 0, bool useDipUnits = true, Dictionary<string, string> replaceStringMap = null)
        {
            throw new Exception(DoNotReferenceMessage);
        }

        public static string DoNotReferenceMessage
        {
            get
            {
                return "You are referencing the Portable version in your App - you need to reference the platform specific version";
            }
        }

        public Configuration Configuration => throw new NotImplementedException(DoNotReferenceMessage);

        public bool UseDipUnits => throw new NotImplementedException(DoNotReferenceMessage);

        public int VectorHeight => throw new NotImplementedException(DoNotReferenceMessage);

        public int VectorWidth => throw new NotImplementedException(DoNotReferenceMessage);

        public Dictionary<string, string> ReplaceStringMap
        {
            get
            {
                throw new NotImplementedException(DoNotReferenceMessage);
            }
            set
            {
                throw new NotImplementedException(DoNotReferenceMessage);
            }
        }

        public Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            throw new NotImplementedException(DoNotReferenceMessage);
        }
    }
}
