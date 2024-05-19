using MailKit;
using MimeKit;

namespace MailboxBackup
{
    class PreserveRemoteOrganisationStrategy : RemoteOrganisationStrategy
    {
        public override string Apply(MimeMessage message, IMailFolder currentFolder)
        {
            return null;
        }
    }
}