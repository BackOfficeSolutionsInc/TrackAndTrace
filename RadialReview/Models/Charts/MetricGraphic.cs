using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Charts {
    /// <summary>
    /// https://www.metricsgraphicsjs.org/examples.htm
    /// </summary>
    public class MetricGraphic {
        public string title { get; set; }
        public string description { get; set; }
        [Obsolete("use methods")]
        public List<object> data { get; set; }
        [Obsolete("use methods")]
        public List<string> legend { get; set; }

        public List<MarkerData> markers { get; set; }
        
        public bool? missing_is_hidden { get; set; }

        /// <summary>
        ///  'percentage'
        /// </summary>
        public string format { get; set; }

        /// <summary>
        /// display digits of precision
        /// </summary>
        public int? decimals { get; set; }
        /// <summary>
        /// is an area chart
        /// </summary>
        public bool? area { get; set; }
        public bool? animate_on_load { get; set; }
        public bool? aggregate_rollover { get; set; }
        public bool? show_rollover_text { get; set; }

        /// <summary>
        /// Show or hide x axis
        /// </summary>
        public bool? x_axis { get; set; }
        ///<summary>
        /// Show or hide y axis
        /// </summary>
        public bool? y_axis { get; set; }

        public string x_label { get; set; }
        public string y_label { get; set; }

        /// <summary>
        /// 'log'
        /// </summary>
        public string y_scale_type { get; set; }

        public string xax_units { get; set; }
        /// <summary>
        /// '$'
        /// </summary>
        public string yax_units { get; set; }        

        //public string x_accessor { get; set; }
        //public string y_accessor { get; set; }
        
        public MetricGraphic(string title, string description=null) {
            this.title = title;
            this.description = description;
            data = new List<object>();
            legend = new List<string>();
            //area = false;
            //aggregate_rollover = true;

        }

        public void AddVertical(DateTime date,string label) {
            markers = markers ?? new List<MarkerData>();
            markers.Add(new MarkerData() {
                date = date,
                label = label??""
            });
        }

        public void AddTimeseries(MetricGraphicTimeseries ts) {
            data.Add(ts.Data.ToList());
            legend.Add(ts.Legend);
        }
        public List<MetricGraphicTimeseries> GetTimeseries() {
            var re = new List<MetricGraphicTimeseries>();
            for(var i=0;i<data.Count;i++) {
                var d = (List<DateData>)data[i];
                var l = legend[i];
                re.Add(new MetricGraphicTimeseries(d, l));
            }
            return re;            
        }


        public class DateData {
            public DateTime date { get; set; }
            public decimal? value { get; set; }
        }
        public class MarkerData {
            public DateTime date { get; set; }
            public string label { get; set; }
        }
    }

    public class MetricGraphicTimeseries {
        public string Legend { get; set; }
        public List<MetricGraphic.DateData> Data { get; set; }

        public MetricGraphicTimeseries(List<MetricGraphic.DateData> data,string legendTitle = null) {
            Data = data;
            Legend = legendTitle??"";
        }
    }
}