using System;

namespace MailboxBackup
{
    class Environment : IEnvironment
    {
        public string GetVariable(string key, string defaultValue = null)
        {
            var result = System.Environment.GetEnvironmentVariable(key);

            return string.IsNullOrEmpty(result)
                ? defaultValue
                : result;
        }
    }
}
