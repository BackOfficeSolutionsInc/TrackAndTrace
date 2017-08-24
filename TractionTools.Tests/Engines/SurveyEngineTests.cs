using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Areas.People.Engines.Surveys;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Models.Components;
using RadialReview.Models;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using Moq;
using NHibernate;
using System.Collections.Generic;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Askables;
using NHibernate.Criterion;
using System.Linq.Expressions;
using RadialReview.Areas.People.Engines.Surveys.Impl;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections;
using System.Linq;
using Newtonsoft.Json.Linq;
using TractionTools.Tests.Utilities;
using RadialReview.Properties;
using TractionTools.Tests.Properties;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview;
using TractionTools.Tests.TestUtils;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Reconstructor;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Traverse;
using RadialReview.Areas.People.Angular.Survey;
using System.Threading.Tasks;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Transformers;
using RadialReview.Utilities;

namespace TractionTools.Tests.Engines {
	[TestClass]
	public class SurveyEngineTests : BaseTest {

		#region Printing
		private string Print(IComponent component, int depth = 0) {
			var tab = "    ";
			var tabs = "";
			for (int i = 0; i < depth; i++) {
				tabs += tab;
			}
			var builder = tabs + (component.ToPrettyString().Replace("\n", "\n" + tabs).Replace("\t", tab));
			Console.WriteLine(builder);
			return builder + "\n";
		}
		private string PrintContainer(ISurveyContainer surveyContainer) {
			var builder = "";
			foreach (var survey in surveyContainer.GetSurveys()) {
				builder += Print(survey);
				foreach (var section in survey.GetSections()) {
					builder += Print(section, 1);
					foreach (var item in section.GetItemContainers()) {
						builder += Print(item, 2);
					}
				}
			}
			return builder;
		}
		#endregion


		private void ConstructSurveyEnv(FullOrg org) {
			//Init DB
			DbCommit(s => {
				var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK);

				//Init values
				for (int i = 0; i < 3; i++) {
					s.Save(new CompanyValueModel() {
						OrganizationId = org.Id,
						CompanyValue = "Value " + i,
						CompanyValueDetails = "Value Details " + i,
						Category = category
					});
				}

				//Init roles
				var roles = new List<RoleModel>();
				for (int i = 0; i < 6; i++) {
					var role = new RoleModel() {
						OrganizationId = org.Id,
						Role = "Role " + i,
						Category = category
					};
					s.Save(role);
					roles.Add(role);
				}

				//Init role links
				s.Save(new RoleLink() {
					RoleId = roles[0].Id,
					AttachId = org.Employee.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});
				s.Save(new RoleLink() {
					RoleId = roles[1].Id,
					AttachId = org.E1.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});
				s.Save(new RoleLink() {
					RoleId = roles[2].Id,
					AttachId = org.InterreviewTeam.Id,
					AttachType = AttachType.Team,
					OrganizationId = org.Id,
				});
				s.Save(new RoleLink() {
					RoleId = roles[3].Id,
					AttachId = org.E2.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});
				s.Save(new RoleLink() {
					RoleId = roles[4].Id,
					AttachId = org.E2.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});
				s.Save(new RoleLink() {
					RoleId = roles[5].Id,
					AttachId = org.E3.Id,
					AttachType = AttachType.User,
					OrganizationId = org.Id,
				});

				//Init Rocks
				s.Save(new RockModel() {
					Rock = "Rock 1",
					ForUserId = org.E1.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "Rock 2",
					ForUserId = org.E2.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "Rock 3",
					ForUserId = org.E2.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "Rock 4",
					ForUserId = org.E3.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "Rock 5",
					ForUserId = org.E3.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "Rock 6",
					ForUserId = org.E3.Id,
					OrganizationId = org.Id,
					Category = category
				});
				s.Save(new RockModel() {
					Rock = "Rock 7",
					ForUserId = org.E4.Id,
					OrganizationId = org.Id,
					Category = category
				});
			});
		}
		private ISurveyContainer ConstructSurvey(FullOrg org) {
			ISurveyContainer container = null;
			//Construct Survey
			DbCommit(s => {
				var engine = new SurveyBuilderEngine(
					new QuarterlyConversationInitializer(ForModel.Create(org.Manager), "TestSurvey", org.Id, DateTime.MaxValue),
					new SurveyBuilderEventsSaveStrategy(s),
					new TransformAboutAccountabilityNodes(s)
				);

				var byAbout = new[] {
					new ByAbout(org.Manager,org.Manager),
					new ByAbout(org.Manager,org.Employee),
					new ByAbout(org.Employee,org.Employee),
					new ByAbout(org.Middle,org.Middle),
					new ByAbout(org.Middle,org.E1),
					new ByAbout(org.E1,org.E1),
					new ByAbout(org.Middle,org.E2),
					new ByAbout(org.E2,org.E2),
					new ByAbout(org.Middle,org.E3),
					new ByAbout(org.E3,org.E3),
					new ByAbout(org.E2,org.E6),
				};

				container = engine.BuildSurveyContainer(byAbout);
			});
			return container;
		}


		[TestMethod]
		[TestCategory("Survey")]
		public async Task SmokeTestDatabase() {
			var org = await OrgUtil.CreateFullOrganization();
			ConstructSurveyEnv(org);
			var container = ConstructSurvey(org);

			var angularContainer = new AngularSurveyContainer();
			DbQuery(s => {
				var engine = new SurveyReconstructionEngine(container.Id, org.Id, new DatabaseAggregator(s), null);
				var sc = engine.ReconstructSurveyContainer();

				engine.Traverse(new TraverseBuildAngular(angularContainer));
			});

			Assert.AreEqual(container.Id, angularContainer.Id);
			Assert.AreEqual(container.GetSurveys().ElementAt(2).Id, angularContainer.GetSurveys().ElementAt(2).Id);
			Assert.AreEqual(container.GetSurveys().ElementAt(2).GetSections().ElementAt(1).Id, angularContainer.GetSurveys().ElementAt(2).GetSections().ElementAt(1).Id);

			Assert.IsInstanceOfType(container, typeof(SurveyContainer));
			Assert.IsInstanceOfType(angularContainer, typeof(AngularSurveyContainer));
		}

		[TestMethod]
		[TestCategory("Survey")]
		public void SmokeTestRocks() {

			// using (CompareUtil.StaticComparer<SurveyItemFormat, int>("CtorCalls", x => x + 1)) {

			long R1 = 1, R2 = 2, R3 = 3, R4 = 4, R5 = 5, R6 = 6, R7 = 7;
			long U2 = 2, U3 = 3, U4 = 4;


			var expectedUsersRocks = new Dictionary<long, long[]> {
					{ U2, new [] { R1, R2, R3, R4} },
					{ U3, new [] { R5, R6 } },
					{ U4, new [] { R7 } }
				};


			var rockData = new[] {
					new RockModel() {Id =R1, ForUserId = U2, Rock = "User2 - Rock1" },
					new RockModel() {Id =R2, ForUserId = U2, Rock = "User2 - Rock2" },
					new RockModel() {Id =R3, ForUserId = U2, Rock = "User2 - Rock3" },
					new RockModel() {Id =R4, ForUserId = U2, Rock = "User2 - Rock4" },
					new RockModel() {Id =R5, ForUserId = U3, Rock = "User3 - Rock1" },
					new RockModel() {Id =R6, ForUserId = U3, Rock = "User3 - Rock2" },
					new RockModel() {Id =R7, ForUserId = U4, Rock = "User4 - Rock1" },
				};

			var byAbout = new[] {
					new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U2)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U3)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U4)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U3),ForModel.Create<UserOrganizationModel>(U2)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U3),ForModel.Create<UserOrganizationModel>(U3)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U4),ForModel.Create<UserOrganizationModel>(U4)),
				};

			var outerLookup = new OuterLookup();
			outerLookup.GetInnerLookup<RockSection>().AddList(rockData);

			var engine = new SurveyBuilderEngine(
				new QuarterlyConversationInitializer(ForModel.Create<UserOrganizationModel>(2), "TestRocksSurvey", 1, DateTime.MaxValue),
				new SurveyBuilderEventsNoOp(),
				new TransformByAboutNoop(),
				outerLookup
			);

			var surveyContainer = engine.BuildSurveyContainer(byAbout);
			var result = PrintContainer(surveyContainer);

			var testRocks = new Action<long, long[]>((userId, expectedRoles) => {
				surveyContainer.GetSurveys()
					.Where(survey => survey.GetAbout().ModelId == userId)
					.SelectMany(survey => survey.GetSections())
					.Where(sec => sec.GetSectionType() == "" + SurveySectionType.Rocks)
					.ToList()
					.ForEach(sec => {
						var roleIds = sec.GetItems().Where(x => x.GetSource() != null && x.GetSource().Is<RockModel>()).Select(x => x.GetSource().ModelId);
						SetUtility.AssertEqual(expectedRoles, roleIds);
					});
			});

			//Confirm rocks
			foreach (var kv in expectedUsersRocks) {
				testRocks(kv.Key, kv.Value);
			}
			//}
		}

		[TestMethod]
		[TestCategory("Survey")]
		public void SmokeTestValues() {
			//var mockSession = new Mock<ISession>();
			//mockSession.Setup(foo => foo.QueryOver<RockModel>().Where(It.IsAny<Expression<Func<RockModel,bool>>>()).Future()).Returns(() => new List<RockModel>() {
			// using (CompareUtil.StaticComparer<SurveyItemFormat, int>("CtorCalls", x => x + 1)) {

			long V1 = 1, V2 = 2, V3 = 3;
			long U2 = 2, U3 = 3, U4 = 4;

			var expectedUsersValues = new Dictionary<long, long[]> {
					{ U2, new [] { V1, V2, V3} },
					{ U3, new [] { V1, V2, V3} },
					{ U4, new [] { V1, V2, V3} },
				};

			var valueData = new[] {
					new CompanyValueModel() {Id= V1, CompanyValue = "Value1", CompanyValueDetails = "Details1"},
					new CompanyValueModel() {Id= V2, CompanyValue = "Value2", CompanyValueDetails = "Details2"},
					new CompanyValueModel() {Id= V3, CompanyValue = "Value3", CompanyValueDetails = "Details3"},
				};

			var byAbout = new[] {
					new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U2)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U3)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U4)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U3),ForModel.Create<UserOrganizationModel>(U2)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U3),ForModel.Create<UserOrganizationModel>(U3)),
					new ByAbout(ForModel.Create<UserOrganizationModel>(U4),ForModel.Create<UserOrganizationModel>(U4)),
				};

			var outerLookup = new OuterLookup();
			outerLookup.GetInnerLookup<ValueSection>().AddList(valueData);

			var engine = new SurveyBuilderEngine(
				new QuarterlyConversationInitializer(ForModel.Create<UserOrganizationModel>(2), "TestValueSurvey", 1, DateTime.MaxValue),
				new SurveyBuilderEventsNoOp(),
				new TransformByAboutNoop(),
				outerLookup
			);

			var surveyContainer = engine.BuildSurveyContainer(byAbout);
			var result = PrintContainer(surveyContainer);

			var testValues = new Action<long, long[]>((userId, expectedValues) => {
				surveyContainer.GetSurveys()
					.Where(survey => survey.GetAbout().ModelId == userId)
					.SelectMany(survey => survey.GetSections())
					.Where(sec => sec.GetSectionType() == "" + SurveySectionType.Values)
					.ToList()
					.ForEach(sec => {
						var valueIds = sec.GetItems().Where(x => x.GetSource() != null && x.GetSource().Is<CompanyValueModel>()).Select(x => x.GetSource().ModelId);
						SetUtility.AssertEqual(expectedValues, valueIds);
					});
			});

			//Confirm rocks
			foreach (var kv in expectedUsersValues) {
				testValues(kv.Key, kv.Value);
			}
			//}
		}

		[TestMethod]
		[TestCategory("Survey")]
		public void SmokeTestRoles() {
			//var mockSession = new Mock<ISession>();
			//mockSession.Setup(foo => foo.QueryOver<RockModel>().Where(It.IsAny<Expression<Func<RockModel,bool>>>()).Future()).Returns(() => new List<RockModel>() {
			// using (CompareUtil.StaticComparer<SurveyItemFormat, int>("CtorCalls", x => x + 4)) {

			long U1 = 1, U2 = 2, U3 = 3, U4 = 4;
			long R1 = 1, R2 = 2, R3 = 3, R4 = 4, R5 = 5, R6 = 6, R7 = 7;

			long T1 = 8;
			long P1 = 11;
			long O1 = 12;

			var userExpectedRoles = new Dictionary<long, long[]>() {
				   { U1, new[] { R6 }},
				   { U2, new[] { R2, R6, R7 }},
				   { U3, new[] { R3, R6 }},
				   { U4, new[] { R4, R5, R7 }}
				};

			var roleData = new[] {
					new RoleModel() {Id = R1, Role = "Role1" },
					new RoleModel() {Id = R2, Role = "Role2" },
					new RoleModel() {Id = R3, Role = "Role3" },
					new RoleModel() {Id = R4, Role = "Role4" },
					new RoleModel() {Id = R5, Role = "Role5" },
					new RoleModel() {Id = R6, Role = "Role6" },
					new RoleModel() {Id = R7, Role = "Role7" },
				};

			var roleLinks = new[] {
					new RoleLink() {AttachType=AttachType.User,     AttachId = U2,       RoleId =R2 },
					new RoleLink() {AttachType=AttachType.User,     AttachId = U3,       RoleId =R3 },
					new RoleLink() {AttachType=AttachType.User,     AttachId = U4,       RoleId =R4 },
					new RoleLink() {AttachType=AttachType.User,     AttachId = U4,       RoleId =R5 },
					new RoleLink() {AttachType=AttachType.Team,     AttachId = T1,       RoleId =R6 },
					new RoleLink() {AttachType=AttachType.Position, AttachId = P1,       RoleId =R7 },
				};

			var teamDur = new[] {
					new TeamDurationModel() { UserId = U1 , TeamId = T1,},
					new TeamDurationModel() { UserId = U2 , TeamId = T1,},
					new TeamDurationModel() { UserId = U3 , TeamId = T1,},
				};

			var posDur = new[] {
					new PositionDurationModel() { UserId = U2,  Position = new OrganizationPositionModel() {Id= P1 }},
					new PositionDurationModel() { UserId = U4,  Position = new OrganizationPositionModel() {Id= P1 }},
				};


			var outerLookup = new OuterLookup();
			var roleQuery = new RoleAccessor.RoleLinksQuery(roleData, roleLinks, teamDur, posDur);
			outerLookup.GetInnerLookup<RoleSection>().Add("RoleQuery", roleQuery);

			var byAbout = new[] {
				new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U2)),
				new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U3)),
				new ByAbout(ForModel.Create<UserOrganizationModel>(U2),ForModel.Create<UserOrganizationModel>(U4)),
				new ByAbout(ForModel.Create<UserOrganizationModel>(U3),ForModel.Create<UserOrganizationModel>(U2)),
				new ByAbout(ForModel.Create<UserOrganizationModel>(U3),ForModel.Create<UserOrganizationModel>(U3)),
				new ByAbout(ForModel.Create<UserOrganizationModel>(U4),ForModel.Create<UserOrganizationModel>(U4)),
			};

			var engine = new SurveyBuilderEngine(
				new QuarterlyConversationInitializer(ForModel.Create<UserOrganizationModel>(U2), "TestRoleSurvey", O1, DateTime.MaxValue),
				new SurveyBuilderEventsNoOp(),
				new TransformByAboutNoop(),// SurveyBuilderEventsSaveStrategy(mockSession.Object)
				outerLookup
			);

			var surveyContainer = engine.BuildSurveyContainer(byAbout);

			var result = PrintContainer(surveyContainer);

			var testRoles = new Action<long, long[]>((userId, expectedRoles) => {
				surveyContainer.GetSurveys()
					.Where(survey => survey.GetAbout().ModelId == userId)
					.SelectMany(survey => survey.GetSections())
					.Where(sec => sec.GetSectionType() == "" + SurveySectionType.Roles)
					.ToList()
					.ForEach(sec => {
						var roleIds = sec.GetItems().Where(x => x.GetSource() != null && x.GetSource().Is<RoleModel>()).Select(x => x.GetSource().ModelId);
						SetUtility.AssertEqual(expectedRoles, roleIds);
					});
			});
			foreach (var kv in userExpectedRoles) {
				testRoles(kv.Key, kv.Value);
			}

			//sec.GetItems())

			//CompareUtil.AssertObjectJsonEqualsString(""/*SurveyResources.RockSectionSmokeTest*/, surveyContainer);

			// }
		}

		[TestMethod]
		[TestCategory("Survey")]
		public async Task TestGetSurveyContainerAbout() {

			var org = await OrgUtil.CreateOrganization();

			DbCommit(s => {
				var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK);

				//Init values
				for (int i = 0; i < 3; i++) {
					s.Save(new CompanyValueModel() {
						OrganizationId = org.Id,
						CompanyValue = "Value " + i,
						CompanyValueDetails = "Value Details " + i,
						Category = category
					});
				}
				//Init roles
				var roles = new List<RoleModel>();
				for (int i = 0; i < 6; i++) {
					var role = new RoleModel() {
						OrganizationId = org.Id,
						Role = "Role " + i,
						Category = category
					};
					s.Save(role);
					roles.Add(role);
				}
				//Init role links
				for (var i = 0; i < 3; i++) {
					s.Save(new RoleLink() {
						RoleId = roles[i].Id,
						AttachId = org.Employee.Id,
						AttachType = AttachType.User,
						OrganizationId = org.Id,
					});
					s.Save(new RockModel() {
						Rock = "Rock -" + i,
						ForUserId = org.Employee.Id,
						OrganizationId = org.Id,
						Category = category
					});
				}
				for (var i = 3; i < 6; i++) {
					s.Save(new RoleLink() {
						RoleId = roles[i].Id,
						AttachId = org.Manager.Id,
						AttachType = AttachType.User,
						OrganizationId = org.Id,
					});
					s.Save(new RockModel() {
						Rock = "Rock -" + i,
						ForUserId = org.Manager.Id,
						OrganizationId = org.Id,
						Category = category
					});
				}
				//add extra 
				s.Save(new RockModel() {
					Rock = "Rock -" + 6,
					ForUserId = org.Employee.Id,
					OrganizationId = org.Id,
					Category = category
				});
			});




			//var container = ConstructSurvey(org);
			//var byAbout = new[] {
			//		new ByAboutSurveyUserNode(SurveyUserNode.Create(org.ManagerNode{UserOrganizationId= org.Manager.Id, },new SurveyUserNode() {AccountabilityNodeId= org.ManagerNode.Id }, AboutType.Self),
			//		new ByAboutSurveyUserNode(new SurveyUserNode() {UserOrganizationId=org.Manager.Id },new SurveyUserNode() {AccountabilityNodeId=org.EmployeeNode.Id }, AboutType.Subordinate),
			//		new ByAboutSurveyUserNode(new SurveyUserNode() {AccountabilityNodeId= org.EmployeeNode.Id },new SurveyUserNode() {AccountabilityNodeId=org.EmployeeNode.Id }, AboutType.Self),
			//	};
			var manager = SurveyUserNode.Create(org.ManagerNode);
			var empl = SurveyUserNode.Create(org.EmployeeNode);
			var byAbout = new[] {
					new ByAboutSurveyUserNode(manager,manager, AboutType.Self),
					new ByAboutSurveyUserNode(manager, empl, AboutType.Subordinate),
					new ByAboutSurveyUserNode(empl,empl, AboutType.Self),
				};
		long containerId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, org.Manager);
					var result = QuarterlyConversationAccessor.GenerateQuarterlyConversation_Unsafe(s,perms, "TestGetSurveyContainerAbout", byAbout, DateTime.MaxValue, false);
					containerId = result.SurveyContainerId;
					tx.Commit();
					s.Flush();
				}
			}
			var about = SurveyAccessor.GetSurveyContainerAbout(org.Manager,empl, containerId);

			Assert.AreEqual(1, about.GetSurveys().Count());
			Assert.AreEqual(6, about.GetSurveys().First().GetSections().Count());
			{
				var rockItemContainers = about.GetSurveys().First().GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Rocks).GetItemContainers();
				Assert.AreEqual(5, rockItemContainers.Count());
				Assert.IsTrue(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -0"));
				Assert.IsTrue(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -1"));
				Assert.IsTrue(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -2"));
				Assert.IsFalse(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -3"));
				Assert.IsFalse(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -4"));
				Assert.IsFalse(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -5"));
				Assert.IsTrue(rockItemContainers.Any(x => x.GetItem().GetName() == "Rock -6"));
				Assert.IsTrue(rockItemContainers.Any(x => x.GetItem().GetName() == RockSection.RockCommentHeading));


			}
			{

				var roleItemContainers = about.GetSurveys().First().GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Roles).GetItemContainers();
				Assert.AreEqual(7, roleItemContainers.Count());
				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == "Role 0"));
				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == "Role 1"));
				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == "Role 2"));
				Assert.IsFalse(roleItemContainers.Any(x => x.GetItem().GetName() == "Role 3"));
				Assert.IsFalse(roleItemContainers.Any(x => x.GetItem().GetName() == "Role 4"));
				Assert.IsFalse(roleItemContainers.Any(x => x.GetItem().GetName() == "Role 5"));
				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == RoleSection.RoleCommentHeading));

				var a = roleItemContainers.Select(x => x.GetItem().GetName()).ToList();

				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == "Gets it"));
				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == "Wants it"));
				Assert.IsTrue(roleItemContainers.Any(x => x.GetItem().GetName() == "Capacity to do it"));
			}
			{
				var valueItemContainers = about.GetSurveys().First().GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Values).GetItemContainers();
				Assert.AreEqual(4, valueItemContainers.Count());
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetName() == "Value 0"));
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetName() == "Value 1"));
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetName() == "Value 2"));
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetHelp() == "Value Details 0"));
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetHelp() == "Value Details 1"));
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetHelp() == "Value Details 2"));
				Assert.IsTrue(valueItemContainers.Any(x => x.GetItem().GetName() == ValueSection.ValueCommentHeading));
			}
			//var j = 0;
		}
	}
}
