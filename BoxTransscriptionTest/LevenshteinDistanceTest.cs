using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace BoxTranscriptionTest
{
    [TestClass]
    public class LevenshteinDistanceTest
    {
        private dynamic data = TestHelper.loadJObject("levenshteinDistance");

        [TestMethod]
        public void TestComputePerfectMatch()
        {
            foreach (dynamic pair in data.perfectMatch.Children()) {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.Compute(pair.text.Value, pair.source.Value);
                Assert.AreEqual(0, distance);
            }
        }

        [TestMethod]
        public void TestComputeCloseMatch()
        {
            foreach (dynamic pair in data.closeMatch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.Compute(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance <= 3);
            }
        }

        [TestMethod]
        public void TestComputeNotMatch()
        {
            foreach (dynamic pair in data.notMatch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.Compute(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance > 6);
            }
        }

        [TestMethod]
        public void TestComputePercentPerfectMatch()
        {
            foreach (dynamic pair in data.perfectMatch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.ComputePercent(pair.text.Value, pair.source.Value);
                Assert.AreEqual(1.0m, distance);
            }
        }

        [TestMethod]
        public void TestComputePercentCloseMatch()
        {
            foreach (dynamic pair in data.closeMatch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.ComputePercent(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance > .6m);
            }
        }

        [TestMethod]
        public void TestComputePercentNotMatch()
        {
            foreach (dynamic pair in data.notMatch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.ComputePercent(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance < .4m);

            }
        }

        [TestMethod]
        public void TestSearchPercentPerfectMatch()
        {
            foreach (dynamic pair in data.perfectSearch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.SearchPercent(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance == 1.0m);
            }
        }

        [TestMethod]
        public void TestSearchPercentCloseMatch()
        {
            foreach (dynamic pair in data.closeSearch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.SearchPercent(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance >= 0.8m);
            }
        }


        [TestMethod]
        public void TestSearchPercentNotMatch()
        {
            foreach (dynamic pair in data.notFoundSearch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.SearchPercent(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance < 0.5m);
            }
        }

        [TestMethod]
        public void TestSearchPercentPercentWithMultiplePossibleRanges()
        {
            foreach (dynamic pair in data.multiRangeSearch.Children())
            {
                var distance = BoxTranscriptionLamda.LevenshteinDistance.SearchPercent(pair.text.Value, pair.source.Value);
                Assert.IsTrue(distance > 0.8m);
            }
        }

    }


}
