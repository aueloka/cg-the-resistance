
namespace Austine.CodinGame.TheResistance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Austine.CodinGame.TheResistance.Core;
    using Austine.CodinGame.TheResistance.Runtime;

    internal class Program
    {
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

            if (Config.ReadInputFromConsole)
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
