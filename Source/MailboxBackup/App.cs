using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static MailboxBackup.ArgumentParser;

namespace MailboxBackup
{
    internal class App
    {
        private readonly Logger defaultLogger;

        public App(Logger defaultLogger)
        {
            this.defaultLogger = defaultLogger;
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
            parser.Describe("TLSMODE", new[] { "--tlsmode" }, "TLS Options", "TLS Options", ArgumentConditions.Options, null, SecureSocketOptionArgumentHelper.DefaultValue, SecureSocketOptionArgumentHelper.Values);
            parser.Describe("REMOTE_MOVE", new[] { "--remotemove" }, "Organise remote mail", "Move and organise messages remotely on the server", ArgumentConditions.IsFlag);
            parser.Describe("IMAP_LOG", new[] { "-il", "--imaplog" }, "IMAP log", "IMAP log", ArgumentConditions.TypeString);
            parser.Describe("REMOTE_HOME", new[] { "--remotehome" }, "Remote home", "Remote home path for organised file structure", ArgumentConditions.TypeString);
            parser.Describe("LOCALORGSTRAT", new[] { "--localorgstrat" }, "Local org strategy", "Local downloaded file organisation strategy", ArgumentConditions.Options, null, LocalOrganisationStrategy.Strategies.First(), LocalOrganisationStrategy.Strategies);
            parser.Describe("FILTER_AGE", new[] { "--filterage" }, "Filter e-mail age", "Filter e-mails older than provided age (in days)", ArgumentConditions.TypeInteger, null, "1");

            var argumentErrors = parser.ParseArgs(args, out var argumentValues);

            if (argumentValues.ContainsKey("HELP") && argumentValues.GetBool("HELP"))
            {
                defaultLogger.WriteLine("Mailbox Backup");
                defaultLogger.WriteLine("  Download remote mail items to local filesystem");
                defaultLogger.WriteLine();
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
            var tlsOption = SecureSocketOptionArgumentHelper.ToSecureSocketOptions(argumentValues["TLSMODE"]);
            var remoteMove = argumentValues.GetBool("REMOTE_MOVE");
            var remoteHome = argumentValues.GetString("REMOTE_HOME", string.Empty);
            var imaplog = argumentValues.GetString("IMAP_LOG", null);
            var localOrgStrategyName = argumentValues["LOCALORGSTRAT"];
            var filterAge = argumentValues.GetInt("FILTER_AGE");

            var includeFolderFilter = argumentValues.ContainsKey("FOLDER_INC") ? new Regex(argumentValues["FOLDER_INC"]) : null;
            var excludeFolderFilter = argumentValues.ContainsKey("FOLDER_EXC") ? new Regex(argumentValues["FOLDER_EXC"]) : null;

            IFileSystem fileSystem = new FileSystem();
            if (!fileSystem.DirectoryExists(output))
                fileSystem.CreateDirectory(output);

            var filenamingStrategy = new IdNamingStrategy();
            LocalOrganisationStrategy localOrganisationStrategy = LocalOrganisationStrategy.Create(localOrgStrategyName, output);
            RemoteOrganisationStrategy remoteOrganisationStrategy = remoteMove 
                ? new DatedFolderStructureRemoteOrganisationStrategy(remoteHome) 
                : new PreserveRemoteOrganisationStrategy();

            MailItemCondition messageCondition = new MessageDateAge(filterAge, DateTime.Now);

            using var client = imaplog == null 
                ? new ImapClient()
                : new ImapClient(new ProtocolLogger(imaplog));
                
            client.Connect(server, port, tlsOption);
            client.Authenticate(username, password);
            client.Inbox.Open(FolderAccess.ReadOnly);

            var folderView = RemoteFolderView.Build(defaultLogger, client, includeFolderFilter, excludeFolderFilter);

            var actionText = download ? "Downloading" : "Iterating";
            defaultLogger.WriteLine($"{actionText}...");

            var folderCounter = new EnumerableCounter(folderView.Folders.Count);
            foreach(var folder in folderView.Folders)
            {
                var folderProgressText = folderCounter.Next();

                folder.Open(FolderAccess.ReadOnly);
                var uids = folder.Search(SearchQuery.All);

                defaultLogger.WriteLine($"{actionText} {uids.Count} items from folder '{folder.FullName}' ({folderProgressText})");
                var progress = new ConsoleProgressDisplay();
                progress.Begin(uids.Count);
                foreach (var uid in uids)
                {
                    progress.Update();

                    var message = folder.GetMessage(uid);

                    if(!messageCondition.IsValidItem(message))
                        continue;

                    var remotePath = remoteOrganisationStrategy.Apply(message, folder);
                    if(remotePath != null && !folder.FullName.Equals(remotePath))
                    {
                        var target = folderView.Find(remotePath);
                        if(target == null)
                            target = folderView.Create(remotePath);

                        folder.Open(FolderAccess.ReadWrite);
                        folder.MoveTo(uid, target);
                        //folder.Close();
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

    }
}