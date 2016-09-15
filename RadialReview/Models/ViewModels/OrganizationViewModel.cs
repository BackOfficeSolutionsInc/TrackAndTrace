using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.Scorecard;

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
		public Boolean ManagersCanCreateSurvey { get; set; }
		public Boolean EmployeesCanCreateSurvey { get; set; }
		public bool ManagersCanRemoveUsers { get; set; }
		public String ImageUrl { get; set; }
		public Boolean ManagersCanEditSelf { get; set; }
		public Boolean EmployeesCanEditSelf { get; set; }
		public DayOfWeek WeekStart { get; set; }
		public string TimeZone { get; set; }
		public bool OnlySeeRockAndScorecardBelowYou { get; set; }
		public ScorecardPeriod ScorecardPeriod { get; set; }
		public Month StartOfYearMonth { get; set; }
		public DateOffset StartOfYearOffset { get; set; }
		public string DateFormat { get; set; }
		public NumberFormat NumberFormat { get; set; }
		public long AccountabilityChartId { get; set; }

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
        public List<CreditCardVM> Cards { get; set; }
		public PaymentPlanModel PaymentPlan { get; set; }


		//public OrganizationViewModel() {
		//}

	}
}