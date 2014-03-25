using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
    public class Tree
    {
        public String name { get; set; }
        public String subtext { get; set; }
        public String @class { get; set; }
        public long id { get; set; }
        public bool managing { get; set; }
        public bool manager { get; set; }
        public List<Tree> children { get; set; }
    }
}