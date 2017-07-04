using NHibernate;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IMeasurableHook  : IHook{
		Task CreateMeasurable(ISession s, MeasurableModel m);
	}
}
