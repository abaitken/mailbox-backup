using MailKit;
using MimeKit;

namespace MailboxBackup
{
    abstract class RemoteOrganisationStrategy
    {
        public abstract IMailFolder Apply(MimeMessage message, IMailFolder currentFolder);
    }
}