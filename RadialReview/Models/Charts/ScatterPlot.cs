using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Charts
{
    public class ScatterPlot
    {
        public List<ScatterLegendItem> Legend { get; set; }
        public List<ScatterFilter> Filters { get; set; }
        public List<ScatterGroup> Groups { get; set; }
        public String Class { get; set; }
        public List<ScatterData> Points { get; set; }
        public Dictionary<String,ScatterDimension> Dimensions { get; set; }
        public String InitialXDimension { get; set; }
        public String InitialYDimension { get; set; }

        public dynamic OtherData { get; set; }

        public ScatterPlot Copy()
        {
            return new ScatterPlot()
            {
                Class = Class,
                Dimensions = Dimensions.Keys.Select(k => Dimensions[k].Copy()).ToDictionary(x => x.Id, x => x),
                Filters = Filters.Select(x => x.Copy()).ToList(),
                Groups = Groups.Select(x => x.Copy()).ToList(),
                InitialXDimension = InitialXDimension,
                InitialYDimension = InitialYDimension,
                Points = Points.Select(x => x.Copy()).ToList(),
                Legend=Legend.Select(x=>x.Copy()).ToList()
            };
        }

        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
    }

    public class ScatterLegendItem
    {
        public String Name {get;set;}
        public String Class {get;set;}

        public ScatterLegendItem()
        {
        }

        public ScatterLegendItem(String name,String clss)
        {
            Name = name;
            Class = clss;
        }

        public ScatterLegendItem Copy()
        {
            return new ScatterLegendItem()
            {
                Class=Class,
                Name=Name
            };
        }
    }

    public class ScatterFilter
    {
        public String Name { get; set; }
        public String Class { get; set; }
        //Class to apply when filter is on
        public String FilterClass { get; set; }
        public bool On { get; set; }

        public ScatterFilter(String name, String @class, String filterClass = "on", bool on = false)
        {
            Name = name;
            Class = @class;
            FilterClass = filterClass;
            On = on;
        }
        public ScatterFilter Copy()
        {
            return new ScatterFilter(Name, Class,FilterClass, On);
        }
    }
    public class ScatterGroup
    {
        public String Name { get; set; }
        public String Class { get; set; }
        public bool On { get; set; }

        public ScatterGroup(String name, String @class, bool on = false)
        {
            Name = name;
            Class = @class;
            On = on;
        }

        public ScatterGroup Copy()
        {
            return new ScatterGroup(Name,Class,On);
        }
    }

    public class ScatterData
    {
        public long SliceId { get; set; }
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<String,ScatterDatum> Dimensions { get; set; }
        public String Class { get; set; }

        public String Title { get; set; }
        public String Subtext { get; set; }


        public dynamic OtherData { get; set; }

        public ScatterData Copy()
        {
            return new ScatterData()
            {
                Class = Class,
                Date = new DateTime(Date.Ticks),
                Dimensions = Dimensions.Keys.Select(k => Dimensions[k].Copy()).ToDictionary(x => x.DimensionId, x => x),
                Id = Id,
                SliceId = SliceId,
                Title=this.Title,
                Subtext=this.Subtext,
                OtherData=this.OtherData
            };
        }
    }

    public class ScatterDimension
    {
        public String Id { get; set; }
        public String Name { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }

        public ScatterDimension Copy()
        {
            return new ScatterDimension()
            {
                 Id=Id,
                 Max=Max,
                 Min=Min,
                 Name=Name,
            };
        }
    }


    public class ScatterDatum
    {
        public String DimensionId { get; set; }
        public double Value { get; set; }
        public double Denominator { get; set; }
        public String Class{get;set;}

        public ScatterDatum()
        {
            Denominator = 0;
        }

        public ScatterDatum Copy()
        {
            return new ScatterDatum()
            {
                DimensionId = DimensionId,
                Class = Class,
                Denominator = Denominator,
                Value = Value,
            };
        }
    }

}