﻿using Newtonsoft.Json;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular {
    public class AngularTreeNode<T> : BaseAngular where T : AngularTreeNode<T>{

        public AngularTreeNode(){
        }

        public AngularTreeNode(long id ) : base(id){
        }
        //[JsonIgnore]
        //public long id
        //{
        //    // get is intentionally omitted here
        //    get { return Id; }
        //}
        //[JsonIgnore]
        //public new long Id { get { return base.Id; } set { base.Id=value; }}
        public bool collapsed { get; set; }

        protected IEnumerable<T> __children { get; set; }
        public IEnumerable<T> children { get { return !collapsed ? __children : null; } set { collapsed = false; __children = value; } }
        public IEnumerable<T> _children { get { return collapsed?__children:null; } set { collapsed = true; __children = value; } }


    }
}