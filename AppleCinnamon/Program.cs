using System;
using System.Threading;
using AppleCinnamon.Pipeline;

namespace AppleCinnamon
{
    
    class Program
    {
        [STAThread]
        static void Main()
        {
            
            Game g = new Game();
        }

        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
