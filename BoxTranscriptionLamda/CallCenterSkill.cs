using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/**
 * Does all the processing of the transcription results. Maybe not well named, but
 * wanted to separate from the BoxHelper where the cards were being created
 */
namespace BoxTranscriptionLamda
{
    public static class CallCenterSkill
    {
        private static Dictionary<string, string> scriptPhrases = new Dictionary<string, string>();
        private static readonly string NEGATIVE = "Negative";
        private static readonly string POSITIVE = "Positive";
        private static Configuration config = Configuration.GetInstance.Result;

        static CallCenterSkill () {
            var phrases = config.ScriptAdherence;
            var scriptPhrasesTemp = phrases.ToObject<Dictionary<string, string>>();

            foreach (var key in scriptPhrasesTemp.Keys) {
                scriptPhrases[key] = CleanText(scriptPhrasesTemp[key]);
            }
        }

        public static void ProcessTranscriptionResults (ref SkillResult result) {
            GenerateScriptAdherence(ref result);
            AggregateSpeakerSentiment(ref result);
            AdjustSpeakerTitles(ref result);
            CalculateSupportScore(ref result);
            Console.WriteLine("======== SkillResult =========");
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.None));
        }

        private static void CalculateSupportScore(ref SkillResult result)
        {
            var score = 0m;
            foreach (var check in result.scriptChecks) {
                score += check.Value ? .5m : -.75m;
            }

            if (result.resultsBySpeakerSentiment[result.supportIndex].ContainsKey(NEGATIVE))
            {
                score += 1m - (0.4m * result.resultsBySpeakerSentiment[result.supportIndex][NEGATIVE].Count);
            }

            //TODO: bucketize across time and aggregate sentiment to see if moves from lower to higher (slope)
            foreach (var tuple in result.resultsBySpeakerSentiment) {
                if (tuple.Key == result.supportIndex) continue;
                if (tuple.Value.ContainsKey(NEGATIVE))
                {
                    score += 0m - (0.2m * tuple.Value[NEGATIVE].Count);
                }
                if (tuple.Value.ContainsKey(POSITIVE))
                {
                    score += (0.3m * tuple.Value[POSITIVE].Count);
                }
            }

            result.supportScore = score;
        }

        public static List<SpeakerResult> Bucketize(List<SpeakerResult> source, int totalBuckets)
        {
            var min = source[0].start;
            var max = source[source.Count - 1].start;
            var buckets = new List<SpeakerResult>();

            var bucketSize = (max - min) / totalBuckets;
            if (bucketSize == 0m) return null;
            foreach (var value in source)
            {
                int bucketIndex = 0;
                bucketIndex = (int)((value.start - min) / bucketSize);
                if (bucketIndex == totalBuckets)
                {
                    bucketIndex--;
                }

                buckets[bucketIndex] = value;
            }
            return buckets;
        }

        private static void AggregateSpeakerSentiment(ref SkillResult result)
        {           
            foreach (var speaker in result.resultBySpeaker.Keys) {
                var sentimentAg = new Dictionary<string, List<SpeakerResult>>();
                result.resultsBySpeakerSentiment.Add(speaker, new Dictionary<string, List<SpeakerResult>>());
                foreach (var speakerResult in result.resultBySpeaker[speaker]) {
                    var sentString = speakerResult?.sentiment?.Sentiment?.Value;
                    //if (!sentString.Equals("NEUTRAL"))
                    if (sentString != null)
                    {
                        if (!sentimentAg.ContainsKey(sentString))
                        {
                            sentimentAg.Add(sentString, new List<SpeakerResult>());
                        }
                        sentimentAg[sentString].Add(speakerResult);
                    }
                }
                foreach (var sentValue in sentimentAg.Keys) {
                    //capitalized first char
                    var formattedSentValue = sentValue.Remove(1) + sentValue.ToLower().Remove(0, 1);
                    result.resultsBySpeakerSentiment[speaker].Add(formattedSentValue, sentimentAg[sentValue]);    
                }

            }

        }

        private static void AdjustSpeakerTitles(ref SkillResult result)
        {
            result.speakerLabels[result.supportIndex] = "Support";
            var spIdx = 1;
            for (var i = 0; i < result.speakerLabels.Count; i++) {
                if (i != result.supportIndex) {
                    result.speakerLabels[i] = "Customer" + (result.speakerLabels.Count==2?"":$" {spIdx++}");
                }
            }
        }

        public static void GenerateScriptAdherence (ref SkillResult result) {
            //TODO: should use algorithm for symantic difference, but doing something quick for poc
            //      close text search

            //we don't know which speaker is the support agent. So for each speaker, we will 
            //try to match all of the phrases. If we get more than 2 matches, we'll assume that 
            //is the support agent. Otherwise assume the first speaker and that they didn't
            //follow the script
            Console.WriteLine("======== Script Adherence =========");
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.None));

            int lastPhraseCount = 0;
            foreach (var speaker in result.resultBySpeaker.Keys) {
                var scriptChecks = new Dictionary<string, bool>();
                var phraseCount = 0;
                foreach (var phraseKey in scriptPhrases.Keys) {
                    foreach (var results in result.resultBySpeaker[speaker]) {
                        var found = CleanText(results.text).Contains(scriptPhrases[phraseKey]);
                        if (found) {
                            scriptChecks.Add(phraseKey, true);
                            break;
                        }
                    }
                    if (scriptChecks.ContainsKey(phraseKey)) {
                        phraseCount++;
                    } else {
                        scriptChecks.Add(phraseKey, false);
                    }
                }
                if (phraseCount > lastPhraseCount) {
                    lastPhraseCount = phraseCount;
                    result.supportIndex = speaker;
                    result.scriptChecks = scriptChecks;

                    // With two matches, assume this has to be the support rep, so 
                    // don't bother processing remaining speakers
                    if (phraseCount > 2) break;
                }
            }
            if (result.scriptChecks.Keys.Count == 0)
            {
                // no one matched anything, assume speaker one is support
                result.supportIndex = 0;
                foreach (var phraseKey in scriptPhrases.Keys)
                {
                    result.scriptChecks.Add(phraseKey, false);
                }
            }

        }
           
        private static string CleanText (string text) {
            return Regex.Replace(Regex.Replace(text, @"[\.,!?]", ""), @"[ ]{2,}", " ").ToLower();
        }

    }
}
