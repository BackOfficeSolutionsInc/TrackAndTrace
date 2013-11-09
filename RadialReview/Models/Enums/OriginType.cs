using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{

    public enum OriginType
    {
        [Display(Name = "invalid", ResourceType = typeof(DisplayNameStrings))]
        Invalid,
        [Display(Name = "user", ResourceType = typeof(DisplayNameStrings))]
        User,
        [Display(Name = "group", ResourceType = typeof(DisplayNameStrings))]
        Group,
        [Display(Name = "organization", ResourceType = typeof(DisplayNameStrings))]
        Organization,
        [Display(Name = "industry", ResourceType = typeof(DisplayNameStrings))]
        Industry,
        [Display(Name = "default", ResourceType = typeof(DisplayNameStrings))]
        Application,

    }
}