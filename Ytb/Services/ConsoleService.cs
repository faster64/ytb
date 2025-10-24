using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ytb.Services
{
    public class ConsoleService
    {
        private static object _lock = new object();

        public static void WriteLineSuccess(string text)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }

        public static void WriteLineError(string text)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }
    }
}
