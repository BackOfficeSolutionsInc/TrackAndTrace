using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Charts
{
    public class ChartOptions : ILongIdentifiable,IDeletable
    {
        public virtual long Id { get; set; }

        public virtual String ChartName { get; set; }

        /// <summary>
        /// points are averaged that contain these classes
        /// 
        /// "class1 class4 class5, class2, class3 class1"
        /// This will create 3 groups, 
        ///     1) averages points that have class1 class4 & class5
        ///     2) averages points that have class2
        ///     3) averages points that have class3 & class1
        ///     
        /// </summary>
        public virtual String GroupBy { get; set; }

        public virtual long ForUserId { get; set; }

        public virtual long ByUserId { get; set; }

        /// <summary>
        /// dim1,dim-2,anotherDim
        /// </summary>
        public virtual String DimensionIds { get; set; }


        /// <summary>
        /// Only include points that match a Filter
        /// 
        /// "class1 class4 class5, class2, class3 class1"
        /// Shows 3 types of points,
        ///     1) points that have class1 class4 & class5 or
        ///     2) points that have class2 or
        ///     3) points that have class3 & class1
        /// </summary>
        public virtual String Filters { get; set; }

        public virtual ChartDataSource Source { get; set; }

        public virtual String Options { get; set; }

        public DateTime? DeleteTime { get; set; }

        public ChartOptions()
        {
            Filters = "";
            GroupBy = "";
            DimensionIds = "";
        }
    }
}