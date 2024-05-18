using System;
using System.IO;

namespace MailboxBackup
{
    internal class ConsoleHelper
    {
        public static int GetBufferWidthOrDefault(int defaultValue) 
        {
            // Work around for userinteractive
            try
            {
                return Console.BufferWidth;
            }
            catch (IOException)
            {
                return defaultValue;
            }
        }
    }
}