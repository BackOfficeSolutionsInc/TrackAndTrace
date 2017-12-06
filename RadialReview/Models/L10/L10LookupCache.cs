using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.L10 {
	public partial class L10Recurrence {
		public class L10LookupCache {

			public long RecurrenceId { get; private set; }

			public L10LookupCache(long recurrenceId) {
				RecurrenceId = recurrenceId;
			}

			private List<L10Recurrence_Measurable> _AllMeasurablesAndDividers { get; set; }

			public void SetAllMeasurablesAndDividers(List<L10Recurrence_Measurable> measurables) {
				_AllMeasurablesAndDividers = measurables;
			}

			public List<L10Recurrence_Measurable> GetAllMeasurablesAndDividers(Func<List<L10Recurrence_Measurable>> deflt) {
				if (_AllMeasurablesAndDividers == null)
					return deflt();
				return _AllMeasurablesAndDividers;
			}

		}
	}
}