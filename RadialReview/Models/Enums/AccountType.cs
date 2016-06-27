﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum AccountType
	{
		Paying=-20,
		Implementer=-10,
        [Display(Name="Trial")]
		Demo=0,
		Other = 10,
		Dormant = 20,
		Cancelled = 30,
	}
}