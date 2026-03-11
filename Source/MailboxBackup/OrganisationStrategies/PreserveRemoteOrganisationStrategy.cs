using MailboxBackup.Clients;

namespace MailboxBackup
{
    class PreserveRemoteOrganisationStrategy : RemoteOrganisationStrategy
    {
        public override string Apply(IMailItem message, IRemoteFolder folder)
        {
            return null;
        }
    }
}