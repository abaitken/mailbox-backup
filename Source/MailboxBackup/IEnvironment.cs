using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailboxBackup
{
    internal interface IEnvironment
    {
        string GetVariable(string key, string defaultValue = null);
    }
}
