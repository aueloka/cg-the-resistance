
namespace Austine.CodinGame.TheResistance.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMorseDecoder
    {
        Task<int> DecodeAndReturnMessagesCountAsync(string morseSequence, ISet<string> availableWords = null);

        Task<IEnumerable<string>> DecodeAndReturnMessagesAsync(string morseSequence, ISet<string> availableWords = null);
    }
}
