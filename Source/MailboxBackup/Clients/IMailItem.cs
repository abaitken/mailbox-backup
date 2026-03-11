using System;

namespace MailboxBackup.Clients
{
    public interface IMailItem : IDisposable
    {
        string UniqueId { get; }
        DateTimeOffset Date { get; }

        void MoveTo(IRemoteFolder target);
        void WriteToDisk(string destination);
    }
}
