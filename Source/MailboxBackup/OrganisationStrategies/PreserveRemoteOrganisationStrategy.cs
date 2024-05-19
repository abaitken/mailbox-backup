using MailKit;
using MimeKit;

namespace MailboxBackup
{
    class PreserveRemoteOrganisationStrategy : RemoteOrganisationStrategy
    {
        public override IMailFolder Apply(MimeMessage message, IMailFolder currentFolder)
        {
            return null;
        }
    }
}