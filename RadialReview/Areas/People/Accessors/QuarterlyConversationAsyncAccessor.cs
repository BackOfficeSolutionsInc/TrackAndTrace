using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Areas.People.Accessors {
	public class QuarterlyConversationAsyncAccessor {

		public static async Task<long> BeginGenerateQuarterlyConversation(UserOrganizationModel caller, string name, IEnumerable<ByAboutSurveyUserNode> byAbout, DateRange quarterRange, DateTime dueDate, bool sendEmails) {

			throw new Exception("not implemented");

		}
	}
}