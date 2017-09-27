using NHibernate;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IRockHook :IHook{
		Task CreateRock(ISession s, RockModel rock);
		Task UpdateRock(ISession s, RockModel rock);
		Task ArchiveRock(ISession s, RockModel rock,bool deleted);
	}
}
