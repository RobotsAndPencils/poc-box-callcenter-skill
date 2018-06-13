using System;
using System.Collections.Generic;
using System.Text;

namespace BoxTranscriptionPOC
{
    public class Segments
    {
        public decimal start_time { get; set; }
        public string speaker_label { get; set; }
        public decimal end_time { get; set; }
        public object[] items { get; set; }
    }
}
