using System;
using System.Runtime;
using AppleCinnamon.Pipeline;

namespace AppleCinnamon
{
    class Program
    {
        static void Main(string[] args)
        {
            var fs = new FullScanner();

            Game g = new Game();
        }

        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
