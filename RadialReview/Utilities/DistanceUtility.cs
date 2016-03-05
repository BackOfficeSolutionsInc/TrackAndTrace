using RadialReview.Models;
using RadialReview.Properties;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RadialReview.Utilities {
    public enum NameFormat {
        None = 0,
        FN,
        FNLN,
        FNLI,
        LNFN,
        LN,
        FILI,
        FILN,
        LIFI,
        FNLN_FN,
        FNLN_LN,
    }

    public class DistanceUtility {

        private static List<String> nicknameLines = null;

        public static List<string> GetNicknames(string name, ref Dictionary<string, List<string>> cache)
        {
            name = name.ToLower();
            var names = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (names.Length > 1)
                name = names[0];

            if (cache == null)
                cache = new Dictionary<string, List<string>>();

            if (cache.ContainsKey(name))
                return cache[name];



            if (nicknameLines == null) {
                //string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //string location = Path.Combine(executableLocation, "Data\\nickname.csv");
                //nicknameLines = File.ReadAllLines(location).ToList();
                nicknameLines = Resources.nickname.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var thresh = 1;
            var output = new List<String>();
            output.Add(name);
            foreach (var l in nicknameLines) {
                var split = l.Split(',');
                foreach (var n in split) {
                    if (DamerauLevenshteinDistance(name, n, thresh) <= thresh) {
                        output.AddRange(split);
                    }
                }
            }
            var o = output.Distinct().ToList();
            cache[name] = o;
            return o;

        }

        public static Dictionary<string, DiscreteDistribution<UserOrganizationModel>> TryMatch(IEnumerable<string> names, IEnumerable<UserOrganizationModel> available, int thresh = 2)
        {
            var available_first_last_id = available.Select(x => Tuple.Create(x.GetFirstName().ToLower(), x.GetLastName().ToLower(), x.Id)).ToList();
            var matches = TryMatch(names, available_first_last_id, thresh);

            var output = new Dictionary<string, DiscreteDistribution<UserOrganizationModel>>();
            foreach (var m in matches.Keys) {
                output.Add(m, matches[m].Convert(x => available.FirstOrDefault(y => y.Id == x.Item3)));
            }
            return output;
        }
        public static Dictionary<string, DiscreteDistribution<Tuple<string, string, long>>> TryMatch(IEnumerable<string> names, IEnumerable<Tuple<string, string, long>> available_first_last_id, int thresh = 2)
        {
            var names2 = names.ToList();
            var available_first_last_id2 = available_first_last_id.ToList();
            available_first_last_id2 = available_first_last_id2.Distinct().ToList();
            if (available_first_last_id2 == null || !available_first_last_id2.Any()) {
                throw new ArgumentOutOfRangeException("available", "No users available");
            }
            var nicknameCache = new Dictionary<string, List<string>>();

            var format = _DetermineNameFormat(names, available_first_last_id, nicknameCache, thresh);

            var defaultMatch = available_first_last_id2.FirstOrDefault().NotNull(x => x.Item3);
            var output = new Dictionary<string, DiscreteDistribution<long>>();
            foreach (var name in names2) {
                //long match = defaultMatch;
                output[name] = _TryMatch_SecondPass(name.ToLower(), available_first_last_id2, format, nicknameCache, thresh);
                // match;
            }
            var output2 = new Dictionary<string, DiscreteDistribution<Tuple<string,string,long>>>();
            foreach (var m in output.Keys) {
                output2.Add(m, output[m].Convert(x => available_first_last_id2.FirstOrDefault(y => y.Item3 == x)));
            }

            return output2;
        }

        public static NameFormat _DetermineNameFormat(IEnumerable<string> names, IEnumerable<Tuple<string, string, long>> available_first_last_id, Dictionary<string, List<string>> nicknameCache, int thresh = 2)
        {
            var histogram = new DiscreteDistribution<NameFormat>(thresh, 0);
            var names2 = names.Select(x => x.ToLower()).ToList();
            var available_first_last_id2 = available_first_last_id.Distinct().ToList();
            foreach (var name in names2)
                _TryMatch_FirstPass(name, available_first_last_id2, histogram, nicknameCache, thresh);

            //var histogramCounts = histogram.GetProbabilities().OrderByDescending(x => x.Value).ToList();
            var backings = histogram.GetBacking();

            if (backings.Any(x => x.Key == NameFormat.FN)) {
                //var count = histogramCounts.First(x => x.Key == NameFormat.FN).Value + histogramCounts.Where(x => x.Key == NameFormat.FNLI || x.Key == NameFormat.FNLN).Sum(x => x.Value);
                //histogramCounts.RemoveAll(x => x.Key == NameFormat.FN);
                //histogramCounts.Add(new KeyValuePair<NameFormat, int>(NameFormat.FN, count));
                foreach (var h in backings.Where(x => x.Key == NameFormat.FNLI || x.Key == NameFormat.FNLN)) {
                    histogram.Add(NameFormat.FN, h.Value);
                }
            }
            if (backings.Any(x => x.Key == NameFormat.LN)) {
                //var count = histogramCounts.First(x => x.Key == NameFormat.LN).Value + histogramCounts.Where(x => x.Key == NameFormat.FILN || x.Key == NameFormat.FNLN).Sum(x => x.Value);
                //histogramCounts.RemoveAll(x => x.Key == NameFormat.LN);
                //histogramCounts.Add(new KeyValuePair<NameFormat, int>(NameFormat.LN, count));
                foreach (var h in backings.Where(x => x.Key == NameFormat.FILN || x.Key == NameFormat.FNLN)) {
                    histogram.Add(NameFormat.LN, h.Value);
                }
            }

            var histogramCounts = histogram.GetProbabilities().OrderByDescending(x => x.Value).ToList();

            var format = histogramCounts.FirstOrDefault().NotNull(x => x.Key);

            //if (format == null)
            //    format = NameFormat.None;

            if (format == NameFormat.FN && histogramCounts.Any(x => x.Key == NameFormat.FNLI)) format = NameFormat.FNLI;
            if (format == NameFormat.FN && histogramCounts.Any(x => x.Key == NameFormat.FNLN)) format = NameFormat.FNLN_FN;
            if (format == NameFormat.LN && histogramCounts.Any(x => x.Key == NameFormat.FILN)) format = NameFormat.FILN;
            if (format == NameFormat.LN && histogramCounts.Any(x => x.Key == NameFormat.FNLN)) format = NameFormat.FNLN_LN;

            return format;

        }
        public static DiscreteDistribution<long> _TryMatch_SecondPass(string name, List<Tuple<string, string, long>> available_first_last_id, NameFormat format, Dictionary<string, List<string>> nicknameCache, int thresh)
        {
            available_first_last_id = available_first_last_id.Select(x => Tuple.Create(x.Item1.ToLower(), x.Item2.ToLower(), x.Item3)).ToList();
            name = name.ToLower();
            var names = name.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            var dist = new DiscreteDistribution<long>(thresh, -1);
            //var nicknameCache = new Dictionary<string, List<string>>();
            if (names.Count() == 0) {
                return dist;
            }
            var name1 = names[0];
            var name2 = "";
            if (names.Length > 1)
                name2 = names[names.Length - 1];

            var possible_id_score = new List<Tuple<long, int, string, string>>();
            switch (format) {
                case NameFormat.FILI: {
                        foreach (var a in available_first_last_id) {
                            var best = int.MaxValue;
                            foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                var score = 0;
                                if (a.Item1 == nickname)
                                    score -= 1;
                                if (nickname.Length > 0 && a.Item2.Length > 0 && name1.Length > 0 && name2.Length > 0 && nickname[0] == name1[0] && a.Item2[0] == name2[0])
                                    best = Math.Min(best, score);

                            }
                            if (best != int.MaxValue)
                                possible_id_score.Add(Tuple.Create(a.Item3, best, a.Item1, ""));
                        }
                        break;
                    }
                case NameFormat.FILN: {
                        if (name2 == "") {
                            goto case NameFormat.LN;
                        } else {
                            foreach (var a in available_first_last_id) {
                                var best = int.MaxValue;
                                foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                    if (nickname.Length > 0 && name1.Length > 0 && nickname[0] == name1[0]) {
                                        var score = DamerauLevenshteinDistance(name2, a.Item2, thresh);
                                        if (a.Item1 == nickname)
                                            score -= 1;
                                        if (score <= thresh) {
                                            best = Math.Min(best, score);
                                            //possible_id_score.Add(Tuple.Create(a.Item3, score, a.Item1, nickname));
                                        }
                                    }
                                } 
                                if (best != int.MaxValue)
                                    possible_id_score.Add(Tuple.Create(a.Item3, best ,a.Item1, ""));
                            }
                        }
                        break;
                    }
                case NameFormat.FN: {

                        var fn = name1;
                        foreach (var a in available_first_last_id) {
                            var best = int.MaxValue;
                            foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                var score = DamerauLevenshteinDistance(fn, nickname, thresh);
                                if (a.Item1 == nickname)
                                    score -= 1;
                                if (score <= thresh) {
                                    best = Math.Min(best, score);
                                }
                            } 
                            if (best != int.MaxValue)
                                possible_id_score.Add(Tuple.Create(a.Item3, best, fn, ""));
                        }
                        string b = "";
                        foreach (var i in possible_id_score) {
                            b += i.Item1 + "," + i.Item2 + "," + i.Item3 + "," + i.Item4 + "\n";
                        }
                        break;
                    }
                case NameFormat.FNLI: {
                        if (name2 == "") {
                            goto case NameFormat.FN;
                        } else {
                            var fn = name1;
                            foreach (var a in available_first_last_id) {
                                if (a.Item2.Length > 0 && name2.Length > 0 && a.Item2[0] == name2[0]) {
                                    var best = int.MaxValue;
                                    foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                        var score = DamerauLevenshteinDistance(fn, nickname, thresh);
                                        if (score <= thresh) {
                                            if (a.Item1 == nickname)
                                                score -= 1;
                                            best = Math.Min(best, score);
                                        }
                                    }
                                    if (best != int.MaxValue)
                                        possible_id_score.Add(Tuple.Create(a.Item3, best, fn, ""));
                                }
                            }
                        }
                        break;
                    }
                case NameFormat.FNLN: {
                        var fn = name1;
                        var ln = name2;
                        foreach (var a in available_first_last_id) {
                            var best = int.MaxValue;
                            foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                var fnScore = DamerauLevenshteinDistance(fn, nickname, thresh);
                                var lnScore = DamerauLevenshteinDistance(ln, a.Item2, thresh);
                                if (a.Item1 == nickname)
                                    fnScore -= 1;
                                if ((int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0) <= thresh) {
                                    best = Math.Min(best, (int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0));
                                }
                            }
                            if (best != int.MaxValue)
                                possible_id_score.Add(Tuple.Create(a.Item3, best, fn, ""));
                        }
                        break;
                    }
                case NameFormat.FNLN_FN: {
                        if (name2 == "") {
                            goto case NameFormat.FN;
                        } else {
                            goto case NameFormat.FNLN;
                        }
                    }
                case NameFormat.FNLN_LN: {
                        if (name2 == "") {
                            goto case NameFormat.LN;
                        } else {
                            goto case NameFormat.FNLN;
                        }

                    }
                case NameFormat.LIFI: {
                        foreach (var a in available_first_last_id) {
                            var best = int.MaxValue;
                            foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                var score = 0;
                                if (a.Item1 == nickname)
                                    score -= 1;
                                if (nickname.Length > 0 && a.Item2.Length > 0 && name1.Length > 0 && name2.Length > 0 && a.Item2[0] == name1[0] && nickname[0] == name2[0])
                                    best = Math.Min(best, score);
                            }
                            if (best != int.MaxValue)
                                possible_id_score.Add(Tuple.Create(a.Item3, best, a.Item1, ""));
                        }
                        break;
                    }
                case NameFormat.LN: {
                        var ln = name1;
                        foreach (var a in available_first_last_id) {
                            var score = DamerauLevenshteinDistance(ln, a.Item2, thresh);
                            if (score <= thresh) {
                                possible_id_score.Add(Tuple.Create(a.Item3, score, a.Item1, (string)null));
                            }
                        }
                        break;
                    }
                case NameFormat.LNFN: {
                        var ln = name1;
                        var fn = name2;
                        foreach (var a in available_first_last_id) {
                            var best = int.MaxValue;
                            foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                var fnScore = DamerauLevenshteinDistance(fn, nickname, thresh);
                                var lnScore = DamerauLevenshteinDistance(ln, a.Item2, thresh);
                                if (a.Item1 == nickname)
                                    fnScore -= 1;
                                if ((int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0) <= thresh) {
                                    best = Math.Min(best, (int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0));
                                }
                            }
                            if (best!=int.MaxValue)
                                possible_id_score.Add(Tuple.Create(a.Item3, best, fn, ""));
                        }
                        break;
                    }
                case NameFormat.None: {
                        break;//handle below in finalAvailable
                    }
                default: throw new ArgumentOutOfRangeException("format");
            }
            var ordered = possible_id_score.OrderBy(x => x.Item2).ToList();
            foreach (var o in ordered) {
                dist.Add(o.Item1, o.Item2);
            }
            return dist;
            /*List<Tuple<string, string, long>> finalAvailable;
            var first  =ordered.FirstOrDefault();
            if (first != null){
                var possible = ordered.Where(x => x.Item2 == first.Item2).ToList();

                if (possible.Count == 1) {
                    matchId = possible.First().Item1;
                    return true;
                } else {
                    finalAvailable = available_first_last_id.Where(x => possible.Any(y => y.Item2 == x.Item3)).ToList();
                }
            }else{
                finalAvailable = available_first_last_id;
            }
            var finalScores = new List<Tuple<long, int>>();
            foreach (var a in finalAvailable) {
                var full = a.Item1 + " " + a.Item2;
                finalScores.Add(Tuple.Create(a.Item3, DamerauLevenshteinDistance(name, full, thresh+1)));
            }
            if (!finalScores.Any())
                return false;
            matchId = finalScores.OrderBy(x => x.Item2).FirstOrDefault().NotNull(x=>x.Item1);            
            return true;*/
        }

        public static void _TryMatch_FirstPass(string name, List<Tuple<string, string, long>> available_first_last_id, DiscreteDistribution<NameFormat> formats, Dictionary<string, List<string>> nicknameCache, int thresh = 2)
        {
            name = name.ToLower();
            available_first_last_id = available_first_last_id.Select(x => Tuple.Create(x.Item1.ToLower(), x.Item2.ToLower(), x.Item3)).ToList();
            var names = name.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (names.Count() == 0) {
                return;
            }

            if (names.Count() == 1) {
                var n = names[0].ToLower();
                foreach (var a in available_first_last_id) {
                    //Try first name
                    var added = false;
                    var best = int.MaxValue;
                    foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                        var fnScore = DamerauLevenshteinDistance(n, nickname, thresh);

                        if (fnScore <= thresh)
                            best = Math.Min(best, fnScore);

                        //if (fnScore <= thresh && !added) {
                        //    formats.Add();
                        //    added = true;
                        //}
                        //if (fnScore == 0) {
                        //    formats.Add(NameFormat.FN);
                        //    break;
                        //}
                    }
                    if (best != int.MaxValue)
                        formats.Add(NameFormat.FN, best);
                    var lnScore = DamerauLevenshteinDistance(n, a.Item2.ToLower(), thresh);
                    if (lnScore <= thresh)
                        formats.Add(NameFormat.LN, lnScore);
                    //if (lnScore <= thresh) {
                    //    formats.Add(NameFormat.LN);
                    //}
                    //if (lnScore == 0)
                    //    formats.Add(NameFormat.LN);
                }

            }

            if (names.Count() >= 2) {
                names = new[] { names[0], names[names.Length - 1] };
                var fn = names[0].ToLower();
                var ln = names[1].ToLower();
                foreach (var a in available_first_last_id) {
                    //Try first name
                    if (names[0].TrimEnd('.').Length == 1 && names[1].TrimEnd('.').Length == 1) { // J. S.
                        //FILI
                        if ((a.Item1.Length > 0 && a.Item1[0] == names[0][0]) && (a.Item2.Length > 0 && a.Item2[0] == names[1][0])) {
                            //formats.Add(NameFormat.FILI);
                            //formats.Add(NameFormat.FILI);
                            formats.Add(NameFormat.FILI, 0);
                        }
                        if ((a.Item1.Length > 0 && a.Item1[0] == names[1][0]) && (a.Item2.Length > 0 && a.Item2[0] == names[0][0])) {
                            //formats.Add(NameFormat.LIFI);
                            //formats.Add(NameFormat.LIFI);
                            formats.Add(NameFormat.LIFI, 0);
                        }
                    } else if (names[0].TrimEnd('.').Length == 1 && names[1].TrimEnd('.').Length > 1) {// J. Smith

                        if (a.Item1.Length > 0 && names[0][0] == a.Item1[0]) {
                            var lnThresh = DamerauLevenshteinDistance(ln, a.Item2.ToLower(), thresh);
                            if (lnThresh <= thresh)
                                formats.Add(NameFormat.FILN, lnThresh);
                            //if (lnThresh <= thresh) {
                            //    formats.Add(NameFormat.FILN);
                            //    if (lnThresh == 0)
                            //        formats.Add(NameFormat.FILN);
                            //}
                        }
                    } else if (names[0].TrimEnd('.').Length > 1 && names[1].TrimEnd('.').Length == 1) {// John S.
                        if (a.Item2.Length > 0 && names[1][0] == a.Item2[0]) {
                            bool added = false;
                            var best = int.MaxValue;
                            foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                                var fnThresh = DamerauLevenshteinDistance(names[0], nickname.ToLower(), thresh);
                                if (fnThresh <= thresh)
                                    best = Math.Min(best, fnThresh);
                                //if (fnThresh <= thresh && added == false) {
                                //    formats.Add(NameFormat.FNLI);
                                //    added = true;
                                //}
                                //if (fnThresh == 0) {
                                //    formats.Add(NameFormat.FNLI);
                                //    break;
                                //}
                            }
                            if (best != int.MaxValue)
                                formats.Add(NameFormat.FNLI, best);

                        }
                    } else if (names[0].TrimEnd('.').Length > 1 && names[1].TrimEnd('.').Length > 1) {
                        // John Smith
                        var added = false;
                        var best = int.MaxValue;
                        foreach (var nickname in GetNicknames(a.Item1, ref nicknameCache)) {
                            var fnScore = DamerauLevenshteinDistance(fn, nickname.ToLower(), thresh);
                            var lnScore = DamerauLevenshteinDistance(ln, a.Item2.ToLower(), thresh);
                            if ((int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0) <= thresh)
                                best = Math.Min(best, (int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0));
                            //if (fnThresh <= thresh && lnThresh <= thresh && !added) {
                            //    formats.Add(NameFormat.FNLN);
                            //    added = true;
                            //}
                            //if (fnThresh == 0 && lnThresh == 0) {
                            //    formats.Add(NameFormat.FNLN);
                            //    break;
                            //}
                        }
                        if (best != int.MaxValue)
                            formats.Add(NameFormat.FNLN, best);

                        // Smith John
                        added = false;
                        best = int.MaxValue;
                        foreach (var nickname in GetNicknames(a.Item2, ref nicknameCache)) {
                            var fnScore = DamerauLevenshteinDistance(fn, nickname.ToLower(), thresh);
                            var lnScore = DamerauLevenshteinDistance(ln, a.Item1.ToLower(), thresh);

                            if ((int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0) <= thresh)
                                best = Math.Min(best, (int)Math.Ceiling(fnScore / 2.0 + lnScore / 2.0));
                            //if (fnThresh <= thresh && lnThresh <= thresh && !added) {
                            //    formats.Add(NameFormat.LNFN);
                            //    added = true;
                            //}
                            //if (fnThresh == 0 && lnThresh == 0) {
                            //    formats.Add(NameFormat.LNFN);
                            //    break;
                            //}
                        }
                        if (best != int.MaxValue)
                            formats.Add(NameFormat.LNFN, best);
                    } else {
                        throw new ArgumentOutOfRangeException("Unhandled Case");
                    }
                }
            }
        }



        private static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }

        /// <summary>
        /// Computes the Damerau-Levenshtein Distance between two strings, represented as arrays of
        /// integers, where each integer represents the code point of a character in the source string.
        /// Includes an optional threshhold which can be used to indicate the maximum allowable distance.
        /// </summary>
        /// <param name="source">An array of the code points of the first string</param>
        /// <param name="target">An array of the code points of the second string</param>
        /// <param name="threshold">Maximum allowable distance</param>
        /// <returns>Int.MaxValue if threshhold exceeded; otherwise the Damerau-Leveshteim distance between the strings</returns>
        /// http://stackoverflow.com/questions/9453731/how-to-calculate-distance-similarity-measure-of-given-2-strings
        public static int DamerauLevenshteinDistance(string source, string target, int threshold)
        {

            int length1 = source.Length;
            int length2 = target.Length;

            // Return trivial case - difference in string lengths exceeds threshhold
            if (Math.Abs(length1 - length2) > threshold) { return int.MaxValue; }

            // Ensure arrays [i] / length1 use shorter length 
            if (length1 > length2) {
                Swap(ref target, ref source);
                Swap(ref length1, ref length2);
            }

            int maxi = length1;
            int maxj = length2;

            int[] dCurrent = new int[maxi + 1];
            int[] dMinus1 = new int[maxi + 1];
            int[] dMinus2 = new int[maxi + 1];
            int[] dSwap;

            for (int i = 0; i <= maxi; i++) { dCurrent[i] = i; }

            int jm1 = 0, im1 = 0, im2 = -1;

            for (int j = 1; j <= maxj; j++) {

                // Rotate
                dSwap = dMinus2;
                dMinus2 = dMinus1;
                dMinus1 = dCurrent;
                dCurrent = dSwap;

                // Initialize
                int minDistance = int.MaxValue;
                dCurrent[0] = j;
                im1 = 0;
                im2 = -1;

                for (int i = 1; i <= maxi; i++) {

                    int cost = source[im1] == target[jm1] ? 0 : 1;

                    int del = dCurrent[im1] + 1;
                    int ins = dMinus1[i] + 1;
                    int sub = dMinus1[im1] + cost;

                    //Fastest execution for min value of 3 integers
                    int min = (del > ins) ? (ins > sub ? sub : ins) : (del > sub ? sub : del);

                    if (i > 1 && j > 1 && source[im2] == target[jm1] && source[im1] == target[j - 2])
                        min = Math.Min(min, dMinus2[im2] + cost);

                    dCurrent[i] = min;
                    if (min < minDistance) { minDistance = min; }
                    im1++;
                    im2++;
                }
                jm1++;
                if (minDistance > threshold) { return int.MaxValue; }
            }

            int result = dCurrent[maxi];
            return (result > threshold) ? int.MaxValue : result;
        }
    }
}