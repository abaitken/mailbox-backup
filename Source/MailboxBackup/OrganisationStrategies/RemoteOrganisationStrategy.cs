using MailboxBackup.Clients;

namespace MailboxBackup
{
    abstract class RemoteOrganisationStrategy
    {
        public abstract string Apply(IMailItem message, IRemoteFolder currentFolder);
    }
}