using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace lib_edi.Helpers
{
	public class FileHelper
	{
		public static byte[] Compress(string stringToCompress)
		{
			try
			{
				byte[] bytesToCompress = Encoding.ASCII.GetBytes(stringToCompress);

				using (MemoryStream memory = new MemoryStream())
				{
					using (GZipStream gzipStream = new GZipStream(memory, CompressionMode.Compress, true))
					{
						gzipStream.Write(bytesToCompress, 0, bytesToCompress.Length);
					}
					return memory.ToArray();
				}
			}
			catch (Exception)
			{
				return null;
			}


		}

		public static byte[] CompressZipArchive(string fileName, string stringToCompress)
		{
			byte[] bytesToCompress = Encoding.ASCII.GetBytes(stringToCompress);

			byte[] compressedBytes;
			using (var memory = new MemoryStream())
			{
				using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
				{
					var entryInArchive = archive.CreateEntry(fileName, CompressionLevel.Optimal);
					using (var entryStream = entryInArchive.Open())
					using (var fileToCompressStream = new MemoryStream(bytesToCompress))
					{
						fileToCompressStream.CopyTo(entryStream);
					}
				}
				compressedBytes = memory.ToArray();
			}
			return compressedBytes;
		}
	}
}
