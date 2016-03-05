using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using System.Collections.Generic;
using RadialReview.Utilities.DataTypes;

namespace TractionTools.Tests.Utilities {
    [TestClass]
    public class DistanceUtility_User {

        public static List<Tuple<string, string, long>> Available = new List<Tuple<string, string, long>>{
                Tuple.Create("James","Bond",100L),                Tuple.Create("Cameron","Diaz",101L),
                Tuple.Create("William","Defoe",102L),             Tuple.Create("Andy","Warhol",103L),
                Tuple.Create("Bill","Clinton",104L),              Tuple.Create("William","Gates",105L),
                Tuple.Create("George","Orwell",106L),             Tuple.Create("Samuel","Jackson",107L),
                Tuple.Create("Billy","Idol",108L),                Tuple.Create("Jimmy","Johns",109L),
                Tuple.Create("Jordan","Smith",110L),              Tuple.Create("Timothy","Dalton",111L),
                Tuple.Create("James","Dean",112L),                Tuple.Create("Don","Jackson",113L),
                Tuple.Create("Carson","Daily",114L),
         };

        private void AssertHistogram(NameFormat expected, string[] namesIn)
        {
            var cache = new Dictionary<string, List<String>>();
            var found = DistanceUtility._DetermineNameFormat(namesIn, Available, cache);

            Assert.AreEqual(expected, found);
        }

        private void AssertDistribution(DiscreteDistribution<Tuple<string,string,long>> dist, long id, int matches = 1, long deflt = -1)
        {
            Tuple<string, string, long> res = Tuple.Create("","",deflt);
            Assert.AreEqual(matches, dist.TryResolveOne(ref res));
            Assert.AreEqual(id, res.Item3);

        }



        [TestMethod]
        public void FirstNames()
        {
            var namesIn = new[] { "James", "Bill", "Andy", "George", "Cam", "Jammy", "Jorddany", "Timothanydr4" };

            //try out histogram first pass
            AssertHistogram(NameFormat.FN, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);

            Assert.AreEqual(8, match.Keys.Count);

            AssertDistribution(match["James"], 100L, 2);
            AssertDistribution(match["Bill"], 104L);
            AssertDistribution(match["Andy"], 103L);
            AssertDistribution(match["George"], 106L);
            AssertDistribution(match["Cam"], 101L, 2);
            AssertDistribution(match["Jammy"], 109L);//Spelled wrong
            AssertDistribution(match["Jorddany"], 110L);//Spelled pretty wrong
            AssertDistribution(match["Timothanydr4"], -100L, 0, -100L);//Spelled very wrong, too wrong in fact

        }
        [TestMethod]
        public void FirstNamesLastName()
        {
            var namesIn = new[] { 
                "James Johns", "Bill Clintn", "Bill Defoe", "Andy Warhol", "George Orwell",
                "Cam Diaz","James Bond", "Jorddany Smith", "Timothanydr4 Daltonad", "Jim Dean","James" };

            //try out histogram first pass
            AssertHistogram(NameFormat.FNLN, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);

            Assert.AreEqual(11, match.Keys.Count);

            AssertDistribution(match["James Johns"], 109L);
            AssertDistribution(match["Bill Defoe"], 102L);
            AssertDistribution(match["Bill Clintn"], 104L);
            AssertDistribution(match["Andy Warhol"], 103L);
            AssertDistribution(match["George Orwell"], 106L);
            AssertDistribution(match["Cam Diaz"], 101L);
            AssertDistribution(match["James Bond"], 100L);
            AssertDistribution(match["Jorddany Smith"], 110L);//Spelled pretty wrong
            AssertDistribution(match["Timothanydr4 Daltonad"], -100L, 0, -100L);//Spelled very wrong, too wrong in fact
            AssertDistribution(match["Jim Dean"], 112L);

            AssertDistribution(match["James"], -100L, 0, -100L);
        }
        [TestMethod]
        public void FirstNamesLastInitial()
        {
            var namesIn = new[] { "James J.", "Bill", "Andy", "George", "Cam", "James B.", "Jorddany", "Timothanydr4", "James" };

            //try out histogram first pass
            AssertHistogram(NameFormat.FNLI, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);

            Assert.AreEqual(9, match.Keys.Count);

            AssertDistribution(match["James J."], 109L);
            AssertDistribution(match["Bill"], 104L);
            AssertDistribution(match["Andy"], 103L);
            AssertDistribution(match["George"], 106L);
            AssertDistribution(match["Cam"], 101L, 2);
            AssertDistribution(match["James B."], 100L);//Spelled wrong
            AssertDistribution(match["Jorddany"], 110L);//Spelled pretty wrong
            AssertDistribution(match["Timothanydr4"], -100L, 0, -100L);//Spelled very wrong, too wrong in fact

            AssertDistribution(match["James"], 100L, 2);
        }

        [TestMethod]
        public void LastNameFirstNames()
        {
            var namesIn = new[] { 
                "Johns James", "Clintn Bill", "Defoe Bill", "Warhol Andy", "Orwell George",
                "Diaz Cam","Bond James", "Smith Jorddany", "Daltonad Timothanydr4", "Dean Jim","James" };

            //try out histogram first pass
            AssertHistogram(NameFormat.LNFN, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);

            Assert.AreEqual(11, match.Keys.Count);

            AssertDistribution(match["Johns James"], 109L);
            AssertDistribution(match["Defoe Bill"], 102L);
            AssertDistribution(match["Clintn Bill"], 104L);
            AssertDistribution(match["Warhol Andy"], 103L);
            AssertDistribution(match["Orwell George"], 106L);
            AssertDistribution(match["Diaz Cam"], 101L);
            AssertDistribution(match["Bond James"], 100L);
            AssertDistribution(match["Smith Jorddany"], 110L);//Spelled pretty wrong
            AssertDistribution(match["Daltonad Timothanydr4"], -100L, 0, -100L);//Spelled very wrong, too wrong in fact
            AssertDistribution(match["Dean Jim"], 112L);

            AssertDistribution(match["James"], -100L, 0, -100L);
        }
        [TestMethod]
        public void LastNames()
        {
            var namesIn = new[] { "Bond", "Defoe", "Warhol", "Orwell", "Jackson", "Bond", "Johns", "Clinton", "William" };

            //try out histogram first pass
            AssertHistogram(NameFormat.LN, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);
            Assert.AreEqual(8, match.Keys.Count);

            AssertDistribution(match["Bond"], 100L);
            AssertDistribution(match["Defoe"], 102L);
            AssertDistribution(match["Warhol"], 103L);
            AssertDistribution(match["Orwell"], 106L);
            AssertDistribution(match["Jackson"], 107L, 2);
            AssertDistribution(match["Johns"], 109L);
            AssertDistribution(match["Clinton"], 104L);
            AssertDistribution(match["William"], -100L, 0, -100L);//is a first name
        }
        [TestMethod]
        public void FirstInitialLastInitial()
        {
            var namesIn = new[] { "C. D.", "J. B.", "W. D.", "A. W.", "G. O.", "D. J.", "J J", "B C", "S. J." };

            //try out histogram first pass
            AssertHistogram(NameFormat.FILI, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);
            Assert.AreEqual(9, match.Keys.Count);

            AssertDistribution(match["J. B."], 100L);
            AssertDistribution(match["W. D."], 102L);
            AssertDistribution(match["A. W."], 103L);
            AssertDistribution(match["G. O."], 106L);
            AssertDistribution(match["D. J."], 113L);
            AssertDistribution(match["J J"], 109L);
            AssertDistribution(match["B C"], 104L);
            AssertDistribution(match["S. J."], 107L);
            AssertDistribution(match["C. D."], 101L, 2);
        }

        [TestMethod]
        public void FirstInitialLastName()
        {
            var namesIn = new[] { "J. Bond", "Bond", "Defoe", "Warhol", "Orwell", "D. Jackson", "Johns", "Clinton", "S. Jackson", "William" };

            //try out histogram first pass
            AssertHistogram(NameFormat.FILN, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);
            Assert.AreEqual(10, match.Keys.Count);

            AssertDistribution(match["J. Bond"], 100L);
            AssertDistribution(match["Bond"], 100L);
            AssertDistribution(match["Defoe"], 102L);
            AssertDistribution(match["Warhol"], 103L);
            AssertDistribution(match["Orwell"], 106L);
            AssertDistribution(match["D. Jackson"], 113L);
            AssertDistribution(match["Johns"], 109L);
            AssertDistribution(match["Clinton"], 104L);
            AssertDistribution(match["S. Jackson"], 107L);
            AssertDistribution(match["William"], -100L, 0, -100L);//is a first name
        }
        [TestMethod]
        public void LastName_FirstNameLastName()
        {
            var namesIn = new[] { 
                "Sam Jackson","Johns", "Clintn", "Bill Defoe", "Warhol", "Orwell",
                "Diaz","Bond","Smithh", "Don Jackson", "Jackson", "Dean","Billy" 
            };

            //try out histogram first pass
            AssertHistogram(NameFormat.FNLN_LN, namesIn);

            //All together
            var match = DistanceUtility.TryMatch(namesIn, Available);

            Assert.AreEqual(13, match.Keys.Count);

            AssertDistribution(match["Sam Jackson"], 107L);
            AssertDistribution(match["Johns"], 109L);
            AssertDistribution(match["Clintn"], 104L);
            AssertDistribution(match["Bill Defoe"], 102L);
            AssertDistribution(match["Warhol"], 103L);
            AssertDistribution(match["Orwell"], 106L);
            AssertDistribution(match["Diaz"], 101L);
            AssertDistribution(match["Bond"], 100L);
            AssertDistribution(match["Smithh"], 110L);
            AssertDistribution(match["Don Jackson"], 113L);
            AssertDistribution(match["Jackson"], 107L,2);
            AssertDistribution(match["Dean"], 112L);

            AssertDistribution(match["Billy"], -100L, 0, -100L);
        }



    }
}
