using System.IO;

namespace MailboxBackup
{
    internal class FileSystem : IFileSystem
    {
        public bool DirectoryExists(string value)
        {
            return Directory.Exists(value);
        }

        public bool FileExists(string value)
        {
            return File.Exists(value);
        }
    }
}