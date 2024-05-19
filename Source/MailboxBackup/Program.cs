using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailboxBackup
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new App(new ConsoleLogger());
            return app.Run(args);
        }
    }
}
