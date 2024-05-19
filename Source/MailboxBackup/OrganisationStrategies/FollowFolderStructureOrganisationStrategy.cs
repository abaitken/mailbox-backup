using MailKit;
using MimeKit;
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

        public override string Apply(MimeMessage message, IMailFolder folder)
        {
            var pathName  = RemoteFolderView.ConvertToFileSystemPath(folder.FullName);
            return Path.Combine(_outputFolder, pathName);
        }
    }
}