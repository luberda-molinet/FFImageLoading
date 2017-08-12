using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using System.Text;

namespace FFImageLoading.DataResolvers
{
    public class DataUrlResolver : IDataResolver
    {
        readonly static Regex _regex1 = new Regex(@"data:(?<mime>[\w/]+);(?<encoding>\w+),(?<data>.*)");
        readonly static Regex _regex2 = new Regex(@"data:(?<mime>.+?),(?<data>.+)");

        public Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);

            if (identifier.StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                var streamXML = new MemoryStream(Encoding.UTF8.GetBytes(identifier));

                return Task.FromResult(new Tuple<Stream, LoadingResult, ImageInformation>(
                    streamXML, LoadingResult.EmbeddedResource, imageInformation));
            }

            var mime = string.Empty;
            var data = string.Empty;
            var encoding = "base64";
            var match1 = _regex1.Match(identifier);
            var success = false;

            if (match1.Success)
            {
                mime = match1.Groups["mime"].Value;
                encoding = match1.Groups["encoding"].Value;
                data = match1.Groups["data"].Value;
                success = true;
            }
            else
            {
                var match2 = _regex2.Match(identifier);
                if (match2.Success)
                {
                    mime = match2.Groups["mime"].Value;
                    data = match2.Groups["data"].Value;
                    success = true;
                }
            }

            if (!success || (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                                   && !mime.StartsWith("text/", StringComparison.OrdinalIgnoreCase)))
            {
                throw new NotImplementedException("Data type not supported");
            }

            if (!encoding.Equals("base64", StringComparison.OrdinalIgnoreCase) 
                || data.StartsWith("<", StringComparison.OrdinalIgnoreCase))
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

                return Task.FromResult(new Tuple<Stream, LoadingResult, ImageInformation>(
                    stream, LoadingResult.EmbeddedResource, imageInformation));
            }

            var streamBase64 = new MemoryStream(Convert.FromBase64String(data));

            return Task.FromResult(new Tuple<Stream, LoadingResult, ImageInformation>(
                streamBase64, LoadingResult.EmbeddedResource, imageInformation));
        }
    }
}
