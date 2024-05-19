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
            var pathName  = folder.Name.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(_outputFolder, message.Date.Year.ToString(), pathName);
        }
    }
}