using System;
using System.Collections.Generic;

namespace MailboxBackup
{
    internal static class StringExtensions
    {
        public static IEnumerable<string> SplitStringIntoLengths(this string subject, int length)
        {
            if(subject.Length <= length)
            {
                yield return subject;
                yield break;
            }

            int index = 0;
            while(index < subject.Length)
            {
                var endIndex = Math.Min(index + length, subject.Length);
                var line = subject[index..endIndex];
                yield return line;
                index = endIndex;
            }
        }
    }
}