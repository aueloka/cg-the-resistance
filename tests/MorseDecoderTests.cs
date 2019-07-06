
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
            this.decoder = new MorseDecoder()
            {
                MorseSequence = "--.----.......-.---.--......-..",
                FirstLetters = GetDefaultFirstLetters(GetDefaultDictionary()),
                WordsByFirstLetter = GetDefaultWordsByFirstLetter(GetDefaultDictionary())
            };
        }

        private static ISet<string> GetDefaultDictionary()
        {
            return new HashSet<string>
            {
                "GOD", "IS", "NO", "NOW", "HERE", "WHERE",
                "HER", "GO", "ME", "MED", "MOD", "MO", "E"
            };
        }

        private static ISet<char> GetDefaultFirstLetters(ISet<string> dictionary)
        {
            ISet<char> result = new HashSet<char>();

            foreach (string word in dictionary)
            {
                result.Add(word[0]);
            }

            return result;
        }

        private static IDictionary<char, ISet<string>> GetDefaultWordsByFirstLetter(ISet<string> dictionary)
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

        [TestMethod]
        public async Task TestGodIsNowHere()
        {
            string morse = "--.----.......-.---.--......-..";

            int solution = await this.decoder.DecodeAsync(morse, GetDefaultDictionary());
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
                //AvailableWords = GetDefaultDictionary(),
                FirstLetters = GetDefaultFirstLetters(dictionary),
                WordsByFirstLetter = GetDefaultWordsByFirstLetter(dictionary)
            };

            IEnumerable<KeyValuePair<string, string>> result = this.decoder.GetValidSequencesFromMorse(morse, new KeyValuePair<string, string>("", ""));
            Assert.AreEqual(6, result.Count());
        }

        [TestMethod]
        public async Task TestSearchSequence()
        {
            await this.decoder.SearchMorseSequenceAsync(0, new KeyValuePair<string, string>("", ""));
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
                //AvailableWords = dictionary,
                MorseSequence = "......-...-..---.-----.-..-..-..",
                FirstLetters = new HashSet<char>
                {
                    'H', 'O', 'W', 'T'
                },
                WordsByFirstLetter = new Dictionary<char, ISet<string>>
                {
                    { 'H', new HashSet<string> { "HELL", "HELLO" } },
                    { 'O', new HashSet<string> { "OWORLD" } },
                    { 'W', new HashSet<string> { "WORLD" } },
                    { 'T', new HashSet<string> { "TEST" } },
                }
            };

            await this.decoder.SearchMorseSequenceAsync(0, new KeyValuePair<string, string>("", ""));
            Assert.AreEqual(2, this.decoder.DecodedMessageCount);
        }

        [TestMethod]
        public async Task TestElapsedTime()
        {

            long[] runTimes =
            {
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
                await this.RunAndMeasureElapsedMs(),
            };

            double sum = runTimes.Sum(runtime => runtime);
            double average = sum / runTimes.Length + 0.0;

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
                FirstLetters = GetDefaultFirstLetters(dictionary),
                WordsByFirstLetter = GetDefaultWordsByFirstLetter(dictionary)
            };

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await this.decoder.SearchMorseSequenceAsync(0, new KeyValuePair<string, string>("", ""));
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
