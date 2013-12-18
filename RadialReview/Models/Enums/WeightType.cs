using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace RadialReview.Models.Responsibilities
{
    public enum WeightType
    {
        [Display(Name = "no", ResourceType = typeof(DisplayNameStrings))]
        No = 0,
        [Display(Name = "lowest", ResourceType = typeof(DisplayNameStrings))]
        Lowest = 1,
        [Display(Name = "low", ResourceType = typeof(DisplayNameStrings))]
        Low = 2,
        [Display(Name = "normal", ResourceType = typeof(DisplayNameStrings))]
        Normal = 3,
        [Display(Name = "high", ResourceType = typeof(DisplayNameStrings))]
        High = 4,
        [Display(Name = "highest", ResourceType = typeof(DisplayNameStrings))]
        Highest = 5
    }
}
