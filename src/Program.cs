
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
        Task<int> DecodeAsync(string morseSequence, ISet<string> availableWords = null);
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

        private static readonly IList<string> SequenceCombinations = new List<string>
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

        public async Task<int> DecodeAsync(string morseSequence, ISet<string> availableWords = null)
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
                await this.SearchMorseSequenceAsync(0, this.GetEmptyContext());
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

        public async Task SearchMorseSequenceAsync(
            int currentIndex,
            KeyValuePair<string, string> currentTrackingContext,
            string preceedingMessage = "")
        {
            if (currentIndex > this.MorseSequence.Length)
            {
                await Console.Error.WriteLineAsync($"Index: {currentIndex} has exceeded morse length: {this.MorseSequence.Length}");
                return;
            }

            string cacheHash = preceedingMessage + currentIndex + currentTrackingContext.Value;
            if (this.searchStateCache.Contains(cacheHash))
            {
                await Console.Error.WriteLineAsync($"Cached State: {cacheHash}");
                return;
            }

            if (currentIndex == this.MorseSequence.Length && currentTrackingContext.Key.Length == 0)
            {
                this.decodedMessages.Add(preceedingMessage);
                await Console.Error.WriteLineAsync($"Adding decoded message: {preceedingMessage}");
                return;
            }

            string searchScope = this.MorseSequence.Substring(
                currentIndex, Math.Min(MorseDecoder.MorseCharacterMaxLength, this.MorseSequence.Length - currentIndex));

            ISet<KeyValuePair<string, string>> validSequences = this.GetValidSequencesFromMorse(searchScope, currentTrackingContext);

            this.searchStateCache.Add(cacheHash);

            foreach (KeyValuePair<string, string> newTrackingContext in validSequences)
            {
                int newIndex = newTrackingContext.Key.Length - currentTrackingContext.Key.Length + currentIndex;

                await this.SearchMorseSequenceAsync(newIndex, newTrackingContext, preceedingMessage);

                if (this.CheckWordExists(newTrackingContext.Value))
                {
                    string newPreceedingMessage = preceedingMessage + newTrackingContext.Value + ";";
                    await Console.Error.WriteLineAsync($"New sequence discovered: {newPreceedingMessage}\n");

                    //fork search to track new word
                    await this.SearchMorseSequenceAsync(newIndex, this.GetEmptyContext(), newPreceedingMessage);
                }
            }
        }

        public ISet<KeyValuePair<string, string>> GetValidSequencesFromMorse(string morse, KeyValuePair<string, string> context)
        {
            if (string.IsNullOrEmpty(morse))
            {
                return new HashSet<KeyValuePair<string, string>>();
            }

            if (context.Key == null || context.Value == null)
            {
                context = this.GetEmptyContext();
            }

            ISet<KeyValuePair<string, string>> validSequences = new HashSet<KeyValuePair<string, string>>();

            foreach (string combo in MorseDecoder.SequenceCombinations)
            {
                this.ProcessMorseCombo(morse, combo, context, validSequences);
            }

            return validSequences;
        }

        private void ProcessMorseCombo(
            string morse,
            string combo,
            KeyValuePair<string, string> currentProcessingContext,
            ISet<KeyValuePair<string, string>> validatedSequences)
        {
            string morseSequenceProcessed = "";
            string morseSequenceTranslated = "";

            int nextMorseIndex = 0;
            foreach (char morseCharacterProcessSizeChar in combo)
            {
                int morseCharacterProcessSize = int.Parse(morseCharacterProcessSizeChar.ToString());

                if (nextMorseIndex + morseCharacterProcessSize > morse.Length)
                {
                    return;
                }

                string morseCharacterToProcess = morse.Substring(nextMorseIndex, morseCharacterProcessSize);

                if (this.ShouldIgnoreMorse(morseCharacterToProcess))
                {
                    this.ignoredSequences.Add(morseCharacterToProcess);
                    return;
                }

                nextMorseIndex += morseCharacterProcessSize;

                morseSequenceProcessed += morseCharacterToProcess;
                morseSequenceTranslated += MorseDecoder.MorseDictionary[morseCharacterToProcess];

                if (string.IsNullOrEmpty(morseSequenceProcessed))
                {
                    continue;
                }

                this.ValidateProcessedMorseSequence(morseSequenceProcessed, morseSequenceTranslated, currentProcessingContext, validatedSequences);
            }
        }

        private void ValidateProcessedMorseSequence(
            string morseSequenceProcessed,
            string morseSequenceTranslated,
            KeyValuePair<string, string> currentTranslatedContext,
            ISet<KeyValuePair<string, string>> validSequences)
        {
            string fullSequenceProcessed = currentTranslatedContext.Key + morseSequenceProcessed;
            string fullSequenceTranslated = currentTranslatedContext.Value + morseSequenceTranslated;

            if (this.CheckPhraseExists(fullSequenceTranslated))
            {
                validSequences.Add(new KeyValuePair<string, string>(fullSequenceProcessed, fullSequenceTranslated));
            }
        }

        private bool ShouldIgnoreMorse(string morse)
        {
            return !MorseDecoder.MorseDictionary.ContainsKey(morse)
                   || this.ignoredSequences.Contains(morse)
                   ;
        }

        private KeyValuePair<string, string> GetEmptyContext()
        {
            return new KeyValuePair<string, string>("", "");
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

            Console.WriteLine(decoder.DecodeAsync(L).GetAwaiter().GetResult());
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
