using System;
using System.Collections.Generic;
using System.Text;

namespace MailboxBackup
{
    internal static class EnumerableExtensions
    {
        public static T? FirstOrNull<T>(this IEnumerable<T> values, Func<T, bool> predicate)
            where T : struct
        {
            foreach (var item in values)
            {
                if (predicate(item))
                    return item;
            }

            return null;
        }

        public static string Combine<T>(this IEnumerable<T> values, string glue)
        {
            var result = new StringBuilder();
            
            foreach (var item in values)
            {
                if(result.Length != 0)
                    result.Append(glue);
                
                result.Append(item);
            }

            return result.ToString();
        }
    }
}