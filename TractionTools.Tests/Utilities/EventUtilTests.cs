using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.TestUtils {
	[TestClass]
	public class EventUtilTests {

		[TestMethod]
		public void TestMaxDurEvent() {

			var now = new DateTime(2016, 1, 28);

			
			var orgStart = new DateTime(2015, 6, 1);
			var result = EventUtil._MaxDurEvent(now, orgStart, EventType.NoLogins_1w, new DateTime(2016, 1, 26).AsList());
			Assert.AreEqual(null, result);

			result = EventUtil._MaxDurEvent(now, orgStart, EventType.NoLogins_1w, new DateTime(2016, 1, 25).AsList());
			Assert.AreEqual(EventType.NoLogins_3d, result);

			result = EventUtil._MaxDurEvent(now, orgStart, EventType.NoLogins_1w, new DateTime(2016, 1, 24).AsList());
			Assert.AreEqual(EventType.NoLogins_3d, result);

			result = EventUtil._MaxDurEvent(now, orgStart, EventType.NoLogins_1w, new DateTime(2016, 1, 23).AsList());
			Assert.AreEqual(EventType.NoLogins_5d, result);

			result = EventUtil._MaxDurEvent(now, orgStart, EventType.NoLogins_1w, new DateTime(2016, 1, 22).AsList());
			Assert.AreEqual(EventType.NoLogins_5d, result);

			result = EventUtil._MaxDurEvent(now, orgStart, EventType.NoLogins_1w, new DateTime(2016, 1, 21).AsList());
			Assert.AreEqual(EventType.NoLogins_1w, result);

			////

			now = new DateTime(2016, 1, 1);

			result = EventUtil._MaxDurEvent(now, new DateTime(2015, 6, 1), EventType.NoLeadershipMeetingCreated_1w);
			Assert.AreEqual(EventType.NoLeadershipMeetingCreated_12w, result);

			result = EventUtil._MaxDurEvent(now, new DateTime(2015, 7, 1), EventType.NoLeadershipMeetingCreated_1w);
			Assert.AreEqual(EventType.NoLeadershipMeetingCreated_12w, result);
			
			result = EventUtil._MaxDurEvent(now, new DateTime(2015, 8, 1), EventType.NoLeadershipMeetingCreated_1w);
			Assert.AreEqual(EventType.NoLeadershipMeetingCreated_12w, result);

			result = EventUtil._MaxDurEvent(now, new DateTime(2015, 9, 1), EventType.NoLeadershipMeetingCreated_1w);
			Assert.AreEqual(EventType.NoLeadershipMeetingCreated_12w, result);

			result = EventUtil._MaxDurEvent(now, new DateTime(2015, 10, 1), EventType.NoLeadershipMeetingCreated_1w);
			Assert.AreEqual(EventType.NoLeadershipMeetingCreated_12w, result);

			result = EventUtil._MaxDurEvent(now, new DateTime(2015, 11, 1), EventType.NoLeadershipMeetingCreated_1w);
			Assert.AreEqual(EventType.NoLeadershipMeetingCreated_8w, result);


		}

	}
}
