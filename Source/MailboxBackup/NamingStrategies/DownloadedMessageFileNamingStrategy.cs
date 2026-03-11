using MailboxBackup.Clients;

namespace MailboxBackup
{
    abstract class DownloadedMessageFileNamingStrategy
    {
        public abstract string Apply(IMailItem message);
    }
}