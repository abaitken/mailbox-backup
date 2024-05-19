using System;
using System.Collections.Generic;

namespace MailboxBackup
{
    public class ArgumentValues
    {
        private readonly List<string> keys;
        private readonly Dictionary<string, string> strings;
        private readonly Dictionary<string, int> ints;
        private readonly Dictionary<string, double> reals;
        private readonly Dictionary<string, bool> bools;

        public ArgumentValues()
        {
            this.keys = new List<String>();
            this.strings = new Dictionary<string, string>();
            this.ints = new Dictionary<string, int>();
            this.reals = new Dictionary<string, double>();
            this.bools = new Dictionary<string, bool>();
        }

        private void Add<T>(string key, T value, Dictionary<string, T> store)
        {
            if (ContainsKey(key))
            {
                store[key] = value;
                return;
            }

            keys.Add(key);
            store.Add(key, value);
        }

        internal void Add(string key, bool value)
        {
            Add(key, value, bools);
        }

        internal void Add(string key, int value)
        {
            Add(key, value, ints);
        }

        internal void Add(string key, double value)
        {
            Add(key, value, reals);
        }

        internal void Add(string key, string value)
        {
            Add(key, value, strings);
        }

        internal bool ContainsKey(string key)
        {
            return keys.Contains(key);
        }

        public string this[string key]
        {
            get { return strings[key]; }
        }

        public string GetString(string key, string defaultValue)
        {
            if (!ContainsKey(key))
                return defaultValue;

            return this[key];
        }

        public string GetString(string key)
        {
            return this[key];
        }

        public bool GetBool(string key)
        {
            return bools[key];
        }

        public int GetInt(string key)
        {
            return ints[key];
        }

        internal double GetReal(string key)
        {
            return reals[key];
        }
    }
}