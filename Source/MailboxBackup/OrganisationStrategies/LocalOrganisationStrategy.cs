using MailKit;
using MimeKit;

namespace MailboxBackup
{
    abstract class LocalOrganisationStrategy
    {
        public abstract string Apply(MimeMessage message, IMailFolder folder);
    }
}