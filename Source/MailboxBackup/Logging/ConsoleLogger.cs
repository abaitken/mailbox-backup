using System;

namespace MailboxBackup
{
    class ConsoleLogger : Logger
    {
        public override void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        public override void WriteLine()
        {
            Console.WriteLine();
        }
    }
}