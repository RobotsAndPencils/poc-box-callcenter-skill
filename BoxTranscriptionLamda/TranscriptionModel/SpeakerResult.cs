using Amazon.Comprehend.Model;

// Used for results after processing response from AWS transcribe and comprehend
namespace BoxTranscriptionLamda {
    public class SpeakerResult
    {
        public decimal start;
        public decimal end;
        public string text;
        public DetectSentimentResponse sentiment;
    }
}