namespace MailboxBackup
{
    internal interface IFileSystem
    {
        bool DirectoryExists(string value);
        bool FileExists(string value);
    }
}