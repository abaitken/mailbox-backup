using MailboxBackup.Clients;

namespace MailboxBackup
{
    class DatedFolderStructureRemoteOrganisationStrategy : RemoteOrganisationStrategy
    {
        private readonly string remoteHome;

        public DatedFolderStructureRemoteOrganisationStrategy(string remoteHome)
        {
            this.remoteHome = remoteHome;
        }

        public override string Apply(IMailItem message, IRemoteFolder currentFolder)
        {
            var messageYear = message.Date.Year.ToString();
            var pathBase = RemoteFolderView.Combine(remoteHome, messageYear);

            if(currentFolder.FullName.StartsWith(pathBase))
                return currentFolder.FullName;

            var remotePath = RemoteFolderView.Combine(pathBase, currentFolder.FullName);
            return remotePath;
        }
    }
}