using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MailboxBackup
{
    internal class App
    {
        public App()
        {
        }

        internal int Run(string[] args)
        {
            var parser = new ArgumentParser(args);
            if(parser.Count == 0)
            {
                Console.WriteLine("ERROR: Expected arguments");
                return ExitCodes.InvalidArguments;
            }

            var username = parser["-u"];
            if(username == null)
            {
                Console.WriteLine("ERROR: Expected username");
                return ExitCodes.InvalidArguments;
            }

            var password = parser["-p"];
            if (password == null)
            {
                Console.WriteLine("ERROR: Expected password");
                return ExitCodes.InvalidArguments;
            }

            var server = parser["-s"];
            if (string.IsNullOrWhiteSpace(server))
            {
                Console.WriteLine("ERROR: Expected server");
                return ExitCodes.InvalidArguments;
            }

            var output = parser["-o"];
            if (string.IsNullOrWhiteSpace(server))
            {
                Console.WriteLine("ERROR: Expected output");
                return ExitCodes.InvalidArguments;
            }

            var includeFolderFilter = parser["-if"] == null ? null : new Regex(parser["-if"]);
            var excludeFolderFilter = parser["-xf"] == null ? null : new Regex(parser["-xf"]);

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
                    if(includeFolderFilter != null && !includeFolderFilter.IsMatch(folder.Name))
                    {
                        Console.WriteLine($"Skipping folder '{folder.Name}', did not match the include filter");
                        continue;
                    }

                    if (excludeFolderFilter != null && excludeFolderFilter.IsMatch(folder.Name))
                    {
                        Console.WriteLine($"Skipping folder '{folder.Name}', matched the exclude filter");
                        continue;
                    }
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