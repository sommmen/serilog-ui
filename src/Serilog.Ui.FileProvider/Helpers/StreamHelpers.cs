using System.IO;
using System.Text;

namespace Serilog.Ui.FileProvider.Helpers
{
    /// <summary>
    /// Helpers for <see cref="Stream"/>
    /// </summary>
    internal class StreamHelpers
    {
        // ReSharper disable InconsistentNaming
        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Returns the number of lines in the given <paramref name="stream"/>.
        /// Taken from <see href="https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/Extensions/StreamExtensions.cs#L46"/> - Copied method to avoid dependency.
        /// Based on this article: <see href="https://www.nimaara.com/counting-lines-of-a-text-file/"/>
        /// </summary>
        public static long CountLines(Stream stream, Encoding encoding = default)
        {
            var lineCount = 0L;
            var byteBuffer = new byte[1024 * 1024];
            var detectedEOL = NULL;
            var currentChar = NULL;
            int bytesRead;

            if(encoding is null || Equals(encoding, Encoding.ASCII) || Equals(encoding, Encoding.UTF8))
            {
                while((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    for(var i = 0; i < bytesRead; i++)
                    {
                        currentChar = (char)byteBuffer[i];

                        if(detectedEOL != NULL)
                        {
                            if(currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if(currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }
            else
            {
                var charBuffer = new char[byteBuffer.Length];

                while((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                {
                    var charCount = encoding.GetChars(byteBuffer, 0, bytesRead, charBuffer, 0);

                    for(var i = 0; i < charCount; i++)
                    {
                        currentChar = charBuffer[i];

                        if(detectedEOL != NULL)
                        {
                            if(currentChar == detectedEOL)
                            {
                                lineCount++;
                            }
                        }
                        else if(currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }

            if(currentChar != LF && currentChar != CR && currentChar != NULL)
            {
                lineCount++;
            }

            return lineCount;
        }
    }
}
