using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;

namespace TractionTools.Tests.Accessors.Evals {
	[TestClass]
	public class IssueEvalTests {

		private void ConfirmOnce(List<AccRelationship> expected, AccRelationship relationship, ref List<AccRelationship> failed) {
			var found = expected.FirstOrDefault(x => x == relationship);
			if (found == null)
				failed.Add(relationship);
			else
				expected.Remove(found);

		}
		private void ConfirmAll(List<AccRelationship> expected, List<AccRelationship> found) {
			var rels = found.Select(x => x).ToList();
			var failed = new List<AccRelationship>();
			foreach (var rel in rels) {
				ConfirmOnce(expected, rel, ref failed);
			}
			string error = "\n";
			bool hasError = false;

			if (failed.Any()) {
				var builder = "";
				foreach (var f in failed)
					builder += "Unexpected relationship: " + f + "\n";

				Console.WriteLine(builder);
				hasError = true;
				error += ("Unexpected relationships " + failed.Count + ".\n");
			}

			if (expected.Any()) {
				var builder = "";
				foreach (var f in expected)
					builder += "Extra relationship: " + f + "\n";

				Console.WriteLine(builder);
				hasError = true;
				error += ("Expected contain " + expected.Count + " extra relationships.\n");
			}

			if (hasError) {
				Assert.Fail(error);
			}
		}


		[TestMethod]
		public void TestRelationships_AllMembersTeam() {
			var org = OrgUtil.CreateFullOrganization();

			var rels = ReviewAccessor.GetAllRelationships(org.Manager, org.AllMembersTeam.Id, ReviewParameters.AllTrue()).GetAll();

			var allExpected = new List<AccRelationship>() {
				new AccRelationship(new Reviewer(org.ManagerNode),new Reviewee(org.ManagerNode),AboutType.Self),
				new AccRelationship(new Reviewer(org.ManagerNode),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.ManagerNode),new Reviewee(org.EmployeeNode),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.ManagerNode),new Reviewee(org.MiddleNode),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.ManagerNode),new Reviewee(org.E1MiddleNode),AboutType.Subordinate),

				new AccRelationship(new Reviewer(org.EmployeeNode),new Reviewee(org.EmployeeNode),AboutType.Self),
				new AccRelationship(new Reviewer(org.EmployeeNode),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.EmployeeNode),new Reviewee(org.ManagerNode),AboutType.Manager),

				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.MiddleNode),AboutType.Self),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.ManagerNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.E1BottomNode),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.E2Node),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.E3Node),AboutType.Subordinate),

				new AccRelationship(new Reviewer(org.E1MiddleNode),new Reviewee(org.E1MiddleNode),AboutType.Self),
				new AccRelationship(new Reviewer(org.E1MiddleNode),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E1MiddleNode),new Reviewee(org.ManagerNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E1MiddleNode),new Reviewee(org.E4Node),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.E1MiddleNode),new Reviewee(org.E5Node),AboutType.Subordinate),

				new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.E1BottomNode),AboutType.Self),
			  //new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.Organization),AboutType.Organization), //Already added to E1MiddleNode
				new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.E3Node),AboutType.Peer),

				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.E2Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.E6Node),AboutType.Subordinate),

				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.E3Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.E1BottomNode),AboutType.Peer),

				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.E4Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.E1MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.E5Node),AboutType.Peer),

				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.E5Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.E1MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.E4Node),AboutType.Peer),
				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.E6Node),AboutType.Teammate),


				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E6Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E2Node),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E5Node),AboutType.Teammate),

				new AccRelationship(new Reviewer(org.E7),new Reviewee(org.E7),AboutType.Self),
				new AccRelationship(new Reviewer(org.E7),new Reviewee(org.Organization),AboutType.Organization),
			};

			ConfirmAll(allExpected, rels);
		}

		[TestMethod]
		public void TestRelationships_StandardTeam() {
			var org = OrgUtil.CreateFullOrganization();

			var rels = ReviewAccessor.GetAllRelationships(org.Manager, org.InterreviewTeam.Id, ReviewParameters.AllTrue()).GetAll();

			var allExpected = new List<AccRelationship>() {

				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.E5Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E5Node),new Reviewee(org.E6Node),AboutType.Teammate),


				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E6Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E5Node),AboutType.Teammate),
			};

			ConfirmAll(allExpected, rels);
		}


		[TestMethod]
		public void TestRelationships_Noninterreview() {
			var org = OrgUtil.CreateFullOrganization();

			var rels = ReviewAccessor.GetAllRelationships(org.Manager, org.NonreviewTeam.Id, ReviewParameters.AllTrue()).GetAll();

			var allExpected = new List<AccRelationship>() {
				

				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.E3Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.E4Node),AboutType.Teammate),

				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.E4Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E4Node),new Reviewee(org.E3Node),AboutType.Teammate),

			};

			ConfirmAll(allExpected, rels);
		}


		[TestMethod]
		public void TestRelationships_Subordinate() {
			var org = OrgUtil.CreateFullOrganization();

			var rels = ReviewAccessor.GetAllRelationships(org.Manager, org.MiddleSubordinatesTeam.Id, ReviewParameters.AllTrue()).GetAll();

			var allExpected = new List<AccRelationship>() {			
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.MiddleNode),AboutType.Self),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.E1BottomNode),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.E2Node),AboutType.Subordinate),
				new AccRelationship(new Reviewer(org.MiddleNode),new Reviewee(org.E3Node),AboutType.Subordinate),
				
				new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.E1BottomNode),AboutType.Self),
			    new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E1BottomNode),new Reviewee(org.E3Node),AboutType.Peer),

				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.E2Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E2Node),new Reviewee(org.MiddleNode),AboutType.Manager),

				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.E3Node),AboutType.Self),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.Organization),AboutType.Organization),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.MiddleNode),AboutType.Manager),
				new AccRelationship(new Reviewer(org.E3Node),new Reviewee(org.E1BottomNode),AboutType.Peer),
				
				//new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E6Node),AboutType.Self),
				//new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.Organization),AboutType.Organization),
				//new AccRelationship(new Reviewer(org.E6Node),new Reviewee(org.E2Node),AboutType.Manager),	
			};

			var allE1 = rels.Where(x => x.Reviewer.RGMId == org.E1.Id && x._RevieweeIsThe == AboutType.Self).ToList();



			ConfirmAll(allExpected, rels);
		}

	}
}
