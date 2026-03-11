using MailboxBackup.Clients;
using System.IO;

namespace MailboxBackup
{
    class FollowFolderStructureOrganisationStrategy : LocalOrganisationStrategy
    {
        private readonly string _outputFolder;

        public FollowFolderStructureOrganisationStrategy(string outputFolder)
        {
            _outputFolder = outputFolder;
        }

        public override string Apply(IMailItem message, IRemoteFolder folder)
        {
            var pathName  = RemoteFolderView.ConvertToFileSystemPath(folder.FullName);
            return Path.Combine(_outputFolder, pathName);
        }
    }
}