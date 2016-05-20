using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum RockState
	{
        [Display(Name = "Not Set")]
        Indeterminate = -1,
        [Display(Name = "Off Track")]
        AtRisk = 0,
        [Display(Name = "On Track")]
        OnTrack = 1,
        [Display(Name = "Done")]
		Complete = 2,
	}
}