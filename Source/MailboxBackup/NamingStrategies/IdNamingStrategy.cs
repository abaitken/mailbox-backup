using MailboxBackup.Clients;

namespace MailboxBackup
{
    class IdNamingStrategy : DownloadedMessageFileNamingStrategy
    {
        public override string Apply(IMailItem message)
        {
            return $"{message.UniqueId}.eml";
        }
    }
}