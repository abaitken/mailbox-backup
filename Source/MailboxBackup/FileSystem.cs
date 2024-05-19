using System.IO;

namespace MailboxBackup
{
    internal class FileSystem : IFileSystem
    {
        public void CreateDirectory(string value)
        {
            Directory.CreateDirectory(value);
        }

        public bool DirectoryExists(string value)
        {
            return Directory.Exists(value);
        }

        public bool FileExists(string value)
        {
            return File.Exists(value);
        }

        public Stream Read(string value)
        {
            return new FileStream(value, FileMode.Open);
        }
    }
}