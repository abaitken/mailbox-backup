using MimeKit;

namespace MailboxBackup
{
    abstract class MailItemCondition
    {
        public abstract bool IsValidItem(IMimeMessage message);
    }
}