using MailKit;
using MailKit.Net.Imap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private const char PathSeperator = '/';

        public static string Combine(string left, params string[] other)
        {
            var builder = new StringBuilder();
            builder.Append(left);

            foreach(var item in other)
            {
                if(builder.Length != 0 && builder[^1] != PathSeperator)
                    builder.Append(PathSeperator);
                
                builder.Append(item);
            }

            return builder.ToString();
        }

        public static string ConvertToFileSystemPath(string remotePath)
        {
            var pathName  = remotePath.Replace(PathSeperator, Path.DirectorySeparatorChar);
            return pathName;
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

        public IMailFolder Find(string remotePath)
        {
            var result = allFolders.FirstOrDefault(o => o.FullName.Equals(remotePath));
            return result;
        }

        public IMailFolder Create(string remotePath)
        {
            var folders = remotePath.Split(PathSeperator);
            var parent = topLevel;
            foreach (var item in folders)
            {
                var nextLevel = Find(Combine(parent.FullName, item));

                if(nextLevel == null)
                {
                    nextLevel = parent.Create(item, true);
                    allFolders.Add(nextLevel);
                }

                parent = nextLevel;
            }

            return parent;
        }
    }
}