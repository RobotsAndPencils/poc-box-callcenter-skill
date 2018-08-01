using System;
using System.Collections.Generic;

namespace BoxTranscriptionLamda
{
    public class SkillResult
    {
        public decimal duration = 0;
        public Dictionary<int, List<SpeakerResult>> resultBySpeaker;
        public List<SpeakerResult> resultByTime;
        public List<String> speakerLabels;
        public Dictionary<string, Boolean> scriptChecks;
        public Dictionary<string, List<SpeakerResult>> wordLocations;
        public List<string> topics;
        public int supportIndex = 0;

        public SkillResult()
        {
            resultBySpeaker = new Dictionary<int, List<SpeakerResult>>();
            resultByTime = new List<SpeakerResult>();
            speakerLabels = new List<string>();
            scriptChecks = new Dictionary<string, bool>();
            wordLocations = new Dictionary<string, List<SpeakerResult>>();
            topics = new List<string>();
        }
    }
}
