using System;
using System.Collections.Generic;
using System.Text;

namespace AWSTranscriptionLamda
{
    public class TranscribeItems
    {
        public string start_time { get; set; }
        public string type { get; set; }
        public string end_time { get; set; }
        public TranscribeAlternatives alternatives { get; set; }
    }

    public class TranscribeAlternatives
    {
        public double confidence { get; set; }
        public string content { get; set; }
    }
}
