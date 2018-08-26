using System;
namespace BoxTranscriptionLamda
{
    public static class LevenshteinDistance
    {

        // search for string s within a larger block of text t
        // first try to find a range in t where s applies
        // then compute percent distance between s and substring of t
        // limitation: won't work if first and last word in search string doesn't exist in source text
        public static decimal SearchPercent(string s, string t) {
            //find first and last words in s
            string[] words = s.Split(" ");
            //TODO: what if one word? no words, bla bla bla

            string first = words[0];
            string last = words[words.Length - 1];
            decimal finalResult = 0;

            int lastStartIdx = 0;
            int startIdx = 0;
            //do a search for each range we can define (instance of first work with instance of last word after
            while ((startIdx = t.IndexOf(first, lastStartIdx, StringComparison.Ordinal)) !=-1) {
                lastStartIdx = startIdx + first.Length;
                int lastEndIdx = startIdx;
                int endIdx = 0;
                while ((endIdx = t.IndexOf(last, lastEndIdx, StringComparison.Ordinal)) != -1)
                {
                    lastEndIdx = endIdx + last.Length;
                    var result = ComputePercent(s, t.Substring(startIdx, lastEndIdx - startIdx));
                    if (result > finalResult) finalResult = result;
                    //TODO: might be nice to be returning the compute result as well as the range we found it in
                    //but don't need that for this POC
                }
            }

            return finalResult;
        }


        public static decimal ComputePercent(string s, string t) {
            decimal distance = Compute(s, t);
            decimal length = (s.Length > t.Length ? s.Length : t.Length);
            return (length - distance) / length;
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    }
}
