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

        private T Get<T>(string key, Dictionary<string, T> store)
        {
            return store[key];
        }

        private T Get<T>(string key, T defaultValue, Dictionary<string, T> store)
        {
            if (!ContainsKey(key))
                return defaultValue;

            return store[key];
        }

        public string GetString(string key, string defaultValue)
        {
            return Get(key, defaultValue, strings);
        }

        public string GetString(string key)
        {
            return Get(key, strings);
        }

        public bool GetBool(string key, bool defaultValue)
        {
            return Get(key, defaultValue, bools);
        }

        public bool GetBool(string key)
        {
            return Get(key, bools);
        }

        public int GetInt(string key, int defaultValue)
        {
            return Get(key, defaultValue, ints);
        }

        public int GetInt(string key)
        {
            return Get(key, ints);
        }

        internal double GetReal(string key, double defaultValue)
        {
            return Get(key, defaultValue, reals);
        }

        internal double GetReal(string key)
        {
            return Get(key, reals);
        }
    }
}