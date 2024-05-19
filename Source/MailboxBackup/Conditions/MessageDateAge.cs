using System;
using MimeKit;

namespace MailboxBackup
{
    class MessageDateAge : MailItemCondition
    {
        private readonly DateTime before;

        public MessageDateAge(int ageDays, DateTime now)
        {
            this.before = now.Subtract(TimeSpan.FromDays(ageDays));
        }

        public override bool IsValidItem(IMimeMessage message)
        {
            return message.Date <= before;
        }
    }
}