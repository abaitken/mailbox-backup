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
            return Path.Combine(_outputFolder, message.Date.Year.ToString(), folder.Name);
        }
    }
}