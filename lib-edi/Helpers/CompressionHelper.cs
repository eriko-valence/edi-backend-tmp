using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;

namespace lib_edi.Helpers
{
    public class CompressionHelper
    {
        // NHGH-3474 20240912 1514 Extracts zip archive entries from this compressed (.zip) byte array
        public static Dictionary<string, byte[]> ExtractZipArchive(byte[] data)
        {
            const long MaxLength = 100 * 1024 * 1024; // 100 MB

            Dictionary<string, byte[]> extractedFiles = new();
            if (data != null)
            {
                using MemoryStream downloadedRepoStream = new(data);
                using ZipArchive downloadedRepoArchive = new(downloadedRepoStream, ZipArchiveMode.Read, false);

                var archiveSize = downloadedRepoArchive.Entries.Sum(entry => entry.Length);

                if (archiveSize > MaxLength)
                {
                    throw new Exception($"Archive with uncompressed {archiveSize} bytes is greater than the allowed size of {MaxLength} bytes.");
                }

                ReadOnlyCollection<ZipArchiveEntry> entriesToExtract = downloadedRepoArchive.Entries;
                foreach (ZipArchiveEntry entry in entriesToExtract)
                {
                    using Stream entryStream = entry.Open();
                    using MemoryStream ms = new();
                    entryStream.CopyTo(ms);
                    extractedFiles.Add(entry.Name, ms.ToArray());
                }
            }
            return extractedFiles;
        }

        // NHGH-3474 20240912 1514 Builds a tarball from the given dictionary of file names and byte arrays
        public static MemoryStream BuildTarball(Dictionary<string, byte[]> data)
        {
            using MemoryStream outputStream = new();
            using (GZipOutputStream gzoStream = new(outputStream))
            {
                gzoStream.IsStreamOwner = false;
                gzoStream.SetLevel(9);
                using TarOutputStream tarOutputStream = new(gzoStream, null);
                foreach (KeyValuePair<string, byte[]> item in data)
                {
                    if (item.Key != null && item.Value != null)
                    {
                        byte[] contentBytes = item.Value;
                        tarOutputStream.IsStreamOwner = false;
                        TarEntry entry = TarEntry.CreateTarEntry(item.Key);
                        entry.Size = contentBytes.Length;
                        tarOutputStream.PutNextEntry(entry);
                        tarOutputStream.Write(contentBytes, 0, contentBytes.Length);
                        tarOutputStream.CloseEntry();
                    }
                }
                tarOutputStream.Close();
            }

            outputStream.Flush();
            outputStream.Position = 0;

            return outputStream;
        }
    }
}
