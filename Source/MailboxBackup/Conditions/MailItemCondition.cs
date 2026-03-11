using MailboxBackup.Clients;

namespace MailboxBackup
{
    abstract class MailItemCondition
    {
        public abstract bool IsValidItem(IMailItem message);
    }
}