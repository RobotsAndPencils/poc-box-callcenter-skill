using System;
using System.Collections.Generic;
using Amazon.Comprehend.Model;

namespace BoxTranscriptionLamda
{
    public class SkillResult
    {
        public decimal duration = 0;
        public Dictionary<int, List<SpeakerResult>> resultBySpeaker;
        public List<SpeakerResult> resultByTime;
        public Dictionary<int, Dictionary<string, List<SpeakerResult>>> resultsBySpeakerSentiment;
        public List<String> speakerLabels;
        public Dictionary<string, Boolean> scriptChecks;
        public Dictionary<string, List<SpeakerResult>> topicLocations;
        public List<string> topics;
        public decimal supportScore = 0;
        public int supportIndex = 0;

        public SkillResult()
        {
            resultBySpeaker = new Dictionary<int, List<SpeakerResult>>();
            resultByTime = new List<SpeakerResult>();
            resultsBySpeakerSentiment = new Dictionary<int, Dictionary<string, List<SpeakerResult>>>();
            speakerLabels = new List<string>();
            scriptChecks = new Dictionary<string, bool>();
            topicLocations = new Dictionary<string, List<SpeakerResult>>();
        }
    }
}
