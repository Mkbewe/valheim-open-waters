namespace OpenWaters
{
    internal static class PeerInfoProtocol
    {
        public const int VanillaTrailingBytes = 8;

        public static bool ShouldReadServerExtension(int packageSize, int position)
        {
            return packageSize - position > VanillaTrailingBytes;
        }

        public static bool EndsWith(byte[] bytes, int length, byte[] suffix)
        {
            if (bytes == null || suffix == null || suffix.Length == 0 ||
                length < suffix.Length || length > bytes.Length)
            {
                return false;
            }

            int start = length - suffix.Length;
            for (int i = 0; i < suffix.Length; i++)
            {
                if (bytes[start + i] != suffix[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
