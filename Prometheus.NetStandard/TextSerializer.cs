﻿using System;
using System.Globalization;
using System.IO;

namespace Prometheus
{
    internal sealed class TextSerializer : IMetricsSerializer, IDisposable
    {
        private const byte NewLine = (byte)'\n';
        private const byte Space = (byte)' ';

        public TextSerializer(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            _stream = new BufferedStream(stream, 16 * 1024);
        }

        public void Dispose()
        {
            // We do not take ownership of the stream and never want to dispose/finalize it.
            _stream.Flush();
            GC.SuppressFinalize(_stream);
        }

        private readonly BufferedStream _stream;

        // HELP name help
        // TYPE name type
        public void WriteFamilyDeclaration(byte[][] headerLines)
        {
            foreach (var line in headerLines)
            {
                _stream.Write(line, 0, line.Length);
                _stream.WriteByte(NewLine);
            }
        }

        // Reuse a buffer to do the UTF-8 encoding.
        // Maybe one day also ValueStringBuilder but that would be .NET Core only.
        // https://github.com/dotnet/corefx/issues/28379
        // Size limit guided by https://stackoverflow.com/questions/21146544/what-is-the-maximum-length-of-double-tostringd
        private readonly byte[] _stringBytesBuffer = new byte[32];

        // name{labelkey1="labelvalue1",labelkey2="labelvalue2"} 123.456
        public void WriteMetric(byte[] identifier, double value)
        {
            _stream.Write(identifier, 0, identifier.Length);
            _stream.WriteByte(Space);

            var valueAsString = value.ToString(CultureInfo.InvariantCulture);

            var numBytes = PrometheusConstants.ExportEncoding
                .GetBytes(valueAsString, 0, valueAsString.Length, _stringBytesBuffer, 0);

            _stream.Write(_stringBytesBuffer, 0, numBytes);
            _stream.WriteByte(NewLine);
        }
    }
}
