using System;
using System.Collections.Generic;
using System.Text;

namespace AWSTranscriptionLamda
{
    public class Segments
    {
        public string start_time { get; set; }
        public string speaker_label { get; set; }
        public string end_time { get; set; }
        public object[] items { get; set; }
    }
}
