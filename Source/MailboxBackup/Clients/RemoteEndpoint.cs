using System;
using System.Collections.Generic;

namespace MailboxBackup.Clients
{
    abstract class RemoteEndpoint : IRemoteEndpoint
    {
        public abstract IRemoteFolder RootFolder { get; }

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public abstract void Disconnect();
        public abstract IList<IRemoteFolder> GetAllFolders();
    }
}
