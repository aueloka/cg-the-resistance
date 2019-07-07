
namespace Austine.CodinGame.TheResistance.Tests.Unit
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MorseDecoderTests
    {
        private MorseDecoder decoder;

        [TestInitialize]
        public void Setup()
        {
            ISet<string> dictionary = MorseDecoderTests.GetDefaultDictionary();

            this.decoder = new MorseDecoder()
            {
                MorseSequence = "--.----.......-.---.--......-..",
                FirstLetters = MorseDecoderTests.GetFirstLetters(dictionary),
                WordsByFirstLetter = MorseDecoderTests.GetWordsByFirstLetter(dictionary)
            };
        }

        [TestMethod]
        public async Task TestGodIsNowHere()
        {
            string morse = "--.----.......-.---.--......-..";

            int solution = await this.decoder.DecodeAsync(morse, MorseDecoderTests.GetDefaultDictionary());
            Assert.AreEqual(6, solution);
        }

        [TestMethod]
        public void TestGetValidChildren()
        {
            string morse = "--.-";

            ISet<string> dictionary = new HashSet<string>
            {
                "MA", "G", "M", "A", "T", "TNT"
            };

            this.decoder = new MorseDecoder()
            {
                FirstLetters = MorseDecoderTests.GetFirstLetters(dictionary),
                WordsByFirstLetter = MorseDecoderTests.GetWordsByFirstLetter(dictionary)
            };

            ISet<KeyValuePair<string, string>> result = this.decoder.DecodeMorse(morse);
            Assert.AreEqual(6, result.Count);
        }

        [TestMethod]
        public async Task TestSearchSequence()
        {
            await this.decoder.SearchAndDecodeMorseSequenceAsync();
            Assert.AreEqual(6, this.decoder.DecodedMessageCount);
        }

        [TestMethod]
        public async Task TestHelloWorld()
        {
            ISet<string> dictionary = new HashSet<string>
            {
                "HELL", "HELLO", "OWORLD", "WORLD", "TEST"
            };

            this.decoder = new MorseDecoder()
            {
                MorseSequence = "......-...-..---.-----.-..-..-..",
                FirstLetters = MorseDecoderTests.GetFirstLetters(dictionary),
                WordsByFirstLetter = MorseDecoderTests.GetWordsByFirstLetter(dictionary)
            };

            await this.decoder.SearchAndDecodeMorseSequenceAsync();
            Assert.AreEqual(2, this.decoder.DecodedMessageCount);
        }

        [TestMethod]
        public async Task TestElapsedTime()
        {
            int runCount = 10;
            long[] runTimes = new long[runCount];

            for (int i = 0; i < runCount; i++)
            {
                runTimes[i] = await this.RunAndMeasureElapsedMs();
            }

            double sum = runTimes.Sum(runtime => runtime);
            double average = sum / runCount + 0.0;

            Assert.IsTrue(average < 0.51);
        }

        private async Task<long> RunAndMeasureElapsedMs()
        {
            string morse = "--.----.......-.---.--......-..";

            ISet<string> dictionary = new HashSet<string>
            {
                "GOD", "IS", "NO", "NOW", "HERE", "WHERE",
                "HER", "GO", "ME", "MED", "MOD", "MO",
            };

            this.decoder = new MorseDecoder()
            {
                MorseSequence = morse,
                FirstLetters = MorseDecoderTests.GetFirstLetters(dictionary),
                WordsByFirstLetter = MorseDecoderTests.GetWordsByFirstLetter(dictionary)
            };

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await this.decoder.SearchAndDecodeMorseSequenceAsync();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private static ISet<string> GetDefaultDictionary()
        {
            return new HashSet<string>
            {
                "GOD", "IS", "NO", "NOW", "HERE", "WHERE",
                "HER", "GO", "ME", "MED", "MOD", "MO", "E"
            };
        }

        private static ISet<char> GetFirstLetters(ISet<string> dictionary)
        {
            ISet<char> result = new HashSet<char>();

            foreach (string word in dictionary)
            {
                result.Add(word[0]);
            }

            return result;
        }

        private static IDictionary<char, ISet<string>> GetWordsByFirstLetter(ISet<string> dictionary)
        {
            IDictionary<char, ISet<string>> result = new Dictionary<char, ISet<string>>();

            foreach (string word in dictionary)
            {
                if (!result.ContainsKey(word[0]))
                {
                    result[word[0]] = new HashSet<string>();
                }

                result[word[0]].Add(word);
            }

            return result;
        }
    }
}
