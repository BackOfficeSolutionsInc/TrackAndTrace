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
		private void hugeTree(FullOrg org) {

			var L1 = OrgUtil.AddUserToOrg(org, org.E1BottomNode, "L1");
			OrgUtil.AddUserToOrg(org, L1, "L2");
			var L3 = OrgUtil.AddUserToOrg(org, L1, "L3");
			OrgUtil.AddUserToOrg(org, L1, "L4");
			OrgUtil.AddUserToOrg(org, L1, "L5");

			var L6 = OrgUtil.AddUserToOrg(org, L3, "L6");
			OrgUtil.AddUserToOrg(org, L6, "L7");
			OrgUtil.AddUserToOrg(org, L6, "L8a");
			OrgUtil.AddUserToOrg(org, L6, "L8b");
			OrgUtil.AddUserToOrg(org, L6, "L8c");
			OrgUtil.AddUserToOrg(org, L6, "L8d");
			OrgUtil.AddUserToOrg(org, L6, "L8e");
			OrgUtil.AddUserToOrg(org, L6, "L8f");
			OrgUtil.AddUserToOrg(org, L6, "L8g");
			var L9 = OrgUtil.AddUserToOrg(org, L6, "L9");
			var L10 = OrgUtil.AddUserToOrg(org, L6, "L10");


			OrgUtil.AddUserToOrg(org, L9, "L11");
			OrgUtil.AddUserToOrg(org, L9, "L12");
			OrgUtil.AddUserToOrg(org, L9, "L13");
			OrgUtil.AddUserToOrg(org, L9, "L14");
			OrgUtil.AddUserToOrg(org, L9, "L14a");
			OrgUtil.AddUserToOrg(org, L9, "L14b");
			OrgUtil.AddUserToOrg(org, L9, "L14c");
			OrgUtil.AddUserToOrg(org, L9, "L14d");

			OrgUtil.AddUserToOrg(org, L10, "L15");
			OrgUtil.AddUserToOrg(org, L10, "L16");
			OrgUtil.AddUserToOrg(org, L10, "L17a");
			OrgUtil.AddUserToOrg(org, L10, "L17b");
			OrgUtil.AddUserToOrg(org, L10, "L17c");
			OrgUtil.AddUserToOrg(org, L10, "L17d");
			OrgUtil.AddUserToOrg(org, L10, "L17e");
			OrgUtil.AddUserToOrg(org, L10, "L17f");
			OrgUtil.AddUserToOrg(org, L10, "L17g");
			OrgUtil.AddUserToOrg(org, L10, "L17h");
			OrgUtil.AddUserToOrg(org, L10, "L17i");
			OrgUtil.AddUserToOrg(org, L10, "L17j");
			OrgUtil.AddUserToOrg(org, L10, "L17k");
			OrgUtil.AddUserToOrg(org, L10, "L17l");
			OrgUtil.AddUserToOrg(org, L10, "L17m");
			OrgUtil.AddUserToOrg(org, L10, "L17n");

		}
		private void largeTree(FullOrg org) {

			var L1 = OrgUtil.AddUserToOrg(org, org.E1BottomNode, "L1");
			OrgUtil.AddUserToOrg(org, L1, "L2");
			var L3 = OrgUtil.AddUserToOrg(org, L1, "L3");
			OrgUtil.AddUserToOrg(org, L1, "L4");
			OrgUtil.AddUserToOrg(org, L1, "L5");

			var L6 = OrgUtil.AddUserToOrg(org, L3, "L6");
			OrgUtil.AddUserToOrg(org, L6, "L7");
			OrgUtil.AddUserToOrg(org, L6, "L8");
			var L9 = OrgUtil.AddUserToOrg(org, L6, "L9");
			var L10 = OrgUtil.AddUserToOrg(org, L6, "L10");


			OrgUtil.AddUserToOrg(org, L9, "L11");
			OrgUtil.AddUserToOrg(org, L9, "L12");
			OrgUtil.AddUserToOrg(org, L9, "L13");
			OrgUtil.AddUserToOrg(org, L9, "L14");

			OrgUtil.AddUserToOrg(org, L10, "L15");
			OrgUtil.AddUserToOrg(org, L10, "L16");
			OrgUtil.AddUserToOrg(org, L10, "L17");

		}
		#endregion

		[TestMethod]
		[TestCategory("PDF")]
		public void FullTreeDiagram() {
			var c = new Ctx();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);

			var ts = new TreeSettings() {compact = false};

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, ts);

			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram.pdf"));
		}

		[TestMethod]
		[TestCategory("PDF")]
		public void LargeTreeDiagram_single_compact() {

			var c = new Ctx();
			largeTree(c.Org);

			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);


			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, true, compactTS);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_single_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_single_compact.pdf"));

		}
		[TestMethod]
		[TestCategory("PDF")]
		public void HugeTreeDiagram_single_compact() {

			var c = new Ctx();
			hugeTree(c.Org);

			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);


			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, true, compactTS);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "HugeTreeDiagram_single_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "HugeTreeDiagram_single_compact.pdf"));

		}



		[TestMethod]
		[TestCategory("PDF")]
		public void LargeTreeDiagram_multi_compact() {

			var c = new Ctx();
			largeTree(c.Org);

			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);


			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, compactTS);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_multi_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_multi_compact.pdf"));

		}



		[TestMethod]
		[TestCategory("PDF")]
		public void LargeTreeDiagram_single_noncompact() {

			var c = new Ctx();
			largeTree(c.Org);

			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);


			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, true, noncompactTS);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_single_noncompact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_single_noncompact.pdf"));

		}


		[TestMethod]
		[TestCategory("PDF")]
		public void LargeTreeDiagram_multi_noncompact() {

			var c = new Ctx();
			largeTree(c.Org);

			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);


			var compactTS = new TreeSettings() { compact = true };
			var noncompactTS = new TreeSettings() { compact = false };
			

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, noncompactTS);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_multi_noncompact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_multi_noncompact.pdf"));
			

		}


		[TestMethod]
		[TestCategory("PDF")]
		public void FullTreeDiagram_SingleLevel() {
			var c = new Ctx();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);

			var noncompactTS = new TreeSettings() { compact = false };
			var pdfs = AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(root, 11, 8.5, false, noncompactTS);
			var merger = new DocumentMerger();
			merger.AddDocs(pdfs);
			var pdf = merger.Flatten("FullTreeDiagram_SingleLevel", true, true);


			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
		}


	}
}
