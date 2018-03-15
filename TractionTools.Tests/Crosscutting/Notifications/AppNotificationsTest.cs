using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using System.Threading.Tasks;

namespace TractionTools.Tests.Notifications {
	[TestClass]
	public class AppNotificationsTest {
		[TestMethod]
		public async Task SendIOS() {

			var b = NotifcationCreation.Build(0, "Test Heading", "Test body", sensitive:true);

            await NotifcationCreation.SendToDevice(new RadialReview.Models.Notifications.UserDevice() {
                DeviceType = "ios",
                //DeviceId = "APA91bHsxXyWw5bS6n-SBTnQAzkTzcAW321623FQSA1L1XdZA0lext7jh1bSP105TVS-Jvj18p8cPr4r-g6-Vw_Qx4F7OnQ3ZRb_hk37-6eoZPHCHuuD2Zppmqzmd7dGMBYtxb7mNloJ"//"572BB9FE-EE0E-4AB3-BE5E-97BB4DDE1721"
                DeviceId = "d7jsZL4YBDE:APA91bGTD6zxB9Q42YomfFlrk5rY74_18NEF2k4EY-F3vtZHfxcxHcqDzZDGRgtRYAf4fMKtBexa-s-62yD6xHC-OLEoiRteBTus6zVaMBKBf15-q2yWyivnCWVz_VJ9RDPOML8k0er0",

            }, b);
            Console.WriteLine("here");
		}
	}
}
