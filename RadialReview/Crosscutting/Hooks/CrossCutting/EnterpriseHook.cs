using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using System.Threading.Tasks;
using RadialReview.Nhibernate;
using static RadialReview.Accessors.PaymentAccessor;
using RadialReview.Utilities.DataTypes;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace RadialReview.Hooks {
	public class EnterpriseHook : ICreateUserOrganizationHook, IDeleteUserOrganizationHook {
		private int EnterpriseGreaterThanUsers;

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		public bool CanRunRemotely() {
			return true;
		}

		public EnterpriseHook(int greaterThanN) {
			EnterpriseGreaterThanUsers = greaterThanN;
		}

		public async Task CreateUserOrganization(ISession s, UserOrganizationModel user) {
			var calcOrg = UserCount(s, user);
			//Is null when autocalculate is off
			
			if (calcOrg != null) {
				var calc = calcOrg.Item1;
				var org = calcOrg.Item2;
				if (calc.NumberL10Users >= EnterpriseGreaterThanUsers + 1) {
					calc.Plan.BaselinePrice = 499m;
					calc.Plan.FirstN_Users_Free = 45;
					calc.Plan.L10PricePerPerson = 2m;
					org.PaymentPlan = calc.Plan;
					s.Update(org);
				}
			}
		}

		public async Task UndeleteUser(ISession s, UserOrganizationModel user) {
			await CreateUserOrganization(s, user);
		}

		public async Task DeleteUser(ISession s, UserOrganizationModel user) {
			var calcOrg = UserCount(s, user);
			//Is null when autocalculate is off
			
			if (calcOrg != null) {
				var calc = calcOrg.Item1;
				var org = calcOrg.Item2;
				if (calc.NumberL10Users <= EnterpriseGreaterThanUsers) {
					calc.Plan.BaselinePrice = 149m;
					calc.Plan.FirstN_Users_Free = 10;
					calc.Plan.L10PricePerPerson = 10m;
					org.PaymentPlan = calc.Plan;
					s.Update(org);
				}
			}
		}

		public async Task OnUserRegister(ISession s, UserModel user) {
			//Do nothing.
		}

		private Tuple<UserCalculator, OrganizationModel> UserCount(ISession s, UserOrganizationModel user) {
			if (user.Organization.Settings.AutoUpgradePayment) {
				var orgId = user.Organization.Id;
				var org = s.Get<OrganizationModel>(orgId);
				var calc = new UserCalculator(s, orgId, org.PaymentPlan, new DateRange(DateTime.MaxValue, DateTime.MaxValue));
				return Tuple.Create(calc, org);
			}
			return null;
		}

		public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel user) {
			await CreateUserOrganization(s, user);
		}

	}
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously