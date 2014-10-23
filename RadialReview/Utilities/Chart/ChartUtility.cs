using RadialReview.Models.Charts;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.Utilities.Chart
{


    public class ChartClassMatcher
    {
        public string[] Requirements { get; set; }

        protected ChartClassMatcher()
        {

        }

        public static List<ChartClassMatcher> CreateMatchers(String filterString)
        {
            if (filterString == null)
                return new List<ChartClassMatcher>();

            return filterString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => ChartClassMatcher.Create(x.Trim())).ToList();
        }

        public static ChartClassMatcher Create(String requirements)
        {
            return new ChartClassMatcher()
            {
                Requirements = Regex.Split(requirements, "\\s+")
            };
        }

        public bool Match(ScatterData data)
        {
            return Match(data.GetClasses().ToList());
        }

        public bool Match(ScatterDatum datum)
        {
            return Match(datum.GetClasses().ToList());
        }
        public bool MatchesEither(ScatterData data, ScatterDatum datum)
        {
            return Match(data.GetClasses().Union(datum.GetClasses()).ToList());
        }

        public bool Match(List<string> classes)
        {
            if (!Requirements.Any())
                return true;
            return Requirements.All(required =>
            {
                var pattern = "^"+required.Replace("*", "[a-zA-Z0-9_\\-]*")+"$";
                return classes.Any(y =>
                {
                    return Regex.Matches(y, pattern).Count > 0;
                });
            });
        }

        public List<String> InterestingClasses(ScatterData data, ScatterDatum datum)
        {
            return InterestingClasses(data.GetClasses().Union(datum.GetClasses()).ToList());
        }

        public List<String> InterestingClasses(ScatterData data)
        {
            var classes = data.GetClasses().ToList();
            foreach (var p in data.Dimensions)
            {
                classes = classes.Union(p.Value.GetClasses().ToList()).ToList();
            }
            return InterestingClasses(classes);
        }

        public List<String> InterestingClasses(List<string> classes)
        {
            return classes.Where(clss =>
            {
                return Requirements.Any(r =>
                {
                    var pattern = "^"+r.Replace("*", "[a-zA-Z0-9_\\-]*")+"$";
                    return Regex.Matches(clss, pattern).Count > 0;
                });
            }).ToList();
        }

    }


    public class ChartDimensionFilter
    {
        public List<String> AllowableDimensionIds { get; set; }

        protected ChartDimensionFilter()
        {

        }

        public static ChartDimensionFilter Create(String dimensionsString)
        {
            return new ChartDimensionFilter()
            {
                AllowableDimensionIds = dimensionsString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => !String.IsNullOrWhiteSpace(x)).ToList()
            };
        }

        public ScatterData Filter(ScatterData point)
        {
            var copy = point.Copy();
            copy.Dimensions = point.Dimensions.Where(x => AllowableDimensionIds.Any(y => y.EqualsInvariant(x.Key))).ToDictionary(x => x.Key, x => x.Value);
            return copy;
        }

        public bool Match(ScatterDatum point)
        {
            return AllowableDimensionIds.Any(possible => possible.EqualsInvariant(point.DimensionId));
        }

        public bool Unfiltered()
        {
            return !AllowableDimensionIds.Any();
        }
    }

    public class ChartUtility
    {
        public static List<ScatterData> Filter(List<ScatterData> dataPoints, List<ChartClassMatcher> classFilters, ChartDimensionFilter dimensionFilters)
        {
            var newScatter = new List<ScatterData>();

            foreach (var unfilteredDataPoint in dataPoints)
            {
                var newDataPoint = unfilteredDataPoint.Copy();

                newDataPoint.Dimensions = unfilteredDataPoint.Dimensions
                    .Where(dim => dimensionFilters == null || dimensionFilters.Unfiltered() || dimensionFilters.Match(dim.Value))
                    .Where(dim => classFilters == null || !classFilters.Any() || classFilters.Any(f => f.MatchesEither(unfilteredDataPoint, dim.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);

                if (newDataPoint.Dimensions.Any())
                    newScatter.Add(newDataPoint);
            }
            return newScatter;
        }

        private static String[] ClassIntersection(String[] group1, String[] group2)
        {
            return group1.Intersect(group2).ToArray();
        }

        /*private static List<String> NormalizedMatchingClasses(string[] classes, string[] requiredClasses)
        {
            var matches = new List<String>();
            var required = requiredClasses;
            for (var r = 0; r < required.Length; r++) {
                var found = false;
                var regexStr = required[r].Replace("*", "[a-zA-Z0-9_\\-]+");
                var re = new Regex(regexStr);
                for (var i = 0; i < classes.Length; i++) {
                    if (re.Matches(classes[i]).Count>0) 
                    {
                        found = true;
                        matches.Add(classes[i]);
                    }
                }
                if (found == false)
                    return null;
            }
            matches.Sort();
            return matches;
        }*/


        public static List<ScatterData> Group(List<ScatterData> dataPoints, List<ChartClassMatcher> groupByClasses)
        {
            var newScatter = new List<ScatterData>();

            Func<String[], String[], String[]> classIntersection = (o, n) =>
            {
                if (o == null)
                    return n;
                else
                    return n.Intersect(o).ToArray();
            };

            var id = 0;


            if (groupByClasses.Count() == 0)
                groupByClasses = new List<ChartClassMatcher>() { {  
                                                                     ChartClassMatcher.Create("")
                                                               } };

            foreach (var group in groupByClasses)
            {
                var merged = new Dictionary<String,ScatterData>();
                for (var i = 0; i < dataPoints.Count; i++) {
                    var point = dataPoints[i];
                    var sliceId = point.SliceId;         
   
                    var normClasses = group.InterestingClasses(point);

                    //var normClasses= group.InterestingClasses(point,point.

                    var groupId = String.Join(",",normClasses);
                    var keyItems = normClasses;

                    keyItems.Insert(0,""+sliceId);

                    var key=String.Join(",",keyItems);

                    if (!merged.ContainsKey(key)) 
                    {
                        merged[key] = new ScatterData(){
                            SliceId= point.SliceId,
                            Id= point.Id,
                            Date= point.Date,
                            Dimensions= new Dictionary<string,ScatterDatum>(),
                            Class=point.Class,
                            OtherData=new {GroupId=groupId}
                        };
                    }

                    merged[key].Class = String.Join(" ",classIntersection(merged[key].GetClasses(), point.GetClasses()));

                    foreach (var dd in point.Dimensions) {
                        var d   = dd.Key;
                        var dim = dd.Value;
                        if (!merged[key].Dimensions.ContainsKey(d)) {
                            merged[key].Dimensions[d] = new ScatterDatum(){
                                DimensionId= dim.DimensionId,
                                Value= 0,
                                Denominator= 0,
                                Class= dim.Class,
                            };
                        }

                        merged[key].Dimensions[d].Value += dim.Value;
                        merged[key].Dimensions[d].Denominator += dim.Denominator;
                        merged[key].Dimensions[d].Class = string.Join(" ",classIntersection(merged[key].Dimensions[d].GetClasses(), dim.GetClasses()));
                    }
                }

                var output = new List<ScatterData>();

                foreach (var m in merged) {
                    //merged[m].groupId = m.;
                    newScatter.Add(merged[m.Key]);
                }
                //newScatter output;

                /*
                //dimension~sliceId~interestingClasses=value
                DefaultDictionary<String, double> top = new DefaultDictionary<string, double>(y => 0, (k, oldV, newV) => oldV + newV);
                DefaultDictionary<String, double> bottom = new DefaultDictionary<string, double>(y => 0, (k, oldV, newV) => oldV + newV);

                DefaultDictionary<String, string[]> datumClassesIntersection = new DefaultDictionary<string, string[]>(k => null, classIntersection);
                string[] dataClassesIntersection = null;
                var maxDate = DateTime.MinValue;

                var available = new HashSet<String>();

                foreach (var dataPoint in dataPoints)
                {
                    foreach (var datum in dataPoint.Dimensions)
                    {
                        var datumValue = datum.Value;
                        var dimension = datum.Key;
                        var sliceId = dataPoint.SliceId;

                        if (group.MatchesEither(dataPoint, datumValue))
                        {
                            var lookup = sliceId + "~" + dimension + "~" + String.Join("~", group.InterestingClasses(dataPoint, datumValue));

                            top[lookup] += datumValue.Value;
                            bottom[lookup] += datumValue.Denominator;
                            maxDate = new DateTime(Math.Max(maxDate.Ticks, dataPoint.Date.Ticks));

                            //Datum Classes
                            var datumClasses = Regex.Split(datumValue.Class ?? "", "\\s+");
                            datumClassesIntersection.Merge(lookup, datumClasses);

                            //DataPoint Classes
                            var dataClasses = Regex.Split(dataPoint.Class ?? "", "\\s+");

                            dataClassesIntersection = classIntersection("", dataClassesIntersection, dataClasses);
                            //Merge(sliceId, dataClasses);
                            available.Add(lookup);
                        }
                    }
                }


                foreach (var item in available)
                {
                    var dimensions = new List<ScatterDatum>();

                    foreach (var dim in availableDimensions)
                    {
                        dimensions.Add(new ScatterDatum()
                        {
                            Class = String.Join(" ", datumClassesIntersection[dim]),
                            Denominator = bottom[dim][sliceId],
                            Value = top[dim][sliceId],
                            DimensionId = dim
                        });
                    }


                    newScatter.Add(new ScatterData()
                    {
                        Class = String.Join(" ", dataClassesIntersection),
                        Date = maxDate,
                        Dimensions = dimensions.ToDictionary(x => x.DimensionId, x => x),
                        Id = id,
                        SliceId = sliceId,
                    });
                }*/
            }

            return newScatter;

        }
    }
}