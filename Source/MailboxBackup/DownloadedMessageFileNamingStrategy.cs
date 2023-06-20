using MailKit;
using MimeKit;

namespace MailboxBackup
{
    abstract class DownloadedMessageFileNamingStrategy
    {
        public abstract string Apply(UniqueId uid, MimeMessage message);
    }
}