using MailKit;
using MimeKit;

namespace MailboxBackup
{
    abstract class RemoteOrganisationStrategy
    {
        public abstract string Apply(MimeMessage message, IMailFolder currentFolder);
    }
}