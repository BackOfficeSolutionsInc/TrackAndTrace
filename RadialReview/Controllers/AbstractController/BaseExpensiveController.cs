using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers.AbstractController {
	public abstract class BaseExpensiveController : BaseController {
		// GET: BaseExpensive

		public class Divisor {
			public int divisor { get; set; }
			public int remainder { get; set; }
			public double? duration { get; set; }
			public int? updates { get; set; }

			public TimeSpan GetDuration() {
				return TimeSpan.FromMilliseconds(duration ?? 0);
			}

			public double GetDurationSeconds() {
				return GetDuration().TotalSeconds;
			}

			public Divisor() {
				divisor = 13;
				remainder = 0;
				duration = 0;
				updates = 0;
			}
		}

		protected SimpleExpression Mod<T>(Expression<Func<T, object>> property, int divisor, int remainder) {
			return Restrictions.Eq(Projections.SqlFunction("mod", NHibernateUtil.Int64, Projections.Property<T>(property), Projections.Constant(divisor)), remainder);
		}
		protected SimpleExpression Mod<T>(Expression<Func<T, object>> property, Divisor dd) {
			return Mod<T>(property, dd.divisor, dd.remainder);
		}


		protected async Task<ActionResult> BreakUpAction(string controllerAction, Divisor dd, Action<Divisor> action, Func<ActionResult> oncomplete = null) {

			var start = DateTime.UtcNow;
			dd = dd ?? new Divisor();

			if (dd.remainder >= dd.divisor) {
				if (oncomplete == null)
					return Content("Duration:" + dd.GetDurationSeconds() + "s<br/>Updates: " + dd.updates);
				else
					return oncomplete();
			}

			if (dd.divisor <= 0)
				dd.divisor = 1;

			//real work here...
			action(dd);

			dd.duration += (DateTime.UtcNow - start).TotalMilliseconds;
			dd.remainder += 1;

			return RedirectToAction(controllerAction, dd);
		}
	}
}