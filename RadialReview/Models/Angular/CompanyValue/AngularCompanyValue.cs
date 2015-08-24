using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Askables;

namespace RadialReview.Models.Angular.CompanyValue
{
	public class AngularCompanyValue : Base.BaseAngular
	{ 
			[Obsolete("User Static constructor", false)]
		public AngularCompanyValue(long id) : base(id){
		}
		public AngularCompanyValue(){
		}

		public string CompanyValue { get; set; }
		public string CompanyValueDetails { get; set; }

		public static List<AngularCompanyValue> Create(IEnumerable<CompanyValueModel> list)
		{
			return list.Select(x => new AngularCompanyValue(){
				CompanyValue = x.CompanyValue,
				CompanyValueDetails = x.CompanyValueDetails,
				Id = x.Id,
			}).ToList();
		}
	}
}