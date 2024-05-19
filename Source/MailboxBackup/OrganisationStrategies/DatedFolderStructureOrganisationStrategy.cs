using MailKit;
using MimeKit;
using System.IO;

namespace MailboxBackup
{
    class DatedFolderStructureOrganisationStrategy : LocalOrganisationStrategy
    {
        private readonly string _outputFolder;

        public DatedFolderStructureOrganisationStrategy(string outputFolder)
        {
            _outputFolder = outputFolder;
        }

        public override string Apply(MimeMessage message, IMailFolder folder)
        {
            var messageYear = message.Date.Year.ToString();
            var pathName  = RemoteFolderView.ConvertToFileSystemPath(folder.FullName);
            if(pathName.StartsWith(messageYear))
                return Path.Combine(_outputFolder, pathName);
            return Path.Combine(_outputFolder, message.Date.Year.ToString(), pathName);
        }
    }
}