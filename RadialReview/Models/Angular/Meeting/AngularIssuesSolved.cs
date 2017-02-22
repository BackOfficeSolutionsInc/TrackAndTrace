using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Model.Enums;
using Newtonsoft.Json;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.DataType;

namespace RadialReview.Models.Angular.Meeting {	

	public class AngularIssuesSolved : AngularIssuesList {

		public AngularIssuesSolved(long recurrenceId) : base(recurrenceId) {
		}
		public AngularIssuesSolved() {
		}		
	}	
}