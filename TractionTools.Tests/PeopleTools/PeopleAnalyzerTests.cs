using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Areas.People.Accessors;
using RadialReview.Models;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Accessors;

namespace TractionTools.Tests.PeopleTools {
	[TestClass]
	public class PeopleAnalyzerTests :BaseTest {


		private static async Task<long> GenQCAsync(string name, UserOrganizationModel user) {
			var abouts = QuarterlyConversationAccessor.AvailableAboutsForMe(user);
			var byAbouts = QuarterlyConversationAccessor.AvailableByAboutsFiltered(user, abouts, true, true);
			var id= await QuarterlyConversationAccessor.GenerateQuarterlyConversation(user.Id, name, byAbouts, null, DateTime.UtcNow.AddDays(1), false);
			await AddResponses(id, user);
			return id;
		}

		private static async Task AddResponses(long id, UserOrganizationModel user) {
			var s =SurveyAccessor.GetAngularSurveyContainerBy(user, user, id);

			foreach (var survey in s.Surveys) {
				foreach (var section in survey.Sections) {
					foreach (var q in section.Items) {
						var format = q.ItemFormat;
						switch (format.QuestionIdentifier) {
							case SurveyQuestionIdentifier.Value:
								SurveyAccessor.UpdateAngularSurveyResponse(user, q.Response.Id, "often");
								break;
							case SurveyQuestionIdentifier.GWC:
								SurveyAccessor.UpdateAngularSurveyResponse(user, q.Response.Id, "yes");
								break;
							default:
								break;
						}
						if (format.QuestionIdentifier ==SurveyQuestionIdentifier.Value) {
						}
					}
				}
			}


		}

		[TestMethod]
		public async Task VisibilitySettings() {

			var org = await OrgUtil.CreateFullOrganization();
			

			var l10 = await L10Utility.CreateRecurrence(name:"l10",org:org);
			VtoAccessor.AddCompanyValue(org.Manager, l10.Recur.VtoId);


			l10.AddAdmin(org.Manager);
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Middle);
			await l10.AddAttendee(org.E1);

			await GenQCAsync("middle", org.Middle);

			var visible = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.Middle, org.Middle.Id, l10.Id);
			var aboutIds = visible.Rows.Select(v => v.About.ModelId);

			Assert.AreEqual(4, aboutIds.Count());
			Assert.IsTrue(aboutIds.Contains(org.Middle.Id));
			Assert.IsTrue(aboutIds.Contains(org.E1.Id));
			Assert.IsTrue(aboutIds.Contains(org.E2.Id));
			Assert.IsTrue(aboutIds.Contains(org.E3.Id));



		}
	}
}
