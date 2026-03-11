using System.Collections.Generic;

namespace MailboxBackup.Clients
{
    public interface IRemoteFolder
    {
        string FullName { get; }
        void Close();
        IRemoteFolder Create(string name);
        IList<IMailItem> GetItems();
    }
}
