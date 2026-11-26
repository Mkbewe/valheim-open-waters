using System;

namespace OpenWaters
{
    internal static class SidecarCopier
    {
        public static bool TryCopy(
            ISidecarStorage from, string sourcePath,
            ISidecarStorage to, string targetPath, string uid)
        {
            if (from == null || to == null || string.IsNullOrEmpty(sourcePath) ||
                string.IsNullOrEmpty(targetPath) || string.IsNullOrEmpty(uid))
            {
                return false;
            }
            try
            {
                string text = from.Read(sourcePath);
                if (!WorldSettingsCodec.TryParseByUid(text, uid, out _))
                {
                    return false;
                }
                if (to.Exists(targetPath))
                {
                    return false;
                }
                to.Write(targetPath, text);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
