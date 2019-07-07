
namespace Austine.CodinGame.TheResistance.Runtime
{
    using System;

    internal sealed class ConsoleInputReader : IInputReader
    {
        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public void Dispose()
        {
            return;
        }
    }
}
