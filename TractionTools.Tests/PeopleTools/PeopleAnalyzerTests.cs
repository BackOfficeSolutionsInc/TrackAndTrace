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
using RadialReview.Models.L10;
using RadialReview.Exceptions;
using RadialReview.Utilities.DataTypes;

namespace TractionTools.Tests.PeopleTools {
	[TestClass]
	public class PeopleAnalyzerTests :BaseTest {


		private static async Task<long> GenQCAsync(string name, UserOrganizationModel user) {
			var abouts = QuarterlyConversationAccessor.AvailableAboutsForMe(user);
			var byAbouts = QuarterlyConversationAccessor.AvailableByAboutsFiltered(user, abouts, true, true);
			var range = new DateRange(DateTime.UtcNow.AddDays(-90),DateTime.UtcNow.AddDays(1));
			var id= await QuarterlyConversationAccessor.GenerateQuarterlyConversation(user.Id, name, byAbouts, range, DateTime.UtcNow.AddDays(1), false);
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
							case SurveyQuestionIdentifier.GeneralComment:
								SurveyAccessor.UpdateAngularSurveyResponse(user, q.Response.Id, "comment...");
								break;
							default:
								break;
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


			//l10.AddAdmin(org.Manager);
			await l10.AddAttendee(org.Employee);
			await l10.AddAttendee(org.Middle);
			await l10.AddAttendee(org.E1);

			await GenQCAsync("middle", org.Middle);

			{
				//Should show up for Middle
				var visible = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.Middle, org.Middle.Id, l10.Id);
				var aboutIds = visible.Rows.Select(v => v.About.ModelId);

				Assert.AreEqual(3, aboutIds.Count());
				//Assert.IsTrue(aboutIds.Contains(org.MiddleNode.Id));
				Assert.IsTrue(aboutIds.Contains(org.E1BottomNode.Id));
				Assert.IsTrue(aboutIds.Contains(org.E2Node.Id));
				Assert.IsTrue(aboutIds.Contains(org.E3Node.Id));
			}

			{
				//no error when meeting is not specified...
				QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.E4, org.E4.Id/*, NOT SPECIFIED */);

				try {
					//But throw an error when meeting is specified and employee is not a member of the L10
					QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.E4, org.E4.Id, l10.Id);
					Assert.Fail();
				} catch (PermissionsException) {
				}				
			}

			{
				//should not show up for Manager, as Manager hasnt agreed yet.
				var visible = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.Employee, org.Employee.Id, l10.Id);
				var aboutIds = visible.Rows.Select(v => v.About.ModelId);
				Assert.AreEqual(0, aboutIds.Count());
			}

			//Lets share it ...
			await L10Accessor.SharePeopleAnalyzer(org.Middle, org.Middle.Id, l10.Id, L10Recurrence.SharePeopleAnalyzer.Yes);
			{
				//should show up for Manager since they have now agreed.
				var visible = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.Employee, org.Employee.Id, l10.Id);
				var aboutIds = visible.Rows.Select(v => v.About.ModelId);
				Assert.AreEqual(3, aboutIds.Count());
				//Assert.IsTrue(aboutIds.Contains(org.MiddleNode.Id));
				Assert.IsTrue(aboutIds.Contains(org.E1BottomNode.Id));
				Assert.IsTrue(aboutIds.Contains(org.E2Node.Id));
				Assert.IsTrue(aboutIds.Contains(org.E3Node.Id));
			}

			//Lets unshare it ...
			await L10Accessor.SharePeopleAnalyzer(org.Middle, org.Middle.Id, l10.Id, L10Recurrence.SharePeopleAnalyzer.No);
			{
				//should show up for Manager since they have now agreed.
				var visible = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.Employee, org.Employee.Id, l10.Id);
				var aboutIds = visible.Rows.Select(v => v.About.ModelId);
				Assert.AreEqual(0, aboutIds.Count());
			}
			//Lets unset it ...
			await L10Accessor.SharePeopleAnalyzer(org.Middle, org.Middle.Id, l10.Id, L10Recurrence.SharePeopleAnalyzer.Unset);
			{
				//should show up for Manager since they have now agreed.
				var visible = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(org.Employee, org.Employee.Id, l10.Id);
				var aboutIds = visible.Rows.Select(v => v.About.ModelId);
				Assert.AreEqual(0, aboutIds.Count());
			}
		}
	}
}
