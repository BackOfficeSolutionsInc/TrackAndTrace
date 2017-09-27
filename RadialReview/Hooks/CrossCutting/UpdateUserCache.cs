using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Askables;
using System.Threading.Tasks;
using RadialReview.Models;

namespace RadialReview.Hooks {
	public class UpdateUserCache : IRockHook {
		public bool CanRunRemotely() {
			return false;
		}

		private async Task UpdateForUser(ISession s, long userId) {
			s.Flush();
			var user = s.Get<UserOrganizationModel>(userId);
			if (user != null) {
				user.UpdateCache(s);
			}
		}


		public async Task CreateRock(ISession s, RockModel rock) {
			await UpdateForUser(s, rock.ForUserId);
		}

		public async Task UpdateRock(ISession s, RockModel rock) {
			await UpdateForUser(s, rock.ForUserId);
		}

		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			await UpdateForUser(s, rock.ForUserId);
		}


	}
}