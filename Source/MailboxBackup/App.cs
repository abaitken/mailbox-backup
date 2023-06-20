using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.IO;
using System.Linq;

namespace MailboxBackup
{
    internal class App
    {
        public App()
        {
        }

        internal int Run(string[] args)
        {
            if(args.Length != 4)
            {
                Console.WriteLine("ERROR: Expected 4 arguments: username, password, server, output folder");
                return ExitCodes.InvalidArguments;
            }

            var username = args[0];
            var password = args[1];
            var server = args[2];
            var output = args[3];

            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

            using (var client = new ImapClient())
            {
                client.Connect(server, 993, SecureSocketOptions.SslOnConnect);
                client.Authenticate(username, password);
                client.Inbox.Open(FolderAccess.ReadOnly);

                var folders = (from FolderNamespace ns in client.PersonalNamespaces
                               from folder in client.GetFolders(ns)
                               select folder).ToList();

                foreach (var folder in folders)
                {
                    folder.Open(FolderAccess.ReadOnly);
                    var uids = folder.Search(SearchQuery.All);

                    Console.WriteLine($"Downloading {uids.Count} items from folder '{folder.Name}'");
                    var progress = new ConsoleProgressDisplay();
                    progress.Begin(uids.Count);
                    foreach (var uid in uids)
                    {
                        progress.Update();
                        var message = folder.GetMessage(uid);

                        var destinationFolder = Path.Combine(output, message.Date.Year.ToString(), folder.Name);
                        if (!Directory.Exists(destinationFolder))
                            Directory.CreateDirectory(destinationFolder);
                        var destination = Path.Combine(destinationFolder, $"{uid}.eml");

                        if(!File.Exists(destination))
                            message.WriteTo(destination);
                    }
                    progress.End();
                    folder.Close();
                }
                

                client.Disconnect(true);
            }
            return ExitCodes.OK;
        }
    }
}