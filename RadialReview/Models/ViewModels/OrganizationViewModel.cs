using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Askables;

namespace RadialReview.Models.ViewModels
{
	public class OrganizationViewModel
	{
		public long Id { get; set; }
		public String OrganizationName { get; set; }
		public Boolean ManagersCanEdit { get; set; }
		public Boolean StrictHierarchy { get; set; }
		public Boolean ManagersCanEditPositions { get; set; }
		public Boolean SendEmailImmediately { get; set; }
		public bool ManagersCanRemoveUsers { get; set; }
		public String ImageUrl { get; set; }
		public Boolean ManagersCanEditSelf { get; set; }
		public Boolean EmployeesCanEditSelf { get; set; }
		public DayOfWeek WeekStart { get; set; }
		public string TimeZone { get; set; }
		public List<SelectListItem> TimeZones
		{
			get{
				return TimeZoneInfo.GetSystemTimeZones().Select(x => new SelectListItem(){
					Text = x.DisplayName,
					Value = x.Id
				}).ToList();
			}
		}

		public List<CompanyValueModel> CompanyValues { get; set; }
		public List<RockModel> CompanyRocks { get; set; }
		public List<AboutCompanyAskable> CompanyQuestions { get; set; }
		public String RockName { get; set; }
	}
}