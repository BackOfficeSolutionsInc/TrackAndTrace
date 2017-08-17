using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Areas.People.Accessors;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Areas.People.Models.Survey;
using System.Linq;
using RadialReview;

namespace TractionTools.Tests.PeopleTools {
	[TestClass]
	public class QcSettings : BaseTest {

		private async Task<Org> CreateOrg() {
			var org = await OrgUtil.CreateOrganization();

			DbCommit(s => {
				var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK);

				//Init values
				s.Save(new CompanyValueModel() {
					OrganizationId = org.Id,
					CompanyValue = "Value",
					CompanyValueDetails = "Value Details ",
					Category = category
				});
				//Init roles
				var eRole = new RoleModel() {
					OrganizationId = org.Id,
					Role = "E Role",
					Category = category
				};
				s.Save(eRole);
				var mRole = new RoleModel() {
					OrganizationId = org.Id,
					Role = "M Role",
					Category = category
				};
				s.Save(mRole);
				//Init role links
				s.Save(new RoleLink() {
					RoleId = eRole.Id,
					AttachId = org.Employee.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});
				s.Save(new RoleLink() {
					RoleId = mRole.Id,
					AttachId = org.Manager.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});
				//Save Rocks
				s.Save(new RockModel() {
					Rock = "M Rock",
					ForUserId = org.Manager.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "E Rock",
					ForUserId = org.Employee.Id,
					OrganizationId = org.Id,
					Category = category
				});
			});
			return org;
		}


		[TestMethod]
		public async Task ManagerIssues_SelfOn_ManagerOn() {
			var org = await CreateOrg();

			var nodes = QuarterlyConversationAccessor.AvailableAboutsForMe(org.Manager);
			var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(org.Manager, nodes, true, true);
			var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(org.Manager, "Test QC", filtered, DateTime.UtcNow, false);

			{
				var expectedManagerQ = new[] {
					//"M Role","M Rock","Value",
					"E Role","E Rock","Value",
					"Rock Quality/Comments","Role Comments", "Value Comments",
					"Gets it", "Wants it","Capacity to do it",
					"Comments",
					"They are rewarding and recognizing",
					"They are having quarterly conversations",
					"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
					"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
					"They act with the greater good in mind","They keep expectations clear"
				};

				var managerQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Manager, org.Manager, id);
				var managerQuestions = managerQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
				SetUtility.AssertEqual(managerQuestions, expectedManagerQ);
			}


			{
				var expectedEmployeeQ = new[] {
					"E Role","E Rock","Value",
					"Rock Quality/Comments","Role Comments", "Value Comments",
					"Gets it", "Wants it","Capacity to do it",
					"Comments",
					"They are rewarding and recognizing",
					"They are having quarterly conversations",
					"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
					"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
					"They act with the greater good in mind","They keep expectations clear"
				};

				var employeeQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Employee, org.Employee, id);
				var employeeQuestions = employeeQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
				SetUtility.AssertEqual(employeeQuestions, expectedEmployeeQ);
			}
		}
		[TestMethod]
		public async Task ManagerIssues_SelfOn_ManagerOff() {

			var org = await CreateOrg();

			var nodes = QuarterlyConversationAccessor.AvailableAboutsForMe(org.Manager);
			var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(org.Manager, nodes, true, false);
			var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(org.Manager, "Test QC", filtered, DateTime.UtcNow, false);

			{
				var expectedManagerQ = new[] {
					//"M Role","M Rock","Value",
					"E Role","E Rock","Value",
					"Rock Quality/Comments","Role Comments", "Value Comments",
					"Gets it", "Wants it","Capacity to do it",
					"Comments",
					"They are rewarding and recognizing",
					"They are having quarterly conversations",
					"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
					"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
					"They act with the greater good in mind","They keep expectations clear"
				};

				var managerQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Manager, org.Manager, id);
				var managerQuestions = managerQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
				SetUtility.AssertEqual(managerQuestions, expectedManagerQ);
			}


			{
				var expectedEmployeeQ = new[] {
					"E Role","E Rock","Value",
					"Rock Quality/Comments","Role Comments", "Value Comments",
					"Gets it", "Wants it","Capacity to do it",
					"Comments",
					//"They are rewarding and recognizing",
					//"They are having quarterly conversations",
					//"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
					//"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
					//"They act with the greater good in mind","They keep expectations clear"
				};

				var employeeQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Employee, org.Employee, id);
				var employeeQuestions = employeeQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
				SetUtility.AssertEqual(employeeQuestions, expectedEmployeeQ);
			}
		}



		[TestMethod]
		public async Task ManagerIssues_SelfOff_ManagerOff() {

			var org = await CreateOrg();

			var nodes = QuarterlyConversationAccessor.AvailableAboutsForMe(org.Manager);
			var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(org.Manager, nodes, false, false);
			var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(org.Manager, "Test QC", filtered, DateTime.UtcNow, false);

			{
				var expectedManagerQ = new[] {
					"E Role","E Rock","Value",
					"Rock Quality/Comments","Role Comments", "Value Comments",
					"Gets it", "Wants it","Capacity to do it",
					"Comments"
				};

				var managerQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Manager, org.Manager, id);
				var managerQuestions = managerQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
				SetUtility.AssertEqual(managerQuestions, expectedManagerQ);
			}


			Throws<Exception>(() => {
				SurveyAccessor.GetAngularSurveyContainerBy(org.Employee, org.Employee, id);
			});

		}
	}
}
