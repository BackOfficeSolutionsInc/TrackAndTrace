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
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;

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
        [TestCategory("Survey")]
        public async Task ManagerIssues_SelfOn_ManagerOn() {
            var org = await CreateOrg();

            var nodes = QuarterlyConversationAccessor.AvailableAboutsForMe(org.Manager);
            var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(org.Manager, nodes, true, true);
            var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(org.Manager, "Test QC", filtered,new DateRange(), DateTime.UtcNow, false);

            {
                var expectedManagerQ = new[] {
					//"M Role","M Rock","Value",
					"E Role","E Rock","Value",
                    "Rock Quality/Comments",//"Role Comments", "Value Comments",
                    "Gets it", "Wants it","Capacity to do it",
                    "Comments",

                    "Core Values Comments",
                    "5 Roles/GWC Comments",
                    //"# of Rocks completed last Quarter",
                    //"# of Rocks last Quarter",

					//"They are rewarding and recognizing",
					//"They are having quarterly conversations",
					//"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
					//"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
					//"They act with the greater good in mind","They keep expectations clear",

					"I am giving clear direction","I am providing the necessary tools","I am letting go of the vine",
                    "I act with the greater good in mind","I am taking Clarity Breaks™","I keep expectations clear",
                    "I am communicating well","I have the right meeting pulse","I am having quarterly conversations",
                    "I am rewarding and recognizing"



                };

                var managerQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Manager, org.Manager, id);
                var managerQuestions = managerQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
                SetUtility.AssertEqual(managerQuestions, expectedManagerQ);
            }


            {
                var expectedEmployeeQ = new[] {
                    "E Role","E Rock","Value",
                    "Rock Quality/Comments",//"Role Comments", "Value Comments",
                    "Gets it", "Wants it","Capacity to do it",
                    "Comments",

                    "Core Values Comments",
                    "5 Roles/GWC Comments",
                    //"# of Rocks completed last Quarter",
                    //"# of Rocks last Quarter",


                    //"They are rewarding and recognizing",
                    //"They are having quarterly conversations",
                    //"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
                    //"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
                    //"They act with the greater good in mind","They keep expectations clear",

					"I am giving clear direction","I am providing the necessary tools","I am letting go of the vine",
                    "I act with the greater good in mind","I am taking Clarity Breaks™","I keep expectations clear",
                    "I am communicating well","I have the right meeting pulse","I am having quarterly conversations",
                    "I am rewarding and recognizing"
                };

                var employeeQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Employee, org.Employee, id);
                var employeeQuestions = employeeQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
                SetUtility.AssertEqual(employeeQuestions, expectedEmployeeQ);
            }
        }
        [TestMethod]
        [TestCategory("Survey")]
        public async Task ManagerIssues_SelfOn_ManagerOff() {

            var org = await CreateOrg();

            var nodes = QuarterlyConversationAccessor.AvailableAboutsForMe(org.Manager);
            var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(org.Manager, nodes, true, false);
            var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(org.Manager, "Test QC", filtered, new DateRange(), DateTime.UtcNow, false);

            {
                var expectedManagerQ = new[] {
					//"M Role","M Rock","Value",
					"E Role","E Rock","Value",
                    "Rock Quality/Comments",//"Role Comments", "Value Comments",
                    "Gets it", "Wants it","Capacity to do it",
                    "Comments",

                    "Core Values Comments",
                    "5 Roles/GWC Comments",
                   // "# of Rocks completed last Quarter",
                    //"# of Rocks last Quarter",


                    "I am giving clear direction","I am providing the necessary tools","I am letting go of the vine",
                    "I act with the greater good in mind","I am taking Clarity Breaks™","I keep expectations clear",
                    "I am communicating well","I have the right meeting pulse","I am having quarterly conversations",
                    "I am rewarding and recognizing"
					//"They are rewarding and recognizing",
					//"They are having quarterly conversations",
					//"We have the right meeting pulse","They are communicating well","They are providing the necessary tools",
					//"They are giving clear direction","They are letting go of the vine","They are taking Clarity Breaks™",
					//"They act with the greater good in mind","They keep expectations clear"
				};






                var managerQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Manager, org.Manager, id);
                var managerQuestions = managerQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
                SetUtility.AssertEqual(managerQuestions, expectedManagerQ);
            }


            {
                var expectedEmployeeQ = new[] {
                    "E Role","E Rock","Value",
                    "Rock Quality/Comments",//"Role Comments", "Value Comments",
                    "Gets it", "Wants it","Capacity to do it",
                    "Comments",

                    "Core Values Comments",
                    "5 Roles/GWC Comments",
                    //"# of Rocks completed last Quarter",
                    //"# of Rocks last Quarter",
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
        [TestCategory("Survey")]
        public async Task ManagerIssues_SelfOff_ManagerOff() {

            var org = await CreateOrg();

            var nodes = QuarterlyConversationAccessor.AvailableAboutsForMe(org.Manager);
            var filtered = QuarterlyConversationAccessor.AvailableByAboutsFiltered(org.Manager, nodes, false, false);
            var id = await QuarterlyConversationAccessor.GenerateQuarterlyConversation(org.Manager, "Test QC", filtered,new DateRange(), DateTime.UtcNow, false);

            {
                var expectedManagerQ = new[] {
                    "E Role","E Rock","Value",
                    "Rock Quality/Comments",//"Role Comments", "Value Comments",
					"Gets it", "Wants it","Capacity to do it",
                    "Comments",
                    "Core Values Comments",
                    "5 Roles/GWC Comments",
                    //"# of Rocks completed last Quarter",
                    //"# of Rocks last Quarter"
                };

                var managerQC = SurveyAccessor.GetAngularSurveyContainerBy(org.Manager, org.Manager, id);
                var managerQuestions = managerQC.GetSurveys().SelectMany(x => x.GetSections().SelectMany(y => y.GetItems().Select(z => z.GetName()))).ToList();
                SetUtility.AssertEqual(managerQuestions, expectedManagerQ);
            }


            Throws<Exception>(() => {
                SurveyAccessor.GetAngularSurveyContainerBy(org.Employee, org.Employee, id);
            });

        }

        [TestMethod]
        [TestCategory("Survey")]
        public void GuessRocks() {
            var allDates = new[] { new DateTime(2015, 11, 16, 15, 18, 49), new DateTime(2015, 11, 16, 15, 19, 7), new DateTime(2015, 11, 16, 15, 20, 0), new DateTime(2015, 11, 16, 15, 20, 29), new DateTime(2015, 11, 16, 15, 21, 4), new DateTime(2015, 11, 16, 22, 22, 18), new DateTime(2015, 11, 16, 22, 28, 6), new DateTime(2015, 11, 16, 22, 28, 28), new DateTime(2015, 11, 16, 22, 29, 36), new DateTime(2015, 11, 16, 22, 29, 51), new DateTime(2015, 11, 16, 22, 30, 47), new DateTime(2015, 11, 16, 22, 31, 40), new DateTime(2015, 11, 16, 22, 32, 19), new DateTime(2015, 11, 16, 22, 33, 51), new DateTime(2015, 11, 16, 22, 46, 29), new DateTime(2015, 11, 16, 22, 47, 41), new DateTime(2015, 11, 16, 22, 50, 34), new DateTime(2015, 11, 16, 22, 54, 21), new DateTime(2015, 11, 16, 22, 57, 6), new DateTime(2015, 11, 16, 22, 57, 35), new DateTime(2015, 11, 16, 23, 8, 5), new DateTime(2015, 11, 16, 23, 8, 33), new DateTime(2015, 11, 16, 23, 10, 58), new DateTime(2015, 11, 16, 23, 12, 4), new DateTime(2015, 11, 16, 23, 12, 40), new DateTime(2015, 11, 16, 23, 21, 23), new DateTime(2015, 11, 19, 21, 52, 12), new DateTime(2016, 3, 22, 13, 35, 13), new DateTime(2016, 3, 22, 13, 35, 32), new DateTime(2016, 3, 22, 13, 35, 49), new DateTime(2016, 3, 22, 13, 36, 8), new DateTime(2016, 3, 22, 13, 36, 54), new DateTime(2016, 3, 22, 13, 37, 18), new DateTime(2016, 3, 22, 13, 38, 47), new DateTime(2016, 3, 22, 13, 38, 58), new DateTime(2016, 3, 22, 13, 39, 26), new DateTime(2016, 3, 22, 13, 40, 2), new DateTime(2016, 3, 22, 13, 40, 30), new DateTime(2016, 3, 22, 13, 40, 48), new DateTime(2016, 3, 22, 13, 57, 36), new DateTime(2016, 3, 22, 13, 57, 57), new DateTime(2016, 3, 22, 13, 58, 16), new DateTime(2016, 3, 22, 13, 58, 35), new DateTime(2016, 3, 22, 13, 58, 58), new DateTime(2016, 3, 22, 13, 59, 42), new DateTime(2016, 6, 27, 14, 19, 38), new DateTime(2016, 6, 27, 14, 19, 59), new DateTime(2016, 6, 27, 14, 20, 21), new DateTime(2016, 6, 27, 14, 20, 47), new DateTime(2016, 6, 27, 14, 22, 27), new DateTime(2016, 6, 27, 14, 27, 4), new DateTime(2016, 7, 13, 21, 7, 57), new DateTime(2016, 7, 13, 21, 8, 18), new DateTime(2016, 7, 13, 21, 8, 59), new DateTime(2016, 7, 13, 21, 11, 32), new DateTime(2016, 7, 13, 21, 11, 45), new DateTime(2016, 7, 13, 21, 12, 33), new DateTime(2016, 7, 13, 21, 12, 42), new DateTime(2016, 7, 13, 21, 12, 53), new DateTime(2016, 7, 13, 21, 12, 59), new DateTime(2016, 7, 13, 21, 13, 48), new DateTime(2016, 7, 13, 21, 14, 15), new DateTime(2016, 7, 13, 21, 14, 30), new DateTime(2016, 7, 13, 21, 14, 46), new DateTime(2016, 7, 13, 21, 14, 57), new DateTime(2016, 7, 13, 21, 15, 18), new DateTime(2016, 7, 13, 21, 15, 33), new DateTime(2016, 7, 13, 21, 15, 45), new DateTime(2016, 7, 13, 21, 15, 58), new DateTime(2016, 7, 13, 21, 16, 7), new DateTime(2016, 7, 18, 17, 42, 24), new DateTime(2016, 7, 18, 17, 42, 48), new DateTime(2016, 10, 3, 18, 36, 59), new DateTime(2016, 10, 3, 18, 37, 44), new DateTime(2016, 10, 3, 18, 37, 58), new DateTime(2016, 10, 3, 18, 38, 26), new DateTime(2016, 10, 3, 18, 38, 33), new DateTime(2016, 10, 3, 18, 38, 49), new DateTime(2016, 10, 3, 18, 39, 15), new DateTime(2016, 10, 3, 18, 41, 23), new DateTime(2016, 10, 3, 18, 42, 50), new DateTime(2016, 10, 3, 18, 50, 8), new DateTime(2016, 10, 3, 18, 51, 9), new DateTime(2016, 10, 3, 18, 51, 13), new DateTime(2016, 10, 3, 18, 52, 9), new DateTime(2016, 10, 3, 19, 24, 58), new DateTime(2016, 10, 3, 19, 25, 18), new DateTime(2016, 10, 3, 19, 27, 20), new DateTime(2016, 10, 7, 20, 50, 17), new DateTime(2016, 10, 7, 20, 50, 34), new DateTime(2016, 10, 7, 20, 50, 39), new DateTime(2016, 10, 7, 20, 50, 43), new DateTime(2016, 10, 7, 20, 50, 47), new DateTime(2016, 10, 7, 20, 50, 54), new DateTime(2016, 10, 7, 20, 51, 10), new DateTime(2016, 10, 7, 20, 53, 22), new DateTime(2016, 10, 7, 20, 53, 30), new DateTime(2016, 10, 7, 20, 55, 32), new DateTime(2016, 10, 7, 20, 55, 37), new DateTime(2016, 12, 27, 16, 10, 1), new DateTime(2016, 12, 27, 16, 10, 3), new DateTime(2016, 12, 27, 16, 10, 9), new DateTime(2016, 12, 27, 16, 10, 15), new DateTime(2016, 12, 27, 16, 10, 19), new DateTime(2016, 12, 27, 16, 10, 33), new DateTime(2016, 12, 27, 16, 10, 47), new DateTime(2016, 12, 27, 16, 10, 57), new DateTime(2016, 12, 27, 16, 11, 8), new DateTime(2016, 12, 27, 16, 11, 13), new DateTime(2016, 12, 27, 16, 11, 25), new DateTime(2016, 12, 27, 16, 11, 34), new DateTime(2016, 12, 27, 16, 12, 39), new DateTime(2016, 12, 27, 16, 12, 49), new DateTime(2016, 12, 27, 16, 12, 56), new DateTime(2016, 12, 27, 16, 13, 1), new DateTime(2016, 12, 27, 16, 13, 14), new DateTime(2016, 12, 27, 16, 13, 20), new DateTime(2016, 12, 27, 16, 41, 51), new DateTime(2016, 12, 27, 16, 42, 7), new DateTime(2016, 12, 27, 16, 42, 16), new DateTime(2016, 12, 27, 16, 42, 21), new DateTime(2016, 12, 27, 16, 42, 33), new DateTime(2016, 12, 27, 16, 42, 49), new DateTime(2016, 12, 27, 16, 42, 59), new DateTime(2016, 12, 27, 16, 44, 27), new DateTime(2017, 2, 6, 17, 31, 21), new DateTime(2017, 2, 6, 17, 31, 35), new DateTime(2017, 4, 10, 17, 42, 4), new DateTime(2017, 4, 10, 17, 42, 13), new DateTime(2017, 4, 10, 17, 42, 17), new DateTime(2017, 4, 10, 17, 42, 23), new DateTime(2017, 4, 10, 17, 42, 29), new DateTime(2017, 4, 10, 17, 42, 40), new DateTime(2017, 4, 10, 17, 42, 47), new DateTime(2017, 4, 10, 17, 43, 5), new DateTime(2017, 4, 10, 17, 43, 12), new DateTime(2017, 4, 10, 17, 43, 29), new DateTime(2017, 4, 10, 17, 43, 32), new DateTime(2017, 4, 10, 17, 43, 46), new DateTime(2017, 4, 17, 15, 20, 59), new DateTime(2017, 4, 17, 15, 21, 10), new DateTime(2017, 4, 17, 15, 21, 22), new DateTime(2017, 4, 17, 15, 21, 34), new DateTime(2017, 4, 17, 15, 21, 45), new DateTime(2017, 4, 27, 15, 16, 23), new DateTime(2017, 4, 27, 15, 16, 59), new DateTime(2017, 4, 27, 15, 22, 11), new DateTime(2017, 4, 27, 15, 23, 26), new DateTime(2017, 4, 27, 15, 24, 17), new DateTime(2017, 4, 27, 15, 25, 17), new DateTime(2017, 4, 27, 15, 25, 34), new DateTime(2017, 4, 27, 15, 25, 59), new DateTime(2017, 7, 17, 21, 15, 52), new DateTime(2017, 7, 17, 21, 16, 29), new DateTime(2017, 7, 17, 21, 16, 46), new DateTime(2017, 7, 17, 21, 17, 11), new DateTime(2017, 7, 17, 21, 17, 23), new DateTime(2017, 7, 17, 21, 18, 13), new DateTime(2017, 7, 17, 21, 18, 39), new DateTime(2017, 7, 17, 21, 19, 0), new DateTime(2017, 7, 17, 21, 19, 18), new DateTime(2017, 7, 17, 21, 19, 33), new DateTime(2017, 7, 17, 21, 19, 44), new DateTime(2017, 7, 17, 21, 19, 49), new DateTime(2017, 7, 17, 21, 19, 56), new DateTime(2017, 7, 17, 21, 20, 2), new DateTime(2017, 7, 17, 21, 20, 17), new DateTime(2017, 7, 17, 21, 20, 26), new DateTime(2017, 7, 17, 21, 20, 52), new DateTime(2017, 8, 7, 15, 26, 55), new DateTime(2017, 9, 14, 17, 4, 56), new DateTime(2017, 9, 14, 17, 5, 5), new DateTime(2017, 9, 14, 17, 5, 40), new DateTime(2017, 10, 16, 23, 15, 56), new DateTime(2017, 10, 16, 23, 16, 13), new DateTime(2017, 10, 16, 23, 16, 23), new DateTime(2017, 10, 16, 23, 17, 25), new DateTime(2017, 10, 16, 23, 17, 48), new DateTime(2017, 10, 16, 23, 19, 58), new DateTime(2017, 10, 16, 23, 20, 15), new DateTime(2017, 10, 16, 23, 20, 16), new DateTime(2017, 10, 16, 23, 20, 17), new DateTime(2017, 10, 16, 23, 20, 36), new DateTime(2017, 10, 16, 23, 23, 9), new DateTime(2017, 10, 16, 23, 23, 22), new DateTime(2017, 10, 16, 23, 23, 28), new DateTime(2017, 10, 16, 23, 23, 45), new DateTime(2017, 10, 16, 23, 23, 50), new DateTime(2017, 11, 3, 14, 39, 1), new DateTime(2017, 11, 3, 14, 39, 23), new DateTime(2017, 11, 3, 14, 39, 39), new DateTime(2017, 11, 3, 14, 39, 59), new DateTime(2017, 11, 20, 15, 48, 20), new DateTime(2017, 11, 20, 15, 48, 47), new DateTime(2017, 11, 20, 15, 48, 55), new DateTime(2017, 11, 20, 15, 49, 43), new DateTime(2017, 11, 20, 15, 50, 9), new DateTime(2017, 11, 20, 15, 50, 32), new DateTime(2017, 11, 20, 15, 51, 14), new DateTime(2017, 11, 20, 15, 51, 24), new DateTime(2017, 11, 20, 15, 51, 32), new DateTime(2017, 12, 11, 16, 47, 20), new DateTime(2017, 12, 11, 16, 48, 4), new DateTime(2017, 12, 11, 18, 34, 48), new DateTime(2017, 12, 11, 18, 35, 57), new DateTime(2017, 12, 11, 18, 37, 16), new DateTime(2017, 12, 18, 21, 16, 58), new DateTime(2017, 12, 18, 21, 17, 13), new DateTime(2017, 12, 18, 21, 17, 28), new DateTime(2018, 1, 2, 21, 26, 37), new DateTime(2018, 1, 2, 21, 38, 53) }.ToList();


            var allRemoved = QuarterGuessUtility.RemoveOutliers(allDates);
            foreach(var a in allRemoved) {
                Console.WriteLine(a.Date.ToShortDateString()+","+a.Confidence);
            }
            int b = 0;
        }

    }
}
