using MailKit;
using MailKit.Net.Imap;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MailboxBackup
{
    class RemoteFolderView
    {
        public static RemoteFolderView Build(Logger logger, ImapClient client, Regex includeFolderFilter, Regex excludeFolderFilter)
        {
            var folders = (from FolderNamespace ns in client.PersonalNamespaces
                           from folder in client.GetFolders(ns)
                           select folder).ToList();
            
            var toplevel = client.GetFolder(client.PersonalNamespaces[0]);

            logger.WriteLine($"Discovering...");
            var selectedFolders = new List<IMailFolder>();

            foreach (var folder in folders)
            {
                if (includeFolderFilter != null && !includeFolderFilter.IsMatch(folder.FullName))
                {
                    logger.WriteLine($"\tSkipping folder '{folder.FullName}', did not match the include filter");
                    continue;
                }

                if (excludeFolderFilter != null && excludeFolderFilter.IsMatch(folder.FullName))
                {
                    logger.WriteLine($"\tSkipping folder '{folder.FullName}', matched the exclude filter");
                    continue;
                }

                selectedFolders.Add(folder);
            }

            return new RemoteFolderView(toplevel, folders, selectedFolders);
        }

        private readonly IMailFolder topLevel;
        private readonly List<IMailFolder> allFolders;
        private readonly List<IMailFolder> selectedFolders;

        public RemoteFolderView(IMailFolder topLevel, List<IMailFolder> allFolders, List<IMailFolder> selectedFolders)
        {
            this.topLevel = topLevel;
            this.allFolders = allFolders;
            this.selectedFolders = selectedFolders;
        }

        public IReadOnlyCollection<IMailFolder> Folders
        {
            get => selectedFolders;
        }
    }
}