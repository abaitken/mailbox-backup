using System;
using System.Collections;
using System.Collections.Generic;

namespace MailboxBackup.Clients
{
    internal interface IRemoteEndpoint : IDisposable
    {
        IRemoteFolder RootFolder { get; }

        void Disconnect();
        IList<IRemoteFolder> GetAllFolders();
    }
}
