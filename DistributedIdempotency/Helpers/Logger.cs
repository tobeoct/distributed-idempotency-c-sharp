using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedIdempotency.Helpers
{
    internal static class Logger
    {
        public static void Debug(string message)
        {
            Console.WriteLine(message);
        }

        public static void Info(string message)
        {
            Console.WriteLine(message);
        }

        public static void Error(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        public static void Warn(string message)
        {
            Console.WriteLine(message);
        }
    }
}
