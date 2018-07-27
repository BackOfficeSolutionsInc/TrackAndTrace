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
using System.Threading.Tasks;
using RadialReview.Utilities;
using RadialReview.Utilities.Pdf;
using PdfSharp.Drawing;

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class AccountabilityChartPdfTest : BasePermissionsTest {

		#region helpers
		private void nameDive(AngularAccountabilityNode x,ref int index) {
			x.Name = x.User.Name;
			index += 1;
			if (x.children != null)
				foreach (var c in x.children) {
					nameDive(c,ref index);
				}
		}

		private AngularAccountabilityNode adjTree(AngularAccountabilityChart chart) {
			var root = chart.Root.children.ToList()[0];

			var index = 0;
			nameDive(root, ref index);


			root.Name = "Montgomery Madison-Wolstenholme";
			root.Group.Position = new AngularPosition() { Name = "Chairman and Chief Executive Officer" };
			var a = root.Group.RoleGroups = new List<AngularRoleGroup>() {
				new AngularRoleGroup() {
					Roles = new List<AngularRole>() {
						new AngularRole() {Name = "LMA" },
						new AngularRole() {Name = "Mergers and Acquisitions" },
						new AngularRole() {Name = "Financial Projections" },
						new AngularRole() {Name = "Culture" },
						new AngularRole() {Name = "Determine long and short term vision" },
					}
				}
			};
			var sub = root.children.ToList()[1];
			sub.Name = "Jimmy John";
			sub.Group.Position = new AngularPosition() { Name = "Operations" };
			sub.Group.RoleGroups = new List<AngularRoleGroup>() {
				new AngularRoleGroup() {
					Roles = new List<AngularRole>() {
						new AngularRole() {Name = "LMA" },
						new AngularRole() {Name = "Project manager" },
						new AngularRole() {Name = "Lead team" },
						new AngularRole() {Name = "Culture" },
						new AngularRole() {Name = "Profit and loss for the department" },
					}
				}
			};
			return root;
		}
		private async Task hugeTree(FullOrg org) {

			var L1 = await OrgUtil.AddUserToOrg(org, org.E1BottomNode, "L1");
			await OrgUtil.AddUserToOrg(org, L1, "L2");
			var L3 = await OrgUtil.AddUserToOrg(org, L1, "L3");
			await OrgUtil.AddUserToOrg(org, L1, "L4");
			await OrgUtil.AddUserToOrg(org, L1, "L5");

			var L6 = await OrgUtil.AddUserToOrg(org, L3, "L6");
			await OrgUtil.AddUserToOrg(org, L6, "L7");
			await OrgUtil.AddUserToOrg(org, L6, "L8a");
			await OrgUtil.AddUserToOrg(org, L6, "L8b");
			await OrgUtil.AddUserToOrg(org, L6, "L8c");
			await OrgUtil.AddUserToOrg(org, L6, "L8d");
			await OrgUtil.AddUserToOrg(org, L6, "L8e");
			await OrgUtil.AddUserToOrg(org, L6, "L8f");
			await OrgUtil.AddUserToOrg(org, L6, "L8g");
			var L9 = await OrgUtil.AddUserToOrg(org, L6, "L9");
			var L10 = await  OrgUtil.AddUserToOrg(org, L6, "L10");

			 
			await OrgUtil.AddUserToOrg(org, L9, "L11");
			await OrgUtil.AddUserToOrg(org, L9, "L12");
			await OrgUtil.AddUserToOrg(org, L9, "L13");
			await OrgUtil.AddUserToOrg(org, L9, "L14");
			await OrgUtil.AddUserToOrg(org, L9, "L14a");
			await OrgUtil.AddUserToOrg(org, L9, "L14b");
			await OrgUtil.AddUserToOrg(org, L9, "L14c");
			await OrgUtil.AddUserToOrg(org, L9, "L14d");
			 
			await OrgUtil.AddUserToOrg(org, L10, "L15");
			await OrgUtil.AddUserToOrg(org, L10, "L16");
			await OrgUtil.AddUserToOrg(org, L10, "L17a");
			await OrgUtil.AddUserToOrg(org, L10, "L17b");
			await OrgUtil.AddUserToOrg(org, L10, "L17c");
			await OrgUtil.AddUserToOrg(org, L10, "L17d");
			await OrgUtil.AddUserToOrg(org, L10, "L17e");
			await OrgUtil.AddUserToOrg(org, L10, "L17f");
			await OrgUtil.AddUserToOrg(org, L10, "L17g");
			await OrgUtil.AddUserToOrg(org, L10, "L17h");
			await OrgUtil.AddUserToOrg(org, L10, "L17i");
			await OrgUtil.AddUserToOrg(org, L10, "L17j");
			await OrgUtil.AddUserToOrg(org, L10, "L17k");
			await OrgUtil.AddUserToOrg(org, L10, "L17l");
			await OrgUtil.AddUserToOrg(org, L10, "L17m");
			await OrgUtil.AddUserToOrg(org, L10, "L17n");

		}
		private async Task largeTree(FullOrg org) {

			var L1 = await OrgUtil.AddUserToOrg(org, org.E1BottomNode, "L1");
			await OrgUtil.AddUserToOrg(org, L1, "L2");
			var L3 = await OrgUtil.AddUserToOrg(org, L1, "L3");
			await OrgUtil.AddUserToOrg(org, L1, "L4");
			await OrgUtil.AddUserToOrg(org, L1, "L5");

			var L6 = await OrgUtil.AddUserToOrg(org, L3, "L6");
			await OrgUtil.AddUserToOrg(org, L6, "L7");
			await OrgUtil.AddUserToOrg(org, L6, "L8");
			var L9 = await OrgUtil.AddUserToOrg(org, L6, "L9");
			var L10 = await OrgUtil.AddUserToOrg(org, L6, "L10");


			await OrgUtil.AddUserToOrg(org, L9, "L11");
			await OrgUtil.AddUserToOrg(org, L9, "L12");
			await OrgUtil.AddUserToOrg(org, L9, "L13");
			await OrgUtil.AddUserToOrg(org, L9, "L14");
			 
			await OrgUtil.AddUserToOrg(org, L10, "L15");
			await OrgUtil.AddUserToOrg(org, L10, "L16");
			await OrgUtil.AddUserToOrg(org, L10, "L17");

		}

		private async Task diffSizeTree(FullOrg org) {


			var L1 =await OrgUtil.AddUserToOrg(org, org.E1BottomNode, "L1");
			var LL =await OrgUtil.AddUserToOrg(org, L1, "LL");
					 
			var L2 =await OrgUtil.AddUserToOrg(org, LL, "L2");
			var L3 =await OrgUtil.AddUserToOrg(org, LL, "L3");
			var L4 =await OrgUtil.AddUserToOrg(org, LL, "L4");
			var L5 =await OrgUtil.AddUserToOrg(org, LL, "L5");
			var L6 =await OrgUtil.AddUserToOrg(org, LL, "L6");
			var L7 =await OrgUtil.AddUserToOrg(org, LL, "L7");
			var L8 =await OrgUtil.AddUserToOrg(org, LL, "L8");
			var L9 =await OrgUtil.AddUserToOrg(org, LL, "L9");
			var L16= await OrgUtil.AddUserToOrg(org, LL, "L12");


			await OrgUtil.AddUserToOrg(org, L2, "L2a");
			await OrgUtil.AddUserToOrg(org, L2, "L2b");
			 
			await OrgUtil.AddUserToOrg(org, L3, "L3");
			await OrgUtil.AddUserToOrg(org, L3, "L3");
			await OrgUtil.AddUserToOrg(org, L3, "L3");
			 
			await OrgUtil.AddUserToOrg(org, L4, "L4a");
			await OrgUtil.AddUserToOrg(org, L4, "L4b");
			await OrgUtil.AddUserToOrg(org, L4, "L4c");
			await OrgUtil.AddUserToOrg(org, L4, "L4d");
			 
			await OrgUtil.AddUserToOrg(org, L5, "L5a");
			await OrgUtil.AddUserToOrg(org, L5, "L5b");
			await OrgUtil.AddUserToOrg(org, L5, "L5c");
			await OrgUtil.AddUserToOrg(org, L5, "L5d");
			await OrgUtil.AddUserToOrg(org, L5, "L5e");
			 
			await OrgUtil.AddUserToOrg(org, L6, "L6a");
			await OrgUtil.AddUserToOrg(org, L6, "L6b");
			await OrgUtil.AddUserToOrg(org, L6, "L6c");
			await OrgUtil.AddUserToOrg(org, L6, "L6d");
			await OrgUtil.AddUserToOrg(org, L6, "L6e");
			await OrgUtil.AddUserToOrg(org, L6, "L6f");
			 
			await OrgUtil.AddUserToOrg(org, L7, "L7a");
			await OrgUtil.AddUserToOrg(org, L7, "L7b");
			await OrgUtil.AddUserToOrg(org, L7, "L7c");
			await OrgUtil.AddUserToOrg(org, L7, "L7d");
			await OrgUtil.AddUserToOrg(org, L7, "L7e");
			await OrgUtil.AddUserToOrg(org, L7, "L7f");
			await OrgUtil.AddUserToOrg(org, L7, "L7g");
			 
			await OrgUtil.AddUserToOrg(org, L8, "L8a");
			await OrgUtil.AddUserToOrg(org, L8, "L8b");
			await OrgUtil.AddUserToOrg(org, L8, "L8c");
			await OrgUtil.AddUserToOrg(org, L8, "L8d");
			await OrgUtil.AddUserToOrg(org, L8, "L8e");
			await OrgUtil.AddUserToOrg(org, L8, "L8f");
			await OrgUtil.AddUserToOrg(org, L8, "L8g");
			await OrgUtil.AddUserToOrg(org, L8, "L8h");
			 
			await OrgUtil.AddUserToOrg(org, L9, "L9a");
			await OrgUtil.AddUserToOrg(org, L9, "L9b");
			await OrgUtil.AddUserToOrg(org, L9, "L9c");
			await OrgUtil.AddUserToOrg(org, L9, "L9d");
			await OrgUtil.AddUserToOrg(org, L9, "L9e");
			await OrgUtil.AddUserToOrg(org, L9, "L9f");
			await OrgUtil.AddUserToOrg(org, L9, "L9g");
			await OrgUtil.AddUserToOrg(org, L9, "L9h");
			await OrgUtil.AddUserToOrg(org, L9, "L9i");
			 
			await OrgUtil.AddUserToOrg(org, L16, "L16a");
			await OrgUtil.AddUserToOrg(org, L16, "L16b");
			await OrgUtil.AddUserToOrg(org, L16, "L16c");
			await OrgUtil.AddUserToOrg(org, L16, "L16d");
			await OrgUtil.AddUserToOrg(org, L16, "L16e");
			await OrgUtil.AddUserToOrg(org, L16, "L16f");
			await OrgUtil.AddUserToOrg(org, L16, "L16g");
			await OrgUtil.AddUserToOrg(org, L16, "L16h");
			await OrgUtil.AddUserToOrg(org, L16, "L16i");
			await OrgUtil.AddUserToOrg(org, L16, "L16j");
			await OrgUtil.AddUserToOrg(org, L16, "L16k");
			await OrgUtil.AddUserToOrg(org, L16, "L16l");
			await OrgUtil.AddUserToOrg(org, L16, "L16m");
			await OrgUtil.AddUserToOrg(org, L16, "L16n");

		}
		#endregion


		[TestMethod]
		[TestCategory("PDF")]
		public async Task DiffTreeDiagram() {
			var c = await Ctx.Build();
			MockHttpContext();
			await diffSizeTree(c.Org);
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var ts = new TreeSettings() { compact = true };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(44), XUnit.FromInch(11), new AccountabilityChartPDF.AccountabilityChartSettings(), true, ts).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "DiffTreeDiagram.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "DiffTreeDiagram.pdf"));
		}

		[TestMethod]
		[TestCategory("PDF")]
		public async Task FullTreeDiagram() {
			var c = await Ctx.Build();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var ts = new TreeSettings() {compact = false};
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), false, ts).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram.pdf"));
		}

		[TestMethod]
		[TestCategory("PDF")]
		public async Task LargeTreeDiagram_single_compact() {
			var c = await Ctx.Build();
			MockHttpContext();
			await largeTree(c.Org);
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), true, compactTS).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_single_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_single_compact.pdf"));

		}
		[TestMethod]
		[TestCategory("PDF")]
		public async Task HugeTreeDiagram_single_compact() {
			var c = await Ctx.Build();
			MockHttpContext();
			await hugeTree(c.Org);
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), true, compactTS).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "HugeTreeDiagram_single_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "HugeTreeDiagram_single_compact.pdf"));
		}



		[TestMethod]
		[TestCategory("PDF")]
		public async Task LargeTreeDiagram_multi_compact() {
			var c = await Ctx.Build();
			MockHttpContext();
			await largeTree(c.Org);
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), false, compactTS).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_multi_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_multi_compact.pdf"));
		}



		[TestMethod]
		[TestCategory("PDF")]
		public async Task LargeTreeDiagram_single_noncompact() {
			var c = await Ctx.Build();
			MockHttpContext();
			await largeTree(c.Org);
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), true, noncompactTS).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_single_noncompact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_single_noncompact.pdf"));

		}


		[TestMethod]
		[TestCategory("PDF")]
		public async Task LargeTreeDiagram_multi_noncompact() {
			var c = await Ctx.Build();
			MockHttpContext();
			await largeTree(c.Org);
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), false, noncompactTS).Document;
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_multi_noncompact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_multi_noncompact.pdf"));
		}


		[TestMethod]
		[TestCategory("PDF")]
		public async Task FullTreeDiagram_SingleLevel() {
			var c = await Ctx.Build();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);
			var root = adjTree(tree);
			var noncompactTS = new TreeSettings() { compact = false };
			var pdfs = AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(root, XUnit.FromInch(11), XUnit.FromInch(8.5), new AccountabilityChartPDF.AccountabilityChartSettings(), false, noncompactTS).Select(x=>x.Document);
			var merger = new DocumentMerger();
			merger.AddDocs(pdfs);
			var pdf = merger.Flatten("FullTreeDiagram_SingleLevel", true, true);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
		}
	}
}
