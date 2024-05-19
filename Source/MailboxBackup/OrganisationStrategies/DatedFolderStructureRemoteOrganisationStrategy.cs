using MailKit;
using MimeKit;

namespace MailboxBackup
{
    class DatedFolderStructureRemoteOrganisationStrategy : RemoteOrganisationStrategy
    {
        public override IMailFolder Apply(MimeMessage message, IMailFolder currentFolder)
        {
            throw new System.NotImplementedException();
        }
    }
}