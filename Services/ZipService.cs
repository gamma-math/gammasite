using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace GamMaSite.Services
{
    public class ZipService
    {
        public Dictionary<string, byte[]> GetZipAsMap(Stream stream)
        {
            Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
            ZipArchive zis = new ZipArchive(stream);
            ReadOnlyCollection<ZipArchiveEntry> entries = zis.Entries;
            foreach (ZipArchiveEntry entry in entries)
            {
                using (var memoryStream = new MemoryStream())
                {
                    entry.Open().CopyTo(memoryStream);
                    dictionary[Path.GetFullPath(entry.FullName)] = memoryStream.ToArray();
                }
            }
            return dictionary;
        }

        public Dictionary<string, byte[]> GetZipAsMap(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            var dictionary = GetZipAsMap(stream);
            return dictionary;
        }
    }
}
