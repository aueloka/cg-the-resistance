
namespace Austine.CodinGame.TheResistance.Runtime
{
    using System;

    internal interface IInputReader : IDisposable
    {
        string ReadLine();
    }
}
