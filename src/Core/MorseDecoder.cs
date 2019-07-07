
namespace Austine.CodinGame.TheResistance.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public sealed class MorseDecoder : IMorseDecoder
    {
        public static readonly IDictionary<string, char> MorseDictionary = new Dictionary<string, char>
        {
            { ".-", 'A' }, { "-...", 'B' }, { "-.-.", 'C' }, { "-..", 'D' },
            { ".", 'E' }, { "..-.", 'F' }, { "--.", 'G' }, { "....", 'H' },
            { "..", 'I' }, { ".---", 'J' }, { "-.-", 'K' }, { ".-..", 'L' },
            { "--", 'M' }, { "-.", 'N' }, { "---", 'O' }, { ".--.", 'P' },
            { "--.-", 'Q' }, { ".-.", 'R' }, { "...", 'S' }, { "-", 'T' },
            { "..-", 'U' }, { "...-", 'V' }, { ".--", 'W' }, { "-..-", 'X' },
            { "-.--", 'Y' }, { "--..", 'Z' },
        };

        private static readonly IList<string> DecodingCombinations = new List<string>
        {
            "1111", "112", "121", "13", "211", "22", "31", "4"
        };

        private const int MorseCharacterMaxLength = 4;

        private readonly ISet<string> decodedMessages = new HashSet<string>();
        private readonly ISet<string> searchStateCache = new HashSet<string>();
        private readonly IDictionary<string, bool> phraseCache = new Dictionary<string, bool>();

        public ISet<char> FirstLetters { get; set; } = new HashSet<char>();

        public IDictionary<char, ISet<string>> WordsByFirstLetter { get; set; } = new Dictionary<char, ISet<string>>();

        public int DecodedMessageCount => this.decodedMessages.Count;

        public string MorseSequence { get; set; }

        /// <summary>
        /// Returns the number of possible messages embedded in the specified <paramref name="morseSequence"/>
        /// </summary>
        public async Task<int> DecodeAndReturnMessagesCountAsync(string morseSequence, ISet<string> availableWords = null)
        {
            this.MorseSequence = morseSequence;
            this.decodedMessages.Clear();
            this.searchStateCache.Clear();
            this.phraseCache.Clear();

            await Console.Error.WriteLineAsync($"Decoding Morse Sequence: {morseSequence}");
            await Console.Error.WriteLineAsync($"Sequence Length: {morseSequence.Length}");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                await this.DecodeMorseSequenceAsync();
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.StackTrace);
            }

            stopwatch.Stop();

            await Console.Error.WriteLineAsync($"{Environment.NewLine}Elapsed: {stopwatch.ElapsedMilliseconds} ms");
            await Console.Error.WriteLineAsync($"Decode Cache Size: {this.searchStateCache.Count}");
            await Console.Error.WriteLineAsync($"Phrase Cache Size: {this.phraseCache.Count}");

            this.searchStateCache.Clear();
            this.phraseCache.Clear();

            return this.DecodedMessageCount;
        }

        /// <summary>
        /// Returns all the possible messages embedded in the specified <paramref name="morseSequence"/>
        /// </summary>
        public async Task<IEnumerable<string>> DecodeAndReturnMessagesAsync(string morseSequence, ISet<string> availableWords = null)
        {
            await this.DecodeAndReturnMessagesCountAsync(morseSequence, availableWords);
            return this.decodedMessages;
        }

        /// <summary>
        /// Recursively decodes the current <see cref="MorseSequence"/> beginning from the specified <paramref name="startIndex"/>
        /// </summary>
        /// <param name="currentDecodedContext">If <paramref name="startIndex"/> is not 0, this may contain decoding information
        /// from previous positions.</param>
        /// <param name="startIndex">The position to start decoding from in the <see cref="MorseSequence"/></param>
        /// <param name="currentDecodedMessage">Full string contain the state of the <see cref="MorseSequence"/> decoding till
        /// the <paramref name="startIndex"/></param>
        /// <returns></returns>
        public async Task DecodeMorseSequenceAsync(
            KeyValuePair<string, string> currentDecodedContext = default,
            int startIndex = default,
            string currentDecodedMessage = "")
        {
            if (startIndex > this.MorseSequence.Length)
            {
                await Console.Error.WriteLineAsync($"Index: {startIndex} has exceeded morse length: {this.MorseSequence.Length}");
                return;
            }

            if (currentDecodedContext.Key == null || currentDecodedContext.Value == null)
            {
                currentDecodedContext = MorseDecoder.GetEmptyDecodedContext();
            }

            string cacheHash = currentDecodedMessage + startIndex + currentDecodedContext.Value;
            if (this.searchStateCache.Contains(cacheHash))
            {
                return;
            }

            this.searchStateCache.Add(cacheHash);

            //message is fully decoded when search reaches the end of the entire morse sequence
            //and no sequence is currently being decoded
            if (startIndex == this.MorseSequence.Length && currentDecodedContext.Key.Length == 0)
            {
                this.decodedMessages.Add(currentDecodedMessage.Trim());
                await Console.Error.WriteLineAsync($"Adding decoded message: {currentDecodedMessage}");
                return;
            }

            string morseToDecode = this.MorseSequence.Substring(
                startIndex, Math.Min(MorseDecoder.MorseCharacterMaxLength, this.MorseSequence.Length - startIndex));

            ISet<KeyValuePair<string, string>> decodedSequences = this.DecodeMorse(morseToDecode, currentDecodedContext);

            foreach (KeyValuePair<string, string> newDecodedContext in decodedSequences)
            {
                int newIndex = newDecodedContext.Key.Length - currentDecodedContext.Key.Length + startIndex;

                await this.DecodeMorseSequenceAsync(newDecodedContext, newIndex, currentDecodedMessage);

                if (!this.CheckWordExists(newDecodedContext.Value))
                {
                    continue;
                }

                string newPreceedingMessage = currentDecodedMessage + newDecodedContext.Value + " ";
                await Console.Error.WriteLineAsync($"New sequence discovered: {newPreceedingMessage}\n");

                //clear decoded context to start attempting new words
                await this.DecodeMorseSequenceAsync(MorseDecoder.GetEmptyDecodedContext(), newIndex, newPreceedingMessage);
            }
        }

        /// <summary>
        /// Returns a list of possible character combinations from the given morse input.
        /// 
        /// The input morse should have a limit not greater than <see cref="MorseCharacterMaxLength"/>.
        /// This limit is not enforced but if violated, could result in unexpected behaviours.
        /// </summary>
        /// <param name="morse">The morse to decode</param>
        /// <param name="currentDecodedContext">Possible pre-decoded sequence that the new outputs will be appended to,</param>
        public ISet<KeyValuePair<string, string>> DecodeMorse(string morse, KeyValuePair<string, string> currentDecodedContext = default)
        {
            if (string.IsNullOrEmpty(morse))
            {
                return new HashSet<KeyValuePair<string, string>>();
            }

            if (currentDecodedContext.Key == null || currentDecodedContext.Value == null)
            {
                currentDecodedContext = MorseDecoder.GetEmptyDecodedContext();
            }

            ISet<KeyValuePair<string, string>> decodedSequences = new HashSet<KeyValuePair<string, string>>();

            //Try all combinations to find decoded messages
            foreach (string decodeCombination in MorseDecoder.DecodingCombinations)
            {
                this.DecodeMorseWithCombination(morse, decodeCombination, currentDecodedContext, decodedSequences);
            }

            return decodedSequences;
        }

        private void DecodeMorseWithCombination(
            string morse,
            string decodeCombination,
            KeyValuePair<string, string> currentDecodedContext,
            ISet<KeyValuePair<string, string>> decodedSequences)
        {
            string morseSequenceProcessed = "";
            string morseSequenceTranslated = "";
            int decodingIndex = 0;

            foreach (char decodingCharacterSizeChar in decodeCombination)
            {
                int decodingCharacterSize = int.Parse(decodingCharacterSizeChar.ToString());

                if (decodingIndex + decodingCharacterSize > morse.Length)
                {
                    return;
                }

                string morseCharacterToDecode = morse.Substring(decodingIndex, decodingCharacterSize);

                if (this.ShouldIgnoreMorse(morseCharacterToDecode))
                {
                    //ignore characters that are invalid in the current state
                    return;
                }

                decodingIndex += decodingCharacterSize;
                morseSequenceProcessed += morseCharacterToDecode;
                morseSequenceTranslated += MorseDecoder.MorseDictionary[morseCharacterToDecode];

                if (string.IsNullOrEmpty(morseSequenceProcessed))
                {
                    continue;
                }

                this.ValidateDecodedMorseSequence(morseSequenceProcessed, morseSequenceTranslated, currentDecodedContext, decodedSequences);
            }
        }

        private void ValidateDecodedMorseSequence(
            string morseSequenceProcessed,
            string morseSequenceTranslated,
            KeyValuePair<string, string> currentDecodedContext,
            ISet<KeyValuePair<string, string>> decodedSequences)
        {
            string fullSequenceTranslated = currentDecodedContext.Value + morseSequenceTranslated;

            if (!this.CheckPhraseExists(fullSequenceTranslated))
            {
                return;
            }

            string fullSequenceProcessed = currentDecodedContext.Key + morseSequenceProcessed;
            decodedSequences.Add(new KeyValuePair<string, string>(fullSequenceProcessed, fullSequenceTranslated));
        }

        private bool ShouldIgnoreMorse(string morse)
        {
            return !MorseDecoder.MorseDictionary.ContainsKey(morse);
        }

        private bool CheckPhraseExists(string phrase)
        {
            if (string.IsNullOrEmpty(phrase) || !this.FirstLetters.Contains(phrase[0]))
            {
                return false;
            }

            if (this.phraseCache.ContainsKey(phrase))
            {
                return this.phraseCache[phrase];
            }

            foreach (string word in this.WordsByFirstLetter[phrase[0]])
            {
                if (word == phrase || word.StartsWith(phrase))
                {
                    this.phraseCache[phrase] = true;
                    return true;
                }
            }

            this.phraseCache[phrase] = false;
            return false;
        }

        private bool CheckWordExists(string word)
        {
            if (string.IsNullOrEmpty(word) || !this.FirstLetters.Contains(word[0]))
            {
                return false;
            }

            return this.WordsByFirstLetter[word[0]].Contains(word);
        }

        private static KeyValuePair<string, string> GetEmptyDecodedContext()
        {
            return new KeyValuePair<string, string>("", "");
        }
    }
}
