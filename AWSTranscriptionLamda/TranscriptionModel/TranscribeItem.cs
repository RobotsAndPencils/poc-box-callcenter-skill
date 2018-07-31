using System;
using System.Collections.Generic;
using System.Text;

namespace BoxTranscriptionLamda
{
    public class TranscribeItem
    {
        public decimal start_time { get; set; }
        public string type { get; set; }
        public decimal end_time { get; set; }
        public List<TranscribeAlternative> alternatives { get; set; }
    }

    public class TranscribeAlternative
    {
        public double confidence { get; set; }
        public string content { get; set; }
    }
}
