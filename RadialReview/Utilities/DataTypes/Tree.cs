using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
    public class Tree : TreeModel<Tree> {

    }

    public class TreeModel<T> where T : TreeModel<T>
    {
        public String name { get; set; }
        public String subtext { get; set; }
        public String @class { get; set; }
        public long id { get; set; }
        public bool managing { get; set; }
        public bool manager { get; set; }
        public bool collapsed { get; set; }
        protected IEnumerable<T> __children { get; set; }
        public IEnumerable<T> _children { get { return collapsed? __children:null; } set { __children = value; collapsed = true; } }
        public IEnumerable<T> children { get { return !collapsed ? __children : null; } set { __children = value; collapsed = false; } }
        public IDictionary<string, object> data { get; set; }

        public List<T> Flatten()
        {
            return Flatten((T)this);
        }

        protected List<T> Flatten(T s)
        {
            var o = new List<T>();
            if (s != null) {
                o.Add(s);
                if (s.__children != null) {
                    foreach (var c in s.__children) {
                        o.AddRange(Flatten(c));
                    }
                }
            }
            return o;

        }
    }
}