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

		private void Save(Document doc,string name) {
			PdfDocumentRenderer renderer = new PdfDocumentRenderer(true);
			renderer.Document = doc;			
			renderer.RenderDocument();			
			renderer.PdfDocument.Save(Path.Combine(GetCurrentPdfFolder(), name));
			renderer.PdfDocument.Save(Path.Combine(GetPdfFolder(), name));
		}

		#endregion

		[TestMethod]
		[TestCategory("PDF")]
		public async Task GenerateSurveyPdf() {
			var c = await Ctx.Build();

			var byAbouts = new[] {
				new ByAbout(c.Middle,c.Org.E2Node),
				new ByAbout(c.Org.E2Node,c.Org.E2Node),
			};

			ConstructSurveyEnv(c.Org);
			var surveyContainerId = SurveyAccessor.GenerateSurveyContainer(c.Middle, "Test", byAbouts);

			var surveyContainer = SurveyAccessor.GetAngularSurveyContainerAbout(c.Middle, c.Org.E2Node, surveyContainerId);

			var doc = SurveyPdfAccessor.CreateDoc(c.Manager, "Generate_Survey_Pdf");
			foreach (var survey in surveyContainer.GetSurveys()) {
				SurveyPdfAccessor.AppendSurveyAbout(doc, surveyContainer,null);
			}
			
			Save(doc, "GenerateSurveyPdf.pdf");
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
