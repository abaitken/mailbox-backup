namespace MailboxBackup
{
    class ArgumentParser
    {
        private readonly string[] _args;

        public ArgumentParser(string[] args)
        {
            // For compatibility
            if (args.Length == 4)
            {
                _args = new[]
                {
                    "-u", args[0],
                    "-p", args[1],
                    "-s", args[2],
                    "-o", args[3]
                };
            }
            else
            {
                _args = args;
            }
        }

        public int Count
        {
            get => _args.Length;
        }

        public string this[string key]
        {
            get
            {
                for (int i = 0; i < _args.Length; i++)
                {
                    var item = _args[i];
                    i++;
                    var value = (i < _args.Length) ? _args[i] : null;

                    if (item == key)
                        return value;
                }

                return null;
            }
        }
    }
}