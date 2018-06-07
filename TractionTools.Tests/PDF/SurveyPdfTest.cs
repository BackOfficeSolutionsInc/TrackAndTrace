using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors.PDF;
using RadialReview.Accessors;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Permissions;
using System.IO;
using static RadialReview.Accessors.PDF.D3.Layout;
using RadialReview.Accessors.PDF.JS;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using System.Collections.Generic;
using RadialReview.Models.Angular.Accountability;
using TractionTools.Tests.Utilities;
using RadialReview.Models;
using static RadialReview.Accessors.PDF.JS.Tree;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Areas.People.Engines.Surveys;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview;
using RadialReview.Areas.People.Accessors.PDF;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.Threading.Tasks;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class SurveyPdfTest : BasePermissionsTest {

		#region
		private void ConstructSurveyEnv(FullOrg org) {
			//Init DB
			DbCommit(s => {

				var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK);

				var lu = new[] {
					new { Value = "We love helping", Details = "Customers Coworkers & Family"},
					new { Value = "Optimizer" ,Details="Find the best current path"},
					new { Value = "Find a way to win", Details="Take a situation and turn it into a win-win-win"},
					};

				//Init values
				for (int i = 0; i < 3; i++) {
					s.Save(new CompanyValueModel() {
						OrganizationId = org.Id,
						CompanyValue = lu[i].Value,
						CompanyValueDetails = lu[i].Details,
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
		//private ISurveyContainer ConstructSurvey(FullOrg org) {
		//	ISurveyContainer container = null;
		//	//Construct Survey
		//	DbCommit(s => {
		//		var engine = new SurveyBuilderEngine(
		//			new QuarterlyConversationInitializer(ForModel.Create(org.Manager), "TestSurvey", org.Id),
		//			new SurveyBuilderEventsSaveStrategy(s)
		//		);

		//		var byAbout = new[] {
		//			new ByAbout(org.Manager,org.Manager),
		//			new ByAbout(org.Manager,org.Employee),
		//			new ByAbout(org.Employee,org.Employee),
		//			new ByAbout(org.Middle,org.Middle),
		//			new ByAbout(org.Middle,org.E1),
		//			new ByAbout(org.E1,org.E1),
		//			new ByAbout(org.Middle,org.E2),
		//			new ByAbout(org.E2,org.E2),
		//			new ByAbout(org.Middle,org.E3),
		//			new ByAbout(org.E3,org.E3),
		//			new ByAbout(org.E2,org.E6),
		//		};

		//		container = engine.BuildSurveyContainer(byAbout);
		//	});
		//	return container;
		//}


		#endregion

		string longText1 = @"Jean shorts activated charcoal four dollar toast vaporware tumeric. Gastropub kinfolk church-key, chartreuse viral narwhal post-ironic chambray locavore fashion 
axe polaroid offal messenger bag retro next level. YOLO etsy af listicle direct trade tousled, single-origin coffee swag poutine vinyl gochujang. Dreamcatcher VHS edison bulb lo-fi, helvetica
heirloom post-ironic artisan sustainable succulents +1 try-hard master cleanse neutra. Lyft before they sold out pop-up tbh meggings williamsburg farm-to-table ethical shaman artisan gentrify 
raw denim pickled. Roof party selvage yuccie kale chips actually authentic offal bicycle rights celiac man braid palo santo edison bulb mumblecore. Neutra bicycle rights prism chartreuse 
williamsburg bitters, mixtape hexagon photo booth hot chicken poutine. Poutine chambray lomo hot chicken put a bird on it gluten-free. Synth semiotics normcore kombucha next level cold-pressed
squid vape. Portland chia fixie raw denim paleo fingerstache banjo cornhole. Four dollar toast mixtape forage iceland retro selvage. Plaid food truck biodiesel chia four dollar toast cred hexagon
irony man bun tousled prism. Cloud bread coloring book shoreditch hot chicken 8-bit ennui farm-to-table.";
		string longText2 = @"Waistcoat cronut vaporware butcher, gluten-free yr iPhone kitsch YOLO lo-fi vexillologist. Dreamcatcher swag cold-pressed retro quinoa. Kitsch jean shorts drinking
vinegar, lo-fi locavore vexillologist jianbing cornhole skateboard kombucha four loko. Photo booth austin food truck, pok pok godard tousled literally. Semiotics kogi selvage edison bulb. Copper
mug disrupt wayfarers ethical cloud bread viral cornhole skateboard ";

		[TestMethod]
		[TestCategory("PDF")]
		public async Task GenerateSurveyPdf() {
			var c = await Ctx.Build();


			var mid = SurveyUserNode.Create(c.Org.MiddleNode);
			var e2 = SurveyUserNode.Create(c.Org.E2Node);

			DbCommit(s => {
				s.Save(mid);
				s.Save(e2);
			});

			var byAbouts = new[] {
				new ByAboutSurveyUserNode(mid,e2, AboutType.Subordinate),
				new ByAboutSurveyUserNode(e2,e2, AboutType.Self),
			};



			ConstructSurveyEnv(c.Org);
			long surveyContainerId = 0;
			DbCommit(s => {
				var result = QuarterlyConversationAccessor.GenerateQuarterlyConversation_Unsafe(s, PermissionsUtility.Create(s, c.Middle), "Test", byAbouts, new DateRange(), DateTime.MaxValue, false);
				surveyContainerId = result.SurveyContainerId;
			});



			{
				var survey = SurveyAccessor.GetSurvey(c.Middle, c.Middle, e2, surveyContainerId);
				var valuesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Values);
				var value1 = valuesSection.GetItemContainers().First();
				SurveyAccessor.UpdateAngularSurveyResponse(c.Middle, value1.GetResponse().Id, (string)value1.GetFormat().GetSetting<Dictionary<string, object>>("options").First().Key);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.E2, c.E2, e2, surveyContainerId);
				var valuesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Values);
				var value1 = valuesSection.GetItemContainers().Skip(2).First();
				SurveyAccessor.UpdateAngularSurveyResponse(c.E2, value1.GetResponse().Id, (string)value1.GetFormat().GetSetting<Dictionary<string, object>>("options").Last().Key);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.E2, c.E2, e2, surveyContainerId);
				var valuesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Values);
				var value1 = valuesSection.GetItemContainers().Last();
				SurveyAccessor.UpdateAngularSurveyResponse(c.E2, value1.GetResponse().Id, longText1);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.Middle, c.Middle, e2, surveyContainerId);
				var valuesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Values);
				var value1 = valuesSection.GetItemContainers().Last();
				SurveyAccessor.UpdateAngularSurveyResponse(c.Middle, value1.GetResponse().Id, longText2);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.Middle, c.Middle, e2, surveyContainerId);
				var rockSection = survey.GetSections().FirstOrDefault(x => x.GetSectionType() == "" + SurveySectionType.Rocks);
				Assert.IsTrue(rockSection == null);
				//var value1 = rockSection.GetItemContainers().Skip(1).First();
				//SurveyAccessor.UpdateAngularSurveyResponse(c.Middle, value1.GetResponse().Id, (string)value1.GetFormat().GetSetting<Dictionary<string, object>>("options").Last().Key);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.E2, c.E2, e2, surveyContainerId);
				var rockSection = survey.GetSections().FirstOrDefault(x => x.GetSectionType() == "" + SurveySectionType.Rocks);
				Assert.IsTrue(rockSection == null);
				//var value1 = rockSection.GetItemContainers().First();
				//SurveyAccessor.UpdateAngularSurveyResponse(c.E2, value1.GetResponse().Id, (string)value1.GetFormat().GetSetting<Dictionary<string, object>>("options").First().Key);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.E2, c.E2, e2, surveyContainerId);
				var valuesSection = survey.GetSections().FirstOrDefault(x => x.GetSectionType() == "" + SurveySectionType.Rocks);
				Assert.IsTrue(valuesSection == null);
				//Values??
				//var value1 = valuesSection.GetItemContainers().Last();
				//SurveyAccessor.UpdateAngularSurveyResponse(c.E2, value1.GetResponse().Id, longText1);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.E2, c.E2, e2, surveyContainerId);
				var rolesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Roles);
				var value1 = rolesSection.GetItemContainers().Where(x => x.HasResponse()).First();
				SurveyAccessor.UpdateAngularSurveyResponse(c.E2, value1.GetResponse().Id, (string)value1.GetFormat().GetSetting<Dictionary<string, object>>("options").First().Key);
			}
			{
				var survey = SurveyAccessor.GetSurvey(c.E2, c.E2, e2, surveyContainerId);
				var rolesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Roles);
				var value1 = rolesSection.GetItemContainers().Where(x => x.HasResponse()).Skip(1).First();
				SurveyAccessor.UpdateAngularSurveyResponse(c.E2, value1.GetResponse().Id, (string)value1.GetFormat().GetSetting<Dictionary<string, object>>("options").Last().Key);
			}


			//{
			//	var survey = SurveyAccessor.GetSurvey(c.Middle, c.Middle, e2, surveyContainerId);
			//	var valuesSection = survey.GetSections().First(x => x.GetSectionType() == "" + SurveySectionType.Values);
			//	var value1 = valuesSection.GetItemContainers().Last();
			//	SurveyAccessor.UpdateAngularSurveyResponse(c.Middle, value1.GetResponse().Id, longText2);
			//}


			var surveyContainer = SurveyAccessor.GetSurveyContainerAbout(c.Middle, e2, surveyContainerId);
			var doc = SurveyPdfAccessor.CreateDoc(c.Manager, "Generate_Survey_Pdf");
			var now = c.Middle.GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow);
			foreach (var survey in surveyContainer.GetSurveys()) {
				SurveyPdfAccessor.AppendSurveyAbout(doc, surveyContainer.GetName(), now, survey);
			}
			Save(doc, "GenerateSurveyPdf.pdf");

			var pa = QuarterlyConversationAccessor.GetPeopleAnalyzer(c.Middle, c.Middle.Id);

			//int a = 0;

		}

		//[TestMethod]
		//[TestCategory("PDF")]
		//public void FullTreeDiagram() {
		//	var c = await Ctx.Build();
		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);

		//	var ts = new TreeSettings() {compact = false};

		//	var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, ts);

		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram.pdf"));
		//}

		//[TestMethod]
		//[TestCategory("PDF")]
		//public void LargeTreeDiagram_single_compact() {

		//	var c = await Ctx.Build();
		//	largeTree(c.Org);

		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);


		//	var compactTS = new TreeSettings() { compact = true };
		//	var noncompactTS = new TreeSettings() { compact = false };

		//	var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, true, compactTS);
		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_single_compact.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_single_compact.pdf"));

		//}
		//[TestMethod]
		//[TestCategory("PDF")]
		//public void HugeTreeDiagram_single_compact() {

		//	var c = await Ctx.Build();
		//	hugeTree(c.Org);

		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);


		//	var compactTS = new TreeSettings() { compact = true };
		//	var noncompactTS = new TreeSettings() { compact = false };

		//	var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, true, compactTS);
		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "HugeTreeDiagram_single_compact.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "HugeTreeDiagram_single_compact.pdf"));

		//}



		//[TestMethod]
		//[TestCategory("PDF")]
		//public void LargeTreeDiagram_multi_compact() {

		//	var c = await Ctx.Build();
		//	largeTree(c.Org);

		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);


		//	var compactTS = new TreeSettings() { compact = true };
		//	var noncompactTS = new TreeSettings() { compact = false };
		//	var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, compactTS);
		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_multi_compact.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_multi_compact.pdf"));

		//}



		//[TestMethod]
		//[TestCategory("PDF")]
		//public void LargeTreeDiagram_single_noncompact() {

		//	var c = await Ctx.Build();
		//	largeTree(c.Org);

		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);


		//	var compactTS = new TreeSettings() { compact = true };
		//	var noncompactTS = new TreeSettings() { compact = false };

		//	var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, true, noncompactTS);
		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_single_noncompact.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_single_noncompact.pdf"));

		//}


		//[TestMethod]
		//[TestCategory("PDF")]
		//public void LargeTreeDiagram_multi_noncompact() {

		//	var c = await Ctx.Build();
		//	largeTree(c.Org);

		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);


		//	var compactTS = new TreeSettings() { compact = true };
		//	var noncompactTS = new TreeSettings() { compact = false };


		//	var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, noncompactTS);
		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_multi_noncompact.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_multi_noncompact.pdf"));


		//}


		//[TestMethod]
		//[TestCategory("PDF")]
		//public void FullTreeDiagram_SingleLevel() {
		//	var c = await Ctx.Build();
		//	var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

		//	var root = adjTree(tree);

		//	var noncompactTS = new TreeSettings() { compact = false };
		//	var pdfs = AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(root, 11, 8.5, false, noncompactTS);
		//	var merger = new DocumentMerger();
		//	merger.AddDocs(pdfs);
		//	var pdf = merger.Flatten("FullTreeDiagram_SingleLevel", true, true);


		//	pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
		//	pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
		//}


	}
}
