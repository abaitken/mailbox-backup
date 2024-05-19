namespace MailboxBackup
{
    class EnumerableCounter
    {
        private readonly int total;
        private int current;

        public EnumerableCounter(int total)
        {
            this.total = total;
            this.current = 0;
        }

        public string Next()
        {
            current++;
            return $"{current}/{total}";
        }
    }
}