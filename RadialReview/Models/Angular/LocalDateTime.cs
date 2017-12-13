using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public class LocalDateTime {

		public bool Local { get { return true; } }
		public DateTime Date { get; set; }
		public LocalDateTime(DateTime date) {
			Date = date;
		}

		public static implicit operator LocalDateTime(DateTime d) {
			return new LocalDateTime(d);
		}
		public static implicit operator DateTime(LocalDateTime d) {
			return d.Date;
		}

		public override string ToString() {
			return Date.ToString();
		}

		public string ToString(string format) {
			return Date.ToString(format);
		}

		internal object AddDays(int v) {
			throw new NotImplementedException();
		}
	}
}