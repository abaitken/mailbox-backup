namespace MailboxBackup
{
    abstract class Logger
    {
        public abstract void WriteLine();
        public abstract void WriteLine(string text);
    }
}