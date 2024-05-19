using MailKit;
using MimeKit;

namespace MailboxBackup
{
    class DatedFolderStructureRemoteOrganisationStrategy : RemoteOrganisationStrategy
    {
        public override string Apply(MimeMessage message, IMailFolder currentFolder)
        {
            var messageYear = message.Date.Year.ToString();

            if(currentFolder.FullName.StartsWith(messageYear))
                return currentFolder.FullName;

            var remotePath = RemoteFolderView.Combine(messageYear, currentFolder.FullName);
            return remotePath;
        }
    }
}