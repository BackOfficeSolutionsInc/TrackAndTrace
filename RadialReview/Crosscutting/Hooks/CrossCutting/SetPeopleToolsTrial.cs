using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Events;
using System.Threading.Tasks;
using RadialReview.Models;
using RadialReview.SessionExtension;

namespace RadialReview.Crosscutting.Hooks.CrossCutting {
	public class SetPeopleToolsTrial : IAccountEvent {
		public bool CanRunRemotely() {
			return false;
		}

		public async Task CreateEvent(ISession s, AccountEvent evt) {
			if (evt.Type == Utilities.EventType.EnablePeople) {
				var org = s.Get<OrganizationModel>(evt.OrgId);
				var plan = org.PaymentPlan.Deproxy() as PaymentPlan_Monthly;
				
				if (plan != null) {
					plan.ReviewFreeUntil = DateTime.UtcNow.AddDays(90);
					s.Update(plan);
				}

			}
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}
	}
}