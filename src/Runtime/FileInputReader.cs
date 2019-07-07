
namespace Austine.CodinGame.TheResistance.Runtime
{
    using System.IO;

    internal sealed class FileInputReader : IInputReader
    {
        private readonly StreamReader file;

        public FileInputReader(string filePath)
        {
            this.file = new StreamReader(filePath);
        }

        public string ReadLine()
        {
            return this.file.ReadLine();
        }

        public void Dispose()
        {
            if (this.file != null)
            {
                this.file.Close();
            }
        }
    }
}
