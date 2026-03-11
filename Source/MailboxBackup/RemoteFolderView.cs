using MailboxBackup.Clients;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MailboxBackup
{
    class RemoteFolderView
    {
        public static RemoteFolderView Build(Logger logger, IRemoteEndpoint client, Regex includeFolderFilter, Regex excludeFolderFilter)
        {
            var folders = client.GetAllFolders();
            
            var toplevel = client.RootFolder;

            logger.WriteLine($"Discovering...");
            var selectedFolders = new List<IRemoteFolder>();

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

        private readonly IRemoteFolder topLevel;
        private readonly IList<IRemoteFolder> allFolders;
        private readonly List<IRemoteFolder> selectedFolders;

        public RemoteFolderView(IRemoteFolder topLevel, IList<IRemoteFolder> allFolders, List<IRemoteFolder> selectedFolders)
        {
            this.topLevel = topLevel;
            this.allFolders = allFolders;
            this.selectedFolders = selectedFolders;
        }

        public IReadOnlyCollection<IRemoteFolder> Folders
        {
            get => selectedFolders;
        }

        public IRemoteFolder Find(string remotePath)
        {
            var result = allFolders.FirstOrDefault(o => o.FullName.Equals(remotePath));
            return result;
        }

        public IRemoteFolder Create(string remotePath)
        {
            var folders = remotePath.Split(PathSeperator);
            var parent = topLevel;
            foreach (var item in folders)
            {
                var nextLevel = Find(Combine(parent.FullName, item));

                if(nextLevel == null)
                {
                    nextLevel = parent.Create(item);
                    allFolders.Add(nextLevel);
                }

                parent = nextLevel;
            }

            return parent;
        }
    }
}