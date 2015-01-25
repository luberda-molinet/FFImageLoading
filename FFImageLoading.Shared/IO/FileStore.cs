using System;
using System.Threading.Tasks;
using System.IO;

namespace FFImageLoading.IO
{
    internal static class FileStore
    {
        public static async Task<byte[]> ReadBytes(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                using (var memory = new MemoryStream()) {
                    await fs.CopyToAsync(memory).ConfigureAwait(false);
                    return memory.ToArray();
                }
            }
        }

        public static async Task WriteBytes(string path, byte[] data)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                using (var memory = new MemoryStream(data)) {
                    await memory.CopyToAsync(fs);
                }
            }
        }
    }
}

