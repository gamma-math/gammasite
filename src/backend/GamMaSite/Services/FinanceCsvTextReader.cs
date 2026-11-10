using System;
using System.IO;
using System.Text;

namespace GamMaSite.Services
{
    internal static class FinanceCsvTextReader
    {
        private static readonly Encoding Windows1252 = CreateWindows1252Encoding();

        public static string Read(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();

            try
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Windows1252.GetString(bytes);
            }
        }

        private static Encoding CreateWindows1252Encoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }
    }
}
