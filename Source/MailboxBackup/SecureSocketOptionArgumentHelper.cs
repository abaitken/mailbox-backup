using MailKit.Security;
using System;
using System.Collections.Generic;

namespace MailboxBackup
{
    class SecureSocketOptionArgumentHelper
    {
        public static SecureSocketOptions ToSecureSocketOptions(string value)
        {
            return value switch
            {
                "None" => SecureSocketOptions.None,
                "Auto" => SecureSocketOptions.Auto,
                "SslOnConnect" => SecureSocketOptions.SslOnConnect,
                "StartTls" => SecureSocketOptions.StartTls,
                "StartTlsWhenAvailable" => SecureSocketOptions.StartTlsWhenAvailable,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };

        }

        public static string DefaultValue
        {
            get =>  "SslOnConnect";
        }
        public static IEnumerable<string> Values
        {
            get => new[] { "None", "Auto", "SslOnConnect", "StartTls", "StartTlsWhenAvailable" };
        }
    }
}