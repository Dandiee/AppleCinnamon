using System;
using AppleCinnamon.Pipeline;

namespace AppleCinnamon
{
    
    class Program
    {
        [STAThread]
        static void Main()
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
