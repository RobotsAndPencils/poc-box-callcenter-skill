using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace BoxTranscriptionPOC
{
    public class TranscriptionParser
    {
        public const string SPEAKER_0 = "spk_0";
        public const string SPEAKER_1 = "spk_2";
        public static void ParseFile(string filepath)
        {
            var json = File.ReadAllText(filepath);
            JObject transcriptionResults = JObject.Parse(json);
            ProcessTranscriptionResults(transcriptionResults);
        }
        private static void ProcessTranscriptionResults(JObject transcriptionResults)
        {
            StringBuilder speaker1Text = new StringBuilder();
            StringBuilder speaker2Text = new StringBuilder();
            decimal startSegment = 0;
            decimal endSegment = 0;
            TranscribeAlternatives alternative = null;

            var segments = transcriptionResults["results"]["speaker_labels"]["segments"].ToObject<List<Segments>>();
            var transciptionsItems = transcriptionResults["results"]["items"].ToObject<List<TranscribeItems>>();
            //var speakers = results.Value<JObject>("speaker_labels");
            //var segments = results.Value<List<Segments>>("segments");
            //var items = results.Value<List<TranscribeItems>>("items");
            Console.WriteLine($"items: {transciptionsItems?.Count} segments: {segments.Count}");

            var speakerLabel = string.Empty;
            foreach (var segment in segments)
            {
                startSegment = segment.start_time;
                endSegment = segment.end_time;
                speakerLabel = segment.speaker_label;
                foreach (var item in transciptionsItems)
                {
                    if (startSegment < item.start_time && item.end_time < endSegment)
                    {
                         alternative = item.alternatives.First();
                        if (speakerLabel == SPEAKER_0) {
                            speaker1Text.Append(alternative.content);
                            speaker1Text.Append(" ");
                        }
                        else
                        {
                            speaker2Text.Append(alternative.content);
                            speaker2Text.Append(" ");
                        }
                    }

                }

            }
            Console.WriteLine($"Speaker 1: {speaker1Text.ToString()}");
            Console.WriteLine($"Speaker 2: {speaker2Text.ToString()}");
        }

    }
}
