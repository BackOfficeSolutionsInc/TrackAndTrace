using RadialReview.Models.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.Utilities.Chart
{
    public class ChartClassFilter
    {
        public string[] Requirements { get; set; }

        protected ChartClassFilter()
        {

        }

        public static List<ChartClassFilter> CreateFilters(String filterString)
        {
            return filterString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => ChartClassFilter.Create(x.Trim())).ToList();
        }

        public static ChartClassFilter Create(String requirements)
        {
            return new ChartClassFilter()
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
            return Requirements.All(x => classes.Any(y => x.Equals(y)));
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
            copy.Dimensions = point.Dimensions.Where(x => AllowableDimensionIds.Any(y => y.EqualsInvariant(x.Key))).ToDictionary(x=>x.Key,x=>x.Value);
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
        public static List<ScatterData> Filter(List<ScatterData> dataPoints, List<ChartClassFilter> classFilters,ChartDimensionFilter dimensionFilters)
        {
            var newScatter = new List<ScatterData>();

            foreach (var unfilteredDataPoint in dataPoints)
            {
                var newDataPoint = unfilteredDataPoint.Copy();

                newDataPoint.Dimensions = unfilteredDataPoint.Dimensions
                    .Where(dim => dimensionFilters == null || dimensionFilters.Unfiltered() || dimensionFilters.Match(dim.Value))
                    .Where(dim => classFilters==null || !classFilters.Any() || classFilters.Any(f => f.MatchesEither(unfilteredDataPoint, dim.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);

                if (newDataPoint.Dimensions.Any())
                    newScatter.Add(newDataPoint);
            }
            return newScatter;
        }
    }
}