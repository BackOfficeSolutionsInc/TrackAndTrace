using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public class Chart {
        public string height { get; set; }
        public string width { get; set; }
        public dynamic data { get; set; }
    }
}