using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTBatcher
{
    internal static class Logger
    {
        internal static void Log(object data, params object[] args)
        {
            Console.WriteLine(data.ToString(), args);
            Title(data, args);
        }

        internal static void Title(object data, params object[] args)
        {
            Console.Title = string.Format(data.ToString(), args);
        }
    }
}
