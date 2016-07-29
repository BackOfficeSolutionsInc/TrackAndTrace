using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public class Chart<T> : BaseAngular {

        public Chart(long id) :base(id){
        }

        public Chart(){
        }
        
        public string height { get; set; }
        public string width { get; set; }
        public T data { get; set; }
    }
}