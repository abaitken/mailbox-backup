using System.IO;

namespace MailboxBackup
{
    internal interface IFileSystem
    {
        bool DirectoryExists(string value);
        bool FileExists(string value);
        Stream Read(string value);
        void CreateDirectory(string value);
    }
}