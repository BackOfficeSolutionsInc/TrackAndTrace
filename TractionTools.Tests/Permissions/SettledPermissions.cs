using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using RadialReview.Utilities;
using static RadialReview.Models.PermItem;
using RadialReview.Exceptions;
using RadialReview.Models.Todo;
using System.Collections.Generic;
using RadialReview.Models;
using RadialReview.Controllers;
using RadialReview.Models.Askables;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class SettledPermissions : BasePermissionsTest {
		#region Blank 1

		[TestMethod]
		[TestCategory("Unset")]
		public void EditCompanyPayment() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditGroup() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewGroup() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditApplication() { 
			Assert.Fail("Unimplemented");
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ViewApplication() { 
			Assert.Fail("Unimplemented");			
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditIndustry() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task ViewIndustry() { 
			Assert.Fail("Unimplemented");			
		}

#endregion
		[TestMethod]
		[TestCategory("Unset")]
		public async Task EditQuestion() { 
			Assert.Fail("Unimplemented");
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void PairCategoryToQuestion() {
			Assert.Fail("Unimplemented");
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void ViewQuestion() { 
			Assert.Fail("Unimplemented");
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditQuestionForUser() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewCategory() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditOrganizationQuestions() { 
			Assert.Fail("Unimplemented");
		}

		#region blank 2
		[TestMethod]
		[TestCategory("Unset")]
		public void EditUserDetails() { 
			Assert.Fail("Unimplemented");
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditOrigin() { 
			Assert.Fail("Unimplemented");
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void EditOrigin_TypeId() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewOrigin() { 
			Assert.Fail("Unimplemented");
		}



		[TestMethod]
		[TestCategory("Unset")]
		public void ViewTeam() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditTeam() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void IssueForTeam() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ManagingTeam() {
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void AdminReviewContainer() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task EditReview() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task ViewRGM() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewReviews() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewReview() { 
			Assert.Fail("Unimplemented");
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ManageUserReview() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ManageUserReview_Answer() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditResponsibility() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CreateTemplates() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditTodo() {
			Assert.Fail("Unimplemented");
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ViewTemplate() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditTemplate() { 
			Assert.Fail("Unimplemented");
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void ViewPrereview() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditUserScorecard() { 
			Assert.Fail("Unimplemented");	
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewOrganizationScorecard() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditAttach() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewMeasurable() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditMeasurable() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task EditScore() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task CanViewUserMeasurables() { 
			Assert.Fail("Unimplemented");
		}		

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewHeadline() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CanViewUserRocks() { 
			Assert.Fail("Unimplemented");
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void ViewSurveyContainer() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CreateSurvey() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditSurvey() { 
			Assert.Fail("Unimplemented");
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void EditPermissionOverride() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditForModel() { 
			Assert.Fail("Unimplemented");
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewForModel() { 
			Assert.Fail("Unimplemented");
		}
		#endregion
	}
}
