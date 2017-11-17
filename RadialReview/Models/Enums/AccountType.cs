using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum AccountType
	{
		Paying=-20,
		Coach = -12,
		UserGroup = -11,
		Implementer = -10,
		Invalid = -5,
        [Display(Name="Trial")]
		Demo=0,
		Other = 10,
		SwanServices = 11,
		Dormant = 20,
		Cancelled = 30,
	}
}