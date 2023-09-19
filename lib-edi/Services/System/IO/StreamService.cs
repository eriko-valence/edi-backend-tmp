using lib_edi.Services.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.System.IO
{
    /// <summary>
    /// A class that provides methods related to managing stream objects
    /// </summary>
    public class StreamService
    {
        //https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// <summary>
        /// Converts stream into byte array
        /// </summary>
        /// <param name="s">Stream to convert to a byte array</param>
        /// <returns>
        /// Byte array of the stream object
        /// </returns>
        public static async Task<byte[]> ReadToEnd(Stream s)
        {
            try
            {
                long originalPosition = 0;

                if (s.CanSeek)
                {
                    originalPosition = s.Position;
                    s.Position = 0;
                }

                try
                {
                    byte[] readBuffer = new byte[4096];

                    int totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = s.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                    {
                        totalBytesRead += bytesRead;

                        if (totalBytesRead == readBuffer.Length)
                        {
                            int nextByte = s.ReadByte();
                            if (nextByte != -1)
                            {
                                byte[] temp = new byte[readBuffer.Length * 2];
                                Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                                Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                                readBuffer = temp;
                                totalBytesRead++;
                            }
                        }
                    }

                    byte[] buffer = readBuffer;
                    if (readBuffer.Length != totalBytesRead)
                    {
                        buffer = new byte[totalBytesRead];
                        Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                    }
                    return buffer;
                }
                finally
                {
                    if (s.CanSeek)
                    {
                        s.Position = originalPosition;
                    }
                }
            }
            catch (Exception e)
            {
                string customError = await EdiErrorsService.BuildExceptionMessageString(e, "U791", null);
                throw new Exception(customError);
            }


        }
    }
}
