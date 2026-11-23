using System;
using System.Text;

namespace OpenWaters
{
    internal enum PeerInfoPayloadKind
    {
        NoExtension,
        EmptyExtension,
        OpenWaters,
        OtherMod,
        UnsupportedVersion,
        Malformed
    }

    internal static class PeerInfoPayload
    {
        private const string VersionPrefix = "OWS";

        public static PeerInfoPayloadKind Read(
            byte[] bytes, int position, int size, string marker,
            out string text, out int consumed)
        {
            text = null;
            consumed = 0;
            if (bytes == null || position < 0 || position > size || size > bytes.Length)
            {
                return PeerInfoPayloadKind.Malformed;
            }
            if (!PeerInfoProtocol.ShouldReadServerExtension(size, position))
            {
                return PeerInfoPayloadKind.NoExtension;
            }
            if (!TryReadString(bytes, position, out text, out consumed))
            {
                return PeerInfoPayloadKind.Malformed;
            }
            if (string.IsNullOrEmpty(text))
            {
                return PeerInfoPayloadKind.EmptyExtension;
            }
            if (text.StartsWith(marker, StringComparison.Ordinal))
            {
                return PeerInfoPayloadKind.OpenWaters;
            }
            if (text.StartsWith(VersionPrefix, StringComparison.Ordinal))
            {
                return PeerInfoPayloadKind.UnsupportedVersion;
            }
            return PeerInfoPayloadKind.OtherMod;
        }

        public static byte[] EncodeString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            byte[] utf8 = Encoding.UTF8.GetBytes(value);
            byte[] length = Encode7BitLength(utf8.Length);
            byte[] result = new byte[length.Length + utf8.Length];
            Buffer.BlockCopy(length, 0, result, 0, length.Length);
            Buffer.BlockCopy(utf8, 0, result, length.Length, utf8.Length);
            return result;
        }

        private static bool TryReadString(byte[] bytes, int position, out string text, out int consumed)
        {
            text = null;
            consumed = 0;
            int index = position;
            if (!TryRead7BitLength(bytes, ref index, out int length) || length < 0 ||
                length > bytes.Length - index)
            {
                return false;
            }
            try
            {
                text = Encoding.UTF8.GetString(bytes, index, length);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
            consumed = index + length - position;
            return true;
        }

        private static bool TryRead7BitLength(byte[] bytes, ref int index, out int value)
        {
            value = 0;
            uint result = 0;
            int shift = 0;
            for (int i = 0; i < 5; i++)
            {
                if (index >= bytes.Length)
                {
                    return false;
                }
                byte current = bytes[index++];
                result |= (uint)(current & 0x7F) << shift;
                if ((current & 0x80) == 0)
                {
                    value = (int)result;
                    return result <= int.MaxValue;
                }
                shift += 7;
            }
            return false;
        }

        private static byte[] Encode7BitLength(int length)
        {
            uint value = (uint)length;
            byte[] result = new byte[5];
            int count = 0;
            while (value >= 0x80)
            {
                result[count++] = (byte)((value & 0x7F) | 0x80);
                value >>= 7;
            }
            result[count++] = (byte)value;
            byte[] trimmed = new byte[count];
            Buffer.BlockCopy(result, 0, trimmed, 0, count);
            return trimmed;
        }
    }
}
