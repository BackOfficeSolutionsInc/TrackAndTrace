using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Web;
using TractionTools.Tests.TestUtils;
using RadialReview.Controllers;
using TractionTools.Tests.Utilities;
using RadialReview.Models.L10;
using RadialReview.Accessors;
using RadialReview.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TractionTools.Tests.Controllers {
	[TestClass]
	public class TwilioApi : BaseTest {

		private string PhoneContent(string message) {
			if (message == null)
				return ("<Response></Response>");
			return ("<Response><Sms>" + message + "</Sms></Response>");
		}

		[TestMethod]
		[TestCategory("Twilio")]
		public async Task TestForumMethod() {

			var twilio = new TwilioApiController();

			var from = "14022028077";
			var to = "00000000000";

			var forumName = "Forum";

			MockHttpContext();

			var forum = await L10Utility.CreateRecurrence(forumName);

			DbCommit(s => {
				var l10 = s.Get<L10Recurrence>(forum.Id);
				l10.ForumCode = "ABC".ToLower();
				s.Update(l10);
			});

			//Send Code.
			{
				var res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, "  ", to);
				Assert.AreEqual(PhoneContent("Please send your meeting code."), res.Content);

				res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, "ASDF", to);
				Assert.AreEqual(PhoneContent("Please send your meeting code."), res.Content);

				res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, "ABC", to);
				Assert.AreEqual(PhoneContent("Welcome to the " + forumName + "! Please send your name."), res.Content);
			}
			//Add Name
			{
				var res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, " ", to);
				Assert.AreEqual(PhoneContent("Sorry I didn't get that. Please text your name."), res.Content);

				res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, "FirstN Middle Last", to);
				Assert.AreEqual(PhoneContent("Hi Firstn, what issues do you want to add? Try and keep them to 3 words.\nOne issue per text."), res.Content);
			}
			//Add Issues
			{
				var issues = L10Accessor.GetIssuesForRecurrence(UserOrganizationModel.CreateAdmin(), forum.Id, false);
				Assert.AreEqual(0, issues.Count);

				var res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, " ", to);
				Assert.AreEqual(PhoneContent(null), res.Content);

				res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, "A new issue", to);
				Assert.AreEqual(PhoneContent(null), res.Content);

				issues = L10Accessor.GetIssuesForRecurrence(UserOrganizationModel.CreateAdmin(), forum.Id, false);
				Assert.AreEqual(1, issues.Count);
				var i = issues.First();
				Assert.AreEqual("A new issue", i.Issue.Message);
				Assert.AreEqual(forum.Id, i.Recurrence.Id);


				res = await twilio.ReceiveForum_C4C187FFD1544290A05CB860EED6F2B0(from, "A another new issue", to);
				Assert.AreEqual(PhoneContent(null), res.Content);

				issues = L10Accessor.GetIssuesForRecurrence(UserOrganizationModel.CreateAdmin(), forum.Id, false);
				Assert.AreEqual(2, issues.Count);
				i = issues.FirstOrDefault(x => x.Issue.Message == "A another new issue");
				Assert.IsNotNull(i);
				Assert.AreEqual(forum.Id, i.Recurrence.Id);
			}


		}
	}
}
