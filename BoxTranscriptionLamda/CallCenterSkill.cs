using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

/**
 * Does all the processing of the transcription results. Maybe not well named, but
 * wanted to separate from the BoxHelper where the cards were being created
 */
namespace BoxTranscriptionLamda
{
    public class CallCenterSkill
    {
        private static readonly List<string> stopWords;
        private static readonly Dictionary<string, string> scriptPhrases;

        static CallCenterSkill () {
            var words = JArray.Parse(System.Environment.GetEnvironmentVariable("stopWords"));
            stopWords = words.ToObject<List<string>>();
            var phrases = JObject.Parse(System.Environment.GetEnvironmentVariable("scriptPhrases"));
            scriptPhrases = words.ToObject<Dictionary<string, string>>();
            var keys = scriptPhrases.Keys;
            foreach (var key in keys) {
                scriptPhrases[key] = CleanText(scriptPhrases[key]);
            }
        }

        public static void ProcessTranscriptionResults (ref SkillResult result) {
            GenerateTopics(ref result);
            GenerateScriptAdherance(ref result);
        }

        public static void GenerateScriptAdherance (ref SkillResult result) {
            
        }
           
        private static string CleanText (string text) {
            return Regex.Replace(Regex.Replace(text, @"[\.,!?]", ""), @"[ ]{2,}", " ").ToLower();
        }

        public static void GenerateTopics (ref SkillResult result) {
            //create dictionary of words to array of locations. Can then sort by array lengths
            Console.WriteLine("Building work locations dictionary");

            foreach (var speakerResult in result.resultByTime)
            {
                var text = CleanText(speakerResult.text);
                foreach (string word in text.Split(' '))
                {
                    if (word.Length > 1 && !stopWords.Contains(word))
                    {
                        if (!result.wordLocations.ContainsKey(word))
                        {
                            result.wordLocations.Add(word, new List<SpeakerResult>());
                        }
                        result.wordLocations[word].Add(speakerResult);
                    }
                }
            }

            Console.WriteLine("Sorting words by word location occurances");
            result.topics = new List<string>(result.wordLocations.Keys);
            var wordLocations = result.wordLocations;
            result.topics.Sort(delegate (string wordA, string wordB)
            {
                return wordLocations[wordB].Count - wordLocations[wordA].Count;
            });

        }
    }
}
