using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.DataType {
    public class AngularString : BaseAngular {

#pragma warning disable CS0618 // Type or member is obsolete
        public AngularString() {
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public AngularString(long id) : base(id) { }

        public AngularString(long id, string str) : base(id) {
            Data = str;
        }

        public string Data { get; set; }
    }
}