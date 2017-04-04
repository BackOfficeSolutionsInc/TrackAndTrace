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

namespace TractionTools.Tests.PDF {
	[TestClass]
	public class AccountabilityChartPdfTest : BasePermissionsTest {


		private AngularAccountabilityNode adjTree(AngularAccountabilityChart chart) {
			var root = chart.Root.children.ToList()[0];

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

		[TestMethod]
		public void FullTreeDiagram() {
			var c = new Ctx();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);

			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, false);

			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram.pdf"));
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

		[TestMethod]
		public void LargeTreeDiagram() {

			var c = new Ctx();
			largeTree(c.Org);

			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);

			//var merger = new DocumentMerger();
			//merger.AddDocs(pdfs);
			//var pdf = merger.Flatten("FullTreeDiagram_SingleLevel", true, true);


			var pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, true);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_compact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_compact.pdf"));

			pdf = AccountabilityChartPDF.GenerateAccountabilityChart(root, 11, 8.5, false, false);
			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "LargeTreeDiagram_noncompact.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "LargeTreeDiagram_noncompact.pdf"));
		}


		[TestMethod]
		public void FullTreeDiagram_SingleLevel() {
			var c = new Ctx();
			var tree = AccountabilityAccessor.GetTree(c.Manager, c.Org.Organization.AccountabilityChartId, expandAll: true);

			var root = adjTree(tree);

			var pdfs = AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(root, 11, 8.5, false, false);
			var merger = new DocumentMerger();
			merger.AddDocs(pdfs);
			var pdf = merger.Flatten("FullTreeDiagram_SingleLevel", true, true);


			pdf.Save(Path.Combine(GetCurrentPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
			pdf.Save(Path.Combine(GetPdfFolder(), "FullTreeDiagram_SingleLevel.pdf"));
		}


	}
}
