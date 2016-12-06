using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Scorecard {
    public class AngularMeasurableGroup : BaseAngular {
        public string Name { get; set; }
        public int? Ordering { get; set; }

        public AngularMeasurableGroup(long id) : this(id,null,null) { }
        public AngularMeasurableGroup(long id, string name) : this(id, null, name) { }
        public AngularMeasurableGroup(long id,int? ordering, string name) : base(id) {
            Name = name;
            Ordering = ordering;
        }

    }
}