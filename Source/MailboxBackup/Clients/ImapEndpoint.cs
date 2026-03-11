using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Graph.Models;
using MimeKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SearchQuery = MailKit.Search.SearchQuery;

namespace MailboxBackup.Clients
{
    class ImapEndpoint : RemoteEndpoint
    {
        private bool _disposedValue;
        private readonly ImapClient _client;

        public override IRemoteFolder RootFolder => new RemoteFolder(_client.GetFolder(_client.PersonalNamespaces[0]));

        class MailItem : IMailItem
        {
            private readonly MimeMessage _message;
            private readonly IMailFolder _folder;
            private readonly UniqueId _uid;
            private bool _disposedValue;

            public string UniqueId => _uid.ToString();

            public DateTimeOffset Date => _message.Date;

            public MailItem(IMailFolder folder, UniqueId uid)
            {
                _message = folder.GetMessage(uid);
                _folder = folder;
                _uid = uid;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _message.Dispose();
                    }

                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                System.GC.SuppressFinalize(this);
            }

            public void MoveTo(IRemoteFolder target)
            {
                _folder.Open(FolderAccess.ReadWrite);
                _folder.MoveTo(_uid, ((RemoteFolder)target).Folder);
                //folder.Close();
            }

            public void WriteToDisk(string destination)
            {
                _message.WriteTo(destination);
            }
        }

        class RemoteFolder : IRemoteFolder
        {
            private readonly IMailFolder _folder;

            public RemoteFolder(IMailFolder folder)
            {
                _folder = folder;
            }

            public string FullName => Folder.FullName;

            public IMailFolder Folder => _folder;

            public void Close()
            {
                throw new System.NotImplementedException();
            }

            public IRemoteFolder Create(string name)
            {
                return new RemoteFolder(_folder.Create(name, true));
            }

            public IList<IMailItem> GetItems()
            {
                Folder.Open(FolderAccess.ReadOnly);
                var uids = Folder.Search(SearchQuery.All);

                // TODO : Consider replacing with a smart class which has the uids and gets the MimeMessage on demand
                var messages = from item in uids
                               select (IMailItem)new MailItem(Folder, item);

                return messages.ToList();
            }
        }

        public ImapEndpoint(ImapClient client)
        {
            _client = client;
        }

        public override void Disconnect()
        {
            _client.Disconnect(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                _disposedValue = true;
            }
        }

        public static IRemoteEndpoint Create(ArgumentValues argumentValues)
        {
            var username = argumentValues["USER"];
            var password = argumentValues["PASS"];
            var server = argumentValues["SERVER"];
            var port = argumentValues.GetInt("SERVER_PORT");
            var tlsOption = SecureSocketOptionArgumentHelper.ToSecureSocketOptions(argumentValues["TLSMODE"]);
            var imaplog = argumentValues.GetString("IMAP_LOG", null);
            var client = imaplog == null
                ? new ImapClient()
                : new ImapClient(new ProtocolLogger(imaplog));


            client.Connect(server, port, tlsOption);
            client.Authenticate(username, password);

            client.Inbox.Open(FolderAccess.ReadOnly);
            return new ImapEndpoint(client);
        }

        public override IList<IRemoteFolder> GetAllFolders()
        {
            var mailFolders = (from FolderNamespace ns in _client.PersonalNamespaces
                               from folder in _client.GetFolders(ns)
                               select (IRemoteFolder)new RemoteFolder(folder));

            return mailFolders.ToList();
        }
    }
}
