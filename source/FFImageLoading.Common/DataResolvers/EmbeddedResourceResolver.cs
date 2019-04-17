using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using System.Reflection;

namespace FFImageLoading.DataResolvers
{
    public class EmbeddedResourceResolver : IDataResolver
    {
        public Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            if (!identifier.StartsWith("resource://", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Only resource:// scheme is supported");


            var uri = new Uri(identifier);
            Assembly assembly = null;

            var parts = uri.OriginalString.Substring(11).Split('?');
            var resourceName = parts.First();

            if (parts.Count() > 1)
            {
                var name = Uri.UnescapeDataString(uri.Query.Substring(10));
                var assemblyName = new AssemblyName(name);
                assembly = Assembly.Load(assemblyName);
            }

            if (assembly == null)
            {
                var callingAssemblyMethod = typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetCallingAssembly");
                assembly = (Assembly)callingAssemblyMethod.Invoke(null, new object[0]);
            }

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(identifier);

            var stream = assembly.GetManifestResourceStream(resourceName);

            return Task.FromResult(new DataResolverResult(
                stream, LoadingResult.EmbeddedResource, imageInformation));
        }
    }
}
