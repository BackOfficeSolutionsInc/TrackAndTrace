using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Askables;

namespace RadialReview.Models.Angular.CompanyValue
{
	public class AngularCompanyValue : Base.BaseAngular
	{ 
		public AngularCompanyValue(long id) : base(id){
		}
		public AngularCompanyValue(){
		}

		public string CompanyValue { get; set; }
		public string CompanyValueDetails { get; set; }
		public bool Deleted { get; set; }

		public static List<AngularCompanyValue> Create(IEnumerable<CompanyValueModel> list)
		{
			new AngularCompanyValue();
			return list.Select(Create).ToList();
		}

		public static AngularCompanyValue Create(CompanyValueModel x){
			return new AngularCompanyValue(){
				CompanyValue = x.CompanyValue,
				CompanyValueDetails = x.CompanyValueDetails,
				Id = x.Id,
				Deleted = (x.DeleteTime!=null)
			};
		}
	}
}