using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public enum HookPriority {
		Lowest = 0,
		Low =40,
		Database=45,
		Unset = 50,
		High=60,
		Webhook = 80,
		UI=90,
		Highest=100,

	}

    public interface IHook {
		bool CanRunRemotely();
		HookPriority GetHookPriority();
	}
}
