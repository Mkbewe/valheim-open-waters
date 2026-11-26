using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Splatform;

namespace OpenWaters
{
    internal sealed class CloudSidecarStorage : ISidecarStorage
    {
        public bool Exists(string path)
        {
            return FileHelpers.FileExistsCloud(path);
        }

        public string Read(string path)
        {
            string temporary = TemporaryPath();
            try
            {
                FileHelpers.FileCopyOutFromCloud(path, temporary, false);
                return File.ReadAllText(temporary);
            }
            finally
            {
                DeleteTemporary(temporary);
            }
        }

        public void Write(string path, string text)
        {
            string temporary = TemporaryPath();
            try
            {
                File.WriteAllText(temporary, text);
                if (!FileHelpers.FileCopyIntoCloud(temporary, path, CloudStorageFileGrouping.SameFolder))
                {
                    throw new IOException("FileCopyIntoCloud failed for '" + path + "'.");
                }
            }
            finally
            {
                DeleteTemporary(temporary);
            }
        }

        public void Delete(string path)
        {
            FileHelpers.Delete(path, FileHelpers.FileSource.Cloud);
        }

        public void Move(string source, string destination)
        {
            Write(destination, Read(source));
            Delete(source);
        }

        public string[] GetDirectories(string path)
        {
            string[] files = FileHelpers.GetFiles(FileHelpers.FileSource.Cloud, path, null, null);
            if (files == null)
            {
                return Array.Empty<string>();
            }
            HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                string directory = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(directory))
                {
                    directories.Add(directory);
                }
            }
            return directories.ToArray();
        }

        private static string TemporaryPath()
        {
            return Path.Combine(Path.GetTempPath(), "openwaters-" + Guid.NewGuid().ToString("N") + ".tmp");
        }

        private static void DeleteTemporary(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // Best-effort cleanup of a file outside the save roots.
            }
        }
    }
}
