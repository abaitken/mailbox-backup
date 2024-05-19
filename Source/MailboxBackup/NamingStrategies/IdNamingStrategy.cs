using MailKit;
using MimeKit;

namespace MailboxBackup
{
    class IdNamingStrategy : DownloadedMessageFileNamingStrategy
    {
        public override string Apply(UniqueId uid, MimeMessage message)
        {
            return $"{uid}.eml";
        }
    }
}