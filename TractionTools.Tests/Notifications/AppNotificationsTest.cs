using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors.NotificationAccessor;

namespace TractionTools.Tests.Notifications {
	[TestClass]
	public class AppNotificationsTest {
		[TestMethod]
		public void SendIOS() {

			var b = NotifcationCreation.Build(0, "Test Heading", "Test body");

			NotifcationCreation.SendToDevice(new RadialReview.Models.Notifications.UserDevice() {
				DeviceType = "ios",
				DeviceId = "572BB9FE-EE0E-4AB3-BE5E-97BB4DDE1721"
			}, b);

		}
	}
}
