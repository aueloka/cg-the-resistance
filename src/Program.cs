
namespace Austine.CodinGame.TheResistance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    //-----------------------------------------------------------------------------------------------------------------
    public interface IMorseDecoder
    {
        Task<int> DecodeAndReturnMessagesCountAsync(string morseSequence, ISet<string> availableWords = null);

        Task<IEnumerable<string>> DecodeAndReturnMessagesAsync(string morseSequence, ISet<string> availableWords = null);
    }
    //-----------------------------------------------------------------------------------------------------------------
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

        private readonly ISet<string> ignoredSequences = new HashSet<string>();
        private readonly ISet<string> decodedMessages = new HashSet<string>();
        private readonly ISet<string> searchStateCache = new HashSet<string>();

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
            this.ignoredSequences.Clear();
            this.searchStateCache.Clear();

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
            await Console.Error.WriteLineAsync($"Cache Size: {this.searchStateCache.Count}");

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
                await Console.Error.WriteLineAsync($"Cached State: {cacheHash}");
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
                    this.ignoredSequences.Add(morseCharacterToDecode);
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
            return !MorseDecoder.MorseDictionary.ContainsKey(morse)
                   || this.ignoredSequences.Contains(morse)
                   ;
        }

        private bool CheckPhraseExists(string phrase)
        {
            if (!this.FirstLetters.Contains(phrase[0]))
            {
                return false;
            }

            foreach (string word in this.WordsByFirstLetter[phrase[0]])
            {
                if (word == phrase || word.StartsWith(phrase))
                {
                    return true;
                }
            }

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
    //-----------------------------------------------------------------------------------------------------------------
    #region InputReaders
    //-----------------------------------------------------------------------------------------------------------------
    internal interface IInputReader: IDisposable
    {
        string ReadLine();
    }
    //-----------------------------------------------------------------------------------------------------------------
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
    //-----------------------------------------------------------------------------------------------------------------
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
    //-----------------------------------------------------------------------------------------------------------------
    #endregion
    internal class Program
    {
        internal static class Config
        {
            public const bool RunWithConsole = false;
            public const bool ShouldGenerateInputOnRun = false;
            public const int InputGeneratorWordCount = 300;
            public const int InputGeneratorWordMin = 3;
            public const int InputGeneratorWordMax = 8;
            public const int InputGeneratorSentenceWordCount = 10;
            public const string InputFilePath = "in.txt";
            public const string OutputFilePath = "out.txt";
        }

        #region Initializers
        private static void Main()
        {
            IInputReader inputReader = Program.GetInputReader();

            string L = inputReader.ReadLine();
            int N = int.Parse(inputReader.ReadLine());

            MorseDecoder decoder = new MorseDecoder();

            for (int i = 0; i < N; i++)
            {
                string W = inputReader.ReadLine();

                if (!decoder.WordsByFirstLetter.ContainsKey(W[0]))
                {
                    decoder.WordsByFirstLetter[W[0]] = new HashSet<string>();
                }

                decoder.WordsByFirstLetter[W[0]].Add(W);
                decoder.FirstLetters.Add(W[0]);
            }

            inputReader.Dispose();

            Console.WriteLine(decoder.DecodeAndReturnMessagesCountAsync(L).GetAwaiter().GetResult());
            Console.Read();
        }

        private static IInputReader GetInputReader()
        {
            IInputReader inputReader;

            if (Config.RunWithConsole)
            {
                inputReader = Program.GetConsoleInputReader();
            }
            else
            {
                inputReader = Program.GetFileInputReader(Config.ShouldGenerateInputOnRun);
            }

            return inputReader;
        }

        private static IInputReader GetConsoleInputReader()
        {
            return new ConsoleInputReader();
        }

        private static IInputReader GetFileInputReader(bool regenerate = false)
        {
            if (regenerate)
            {
                Program.GenerateNewInput();
            }

            return new FileInputReader(Config.InputFilePath);
        }

        private static void GenerateNewInput()
        {
            Random r = new Random(DateTime.Now.Millisecond * DateTime.Now.Millisecond);

            const int wordCount = Config.InputGeneratorWordCount;
            const int wordMin = Config.InputGeneratorWordMin;
            const int wordMax = Config.InputGeneratorWordMax;
            const int sentenceWordCount = Config.InputGeneratorSentenceWordCount;
            const string alphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            IList<string> words = new List<string>();

            for (int i = 0; i < wordCount; i++)
            {
                int wordSize = r.Next(wordMin, wordMax);

                string word = "";

                for (int j = 0; j < wordSize; j++)
                {
                    word += alphabets[r.Next(alphabets.Length)];
                }

                words.Add(word);
            }

            string sentence = "";

            for (int i = 0; i < sentenceWordCount; i++)
            {
                string addedWord = words[r.Next(words.Count)];

                if (i == 0)
                {
                    File.WriteAllText(Config.OutputFilePath, addedWord + ";");
                }
                else
                {
                    File.AppendAllText(Config.OutputFilePath, addedWord + ";");
                }

                Console.WriteLine(addedWord);
                sentence += addedWord;
            }

            Console.WriteLine();
            Console.WriteLine(sentence);

            Dictionary<char, string> morseDisctionary = new Dictionary<char, string>();

            foreach (KeyValuePair<string, char> item in MorseDecoder.MorseDictionary)
            {
                morseDisctionary[item.Value] = item.Key;
            }

            string morse = "";

            foreach (char item in sentence)
            {
                morse += morseDisctionary[item];
            }

            Console.WriteLine(morse);
            Console.WriteLine(morse.Length);

            File.WriteAllText(Config.InputFilePath, morse + Environment.NewLine);
            File.AppendAllText(Config.InputFilePath, words.Count.ToString() + Environment.NewLine);
            File.AppendAllLines(Config.InputFilePath, words);
        }
        #endregion
    }
    //-----------------------------------------------------------------------------------------------------------------
}
