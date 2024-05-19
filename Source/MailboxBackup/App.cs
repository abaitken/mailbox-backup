using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static MailboxBackup.ArgumentParser;

namespace MailboxBackup
{
    internal class App
    {
        public App()
        {
        }

        internal int Run(string[] args)
        {
            var parser = new ArgumentParser();
            parser.Describe("HELP", new[] { "-h", "-?", "--help" }, "Help", "Display this help", ArgumentConditions.Help);
            parser.Describe("CONFIG", new[] { "-c", "--config" }, "Config file", "Configuration file\nLoads configuration file when encountered and inserts arguments into the queue. Subsequent arguments will override previous values.", ArgumentConditions.ArgsFileSource);

            parser.Describe("USER", new[] { "-u", "--username" }, "Username", "Account username", ArgumentConditions.TypeString | ArgumentConditions.Required);
            parser.Describe("PASS", new[] { "-p", "--password" }, "Password", "Account password", ArgumentConditions.TypeString | ArgumentConditions.Required, new[] { "USER" });
            parser.Describe("SERVER", new[] { "-s", "--server" }, "Server", "Server address", ArgumentConditions.TypeString | ArgumentConditions.Required);
            parser.Describe("SERVER_PORT", new[] { "--port" }, "Server port", "Server port", ArgumentConditions.TypeInteger, null, 993.ToString());
            parser.Describe("OUTPUTDIR", new[] { "-o", "--outdir" }, "Output", "Output directory", ArgumentConditions.TypeString | ArgumentConditions.Required);
            parser.Describe("FOLDER_INC", new[] { "-if" }, "Include pattern", "Include folder regex\nWhen supplied, only remote folder names matching the pattern will be downloaded. (Otherwise all folders will be downloaded)", ArgumentConditions.TypeString);
            parser.Describe("FOLDER_EXC", new[] { "-xf" }, "Exclude pattern", "Exclude folder regex\nWhen supplied, remote folders matching the pattern will not be downloaded", ArgumentConditions.TypeString);
            parser.Describe("DOWNLOAD_NO", new[] { "--nodl" }, "No download", "Do not download", ArgumentConditions.IsFlag);
            parser.Describe("TLSMODE", new[] { "--tlsmode" }, "TLS Options", "TLS Options", ArgumentConditions.Options, null, "SslOnConnect", new[] { "None", "Auto", "SslOnConnect", "StartTls", "StartTlsWhenAvailable" });
            parser.Describe("REMOTE_MOVE", new[] { "--remotemove" }, "Organise remote mail", "Move and organise messages remotely on the server", ArgumentConditions.IsFlag);
            parser.Describe("IMAP_LOG", new[] { "-il", "--imaplog" }, "IMAP log", "IMAP log", ArgumentConditions.TypeString);

            var argumentErrors = parser.ParseArgs(args, out var argumentValues);

            if (argumentValues.ContainsKey("HELP") && argumentValues.GetBool("HELP"))
            {
                Console.WriteLine("Mailbox Backup");
                Console.WriteLine("  Download remote mail items to local filesystem");
                Console.WriteLine();
                parser.DisplayHelp(ConsoleHelper.GetBufferWidthOrDefault(80));
                return ExitCodes.OK;
            }

            if (argumentErrors.Any())
            {
                parser.ReportErrors(argumentErrors);
                return ExitCodes.OK;
            }

            var username = argumentValues["USER"];
            var password = argumentValues["PASS"];
            var server = argumentValues["SERVER"];
            var port = argumentValues.GetInt("SERVER_PORT");
            var output = argumentValues["OUTPUTDIR"];
            var download = !argumentValues.GetBool("DOWNLOAD_NO");
            var tlsOption = ToSecureSocketOptions(argumentValues["TLSMODE"]);
            var remoteMove = argumentValues.GetBool("REMOTE_MOVE");
            var imaplog = argumentValues.GetString("IMAP_LOG", null);

            var includeFolderFilter = argumentValues.ContainsKey("FOLDER_INC") ? new Regex(argumentValues["FOLDER_INC"]) : null;
            var excludeFolderFilter = argumentValues.ContainsKey("FOLDER_EXC") ? new Regex(argumentValues["FOLDER_EXC"]) : null;

            IFileSystem fileSystem = new FileSystem();
            if (!fileSystem.DirectoryExists(output))
                fileSystem.CreateDirectory(output);

            var filenamingStrategy = new IdNamingStrategy();
            var localOrganisationStrategy = new DatedFolderStructureOrganisationStrategy(output);
            RemoteOrganisationStrategy remoteOrganisationStrategy = remoteMove 
                ? new DatedFolderStructureRemoteOrganisationStrategy() 
                : new PreserveRemoteOrganisationStrategy();

            using var client = imaplog == null 
                ? new ImapClient()
                : new ImapClient(new ProtocolLogger(imaplog));
                
            client.Connect(server, port, tlsOption);
            client.Authenticate(username, password);
            client.Inbox.Open(FolderAccess.ReadOnly);

            var folders = (from FolderNamespace ns in client.PersonalNamespaces
                           from folder in client.GetFolders(ns)
                           select folder).ToList();


            Console.WriteLine($"Discovering...");
            var selectedFolders = new List<IMailFolder>();

            foreach (var folder in folders)
            {
                if (includeFolderFilter != null && !includeFolderFilter.IsMatch(folder.FullName))
                {
                    Console.WriteLine($"\tSkipping folder '{folder.FullName}', did not match the include filter");
                    continue;
                }

                if (excludeFolderFilter != null && excludeFolderFilter.IsMatch(folder.FullName))
                {
                    Console.WriteLine($"\tSkipping folder '{folder.FullName}', matched the exclude filter");
                    continue;
                }

                selectedFolders.Add(folder);
            }

            var actionText = download ? "Downloading" : "Iterating";
            Console.WriteLine($"{actionText}...");

            for (int i = 0; i < selectedFolders.Count; i++)
            {
                var folder = selectedFolders[i];

                folder.Open(FolderAccess.ReadOnly);
                var uids = folder.Search(SearchQuery.All);

                Console.WriteLine($"{actionText} {uids.Count} items from folder '{folder.FullName}' ({i + 1}/{selectedFolders.Count})");
                var progress = new ConsoleProgressDisplay();
                progress.Begin(uids.Count);
                foreach (var uid in uids)
                {
                    progress.Update();

                    var message = folder.GetMessage(uid);

                    var expectedFolder = remoteOrganisationStrategy.Apply(message, folder);
                    if(expectedFolder != null)
                    {
                        folder.MoveTo(uid, expectedFolder);
                    }

                    if (download)
                    {
                        var destinationFolder = localOrganisationStrategy.Apply(message, folder);
                        if (!fileSystem.DirectoryExists(destinationFolder))
                            fileSystem.CreateDirectory(destinationFolder);

                        var filename = filenamingStrategy.Apply(uid, message);
                        var destination = Path.Combine(destinationFolder, filename);

                        if (!fileSystem.FileExists(destination))
                            message.WriteTo(destination);
                    }
                }
                progress.End();
                folder.Close();
            }


            client.Disconnect(true);
            return ExitCodes.OK;
        }

        private static SecureSocketOptions ToSecureSocketOptions(string value)
        {
            return value switch
            {
                "None" => SecureSocketOptions.None,
                "Auto" => SecureSocketOptions.Auto,
                "SslOnConnect" => SecureSocketOptions.SslOnConnect,
                "StartTls" => SecureSocketOptions.StartTls,
                "StartTlsWhenAvailable" => SecureSocketOptions.StartTlsWhenAvailable,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };

        }

    }
}