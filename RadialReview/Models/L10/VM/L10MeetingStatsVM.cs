using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Todo;

namespace RadialReview.Models.L10.VM {
	public class L10MeetingStatsVM {
		public List<L10Meeting> AllMeetings { get; set; }

		public int IssuesSolved { get; set; }
		public int TodoCompleted { get; set; }
		public decimal TodoCompletionPercentage { get; set; }
		public long RecurrenceId { get; set; }
		public List<TodoModel> TodosCreated { get; set; }

		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public double AverageRating { get; set; }

		public int Version { get; set; }


		public List<TodoModel> AllTodos { get; set; }
	}
}
