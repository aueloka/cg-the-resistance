
namespace Austine.CodinGame.TheResistance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    internal class Program
    {
        internal static class Config
        {
            public const int RunWithConsole = 0;
            public const int GenerateInputOnRun = 1;
            public const int InputGeneratorWordCount = 1000;
            public const int InputGeneratorWordMin = 2;
            public const int InputGeneratorWordMax = 8;
            public const int InputGeneratorSentenceWordCount = 6;
            public const string InputFilePath = @"C:\Users\aueloka\Desktop\in.txt";
            public const string OutputFilePath = @"C:\Users\aueloka\Desktop\out.txt";
        }

        #region Initializers
        private static void Main(string[] args)
        {
            int runWithConsole = Config.RunWithConsole;
            int generateNewInput = Config.GenerateInputOnRun;

            if (runWithConsole == 1)
            {
                RunWithConsoleInput();
            }
            else
            {
                RunWithFileInput(generateNewInput == 1);
            }
        }

        private static void RunWithConsoleInput()
        {
            string L = Console.ReadLine();
            int N = int.Parse(Console.ReadLine());
            //ISet<string> dictionary = new HashSet<string>();

            MorseDecoder decoder = new MorseDecoder();

            for (int i = 0; i < N; i++)
            {
                string W = Console.ReadLine();
                //dictionary.Add(W);

                if (!decoder.WordsByFirstLetter.ContainsKey(W[0]))
                {
                    decoder.WordsByFirstLetter[W[0]] = new HashSet<string>();
                }

                decoder.WordsByFirstLetter[W[0]].Add(W);
                decoder.FirstLetters.Add(W[0]);
            }

            Console.WriteLine(decoder.DecodeAsync(L).GetAwaiter().GetResult());
            Console.Read();
        }

        private static void RunWithFileInput(bool regenerate = false)
        {
            if (regenerate)
            {
                GenerateNewInput();
            }

            StreamReader file = null;
            MorseDecoder decoder = new MorseDecoder();
            string L;

            try
            {
                file = new StreamReader(Config.InputFilePath);

                L = file.ReadLine();
                int N = int.Parse(file.ReadLine());

                for (int i = 0; i < N; i++)
                {
                    string W = file.ReadLine();

                    if (!decoder.WordsByFirstLetter.ContainsKey(W[0]))
                    {
                        decoder.WordsByFirstLetter[W[0]] = new HashSet<string>();
                    }

                    decoder.WordsByFirstLetter[W[0]].Add(W);
                    decoder.FirstLetters.Add(W[0]);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }

            Console.WriteLine(decoder.DecodeAsync(L).GetAwaiter().GetResult());
            Console.Read();
        }

        private static void GenerateNewInput()
        {
            Random r = new Random(DateTime.Now.Millisecond * DateTime.Now.Millisecond);

            int wordCount = Config.InputGeneratorWordCount;
            int wordMin = Config.InputGeneratorWordMin;
            int wordMax = Config.InputGeneratorWordMax;
            int sentenceWordCount = Config.InputGeneratorSentenceWordCount;

            string alphabets = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            List<string> words = new List<string>();

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

    public interface IMorseDecoder
    {
        Task<int> DecodeAsync(string morseSequence, ISet<string> availableWords = null);
    }

    public class MorseDecoder : IMorseDecoder
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

        private readonly ISet<string> ignoredSequences = new HashSet<string>();
        private readonly ISet<string> decodedMessages = new HashSet<string>();
        private readonly ISet<string> allCache = new HashSet<string>();

        public ISet<char> FirstLetters { get; set; } = new HashSet<char>();

        public IDictionary<char, ISet<string>> WordsByFirstLetter { get; set; } = new Dictionary<char, ISet<string>>();

        public int DecodedMessageCount => this.decodedMessages.Count;

        public string MorseSequence { get; set; }

        public async Task<int> DecodeAsync(string morseSequence, ISet<string> availableWords = null)
        {
            this.MorseSequence = morseSequence;
            this.decodedMessages.Clear();
            this.ignoredSequences.Clear();
            this.allCache.Clear();

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

            await Console.Error.WriteLineAsync($"Elapsed: {stopwatch.ElapsedMilliseconds} ms");
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
            if (this.allCache.Contains(cacheHash))
            {
                // await Console.Error.WriteLineAsync($"Encountered cached message: {cacheHash}");
                return;
            }

            if (currentIndex == this.MorseSequence.Length && currentTrackingContext.Key.Length == 0)
            {
                this.decodedMessages.Add(preceedingMessage);
                await Console.Error.WriteLineAsync($"Adding decoded message: {preceedingMessage}");
                return;
            }


            string searchScope = this.MorseSequence.Substring(currentIndex, Math.Min(4, this.MorseSequence.Length - currentIndex));

            ISet<KeyValuePair<string, string>> validSequences = this.GetValidSequencesFromMorse(searchScope, currentTrackingContext);

            this.allCache.Add(cacheHash);

            foreach (KeyValuePair<string, string> newTrackingContext in validSequences)
            {
                int newIndex = newTrackingContext.Key.Length - currentTrackingContext.Key.Length + currentIndex;

                await this.SearchMorseSequenceAsync(newIndex, newTrackingContext, preceedingMessage);

                if (this.WordsByFirstLetter[newTrackingContext.Value[0]].Contains(newTrackingContext.Value))
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
            if (string.IsNullOrEmpty(morse) || context.Key == morse)
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
                this.ProcessMorseCombo(morse, context, validSequences, combo);

                if (true)
                {
                    continue;
                }
            }

            return validSequences;
        }

        private void ProcessMorseCombo(
            string morse,
            KeyValuePair<string, string> context,
            ISet<KeyValuePair<string, string>> validSequences,
            string combo)
        {
            string comboMorseKey = "";
            string comboMorseValue = "";

            int nextIndex = 0;
            for (int i = 0; i < combo.Length; i++)
            {
                char morseCharacterDef = combo[i];
                int countToProcess = int.Parse(morseCharacterDef.ToString());

                if (nextIndex + countToProcess > morse.Length)
                {
                    return;
                }

                string subMorse = morse.Substring(nextIndex, countToProcess);

                if (this.ShouldIgnoreMorse(subMorse))
                {
                    this.ignoredSequences.Add(subMorse);
                    return;
                }

                nextIndex += countToProcess;

                comboMorseKey += subMorse;
                comboMorseValue += MorseDecoder.MorseDictionary[subMorse];

                if (string.IsNullOrEmpty(comboMorseKey))
                {
                    continue;
                }

                this.CompareAndAddMorseCombo(context, comboMorseKey, comboMorseValue, validSequences);
            }
        }

        private void CompareAndAddMorseCombo(
            KeyValuePair<string, string> context,
            string comboMorseKey,
            string comboMorseValue,
            ISet<KeyValuePair<string, string>> validSequences)
        {
            string contextPhrase = context.Value + comboMorseValue;
            string contextMorse = context.Key + comboMorseKey;

            if (this.CheckPhraseExists(contextPhrase))
            {
                validSequences.Add(new KeyValuePair<string, string>(contextMorse, contextPhrase));
            }
        }

        private bool ShouldIgnoreMorse(string subMorse)
        {
            return !MorseDecoder.MorseDictionary.ContainsKey(subMorse)
                   || this.ignoredSequences.Contains(subMorse)
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
    }
}
