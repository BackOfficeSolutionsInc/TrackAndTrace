using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Todo;

namespace RadialReview.Models.Angular.Todos
{
	public class AngularTodo : BaseAngular
	{
		public AngularTodo(TodoModel todo) : base(todo.Id)
		{
			Name = todo.Message;
			Details = todo.Details;
			DueDate = todo.DueDate;
			Owner = AngularUser.CreateUser(todo.AccountableUser);
			CompleteTime = todo.CompleteTime;
			CreateTime = todo.CreateTime;
			Complete = todo.CompleteTime != null;

			if (todo.ForRecurrenceId != null){
				var id = todo.ForModelId == -1 ? todo.Id : todo.ForModelId;
				var mod = (todo.ForModel == "Transcript") ? "Transcript" : "TodoModel";

				Link = "/L10/Timeline/" + todo.ForRecurrenceId + "#" + mod + "-" + id;
			}

		}

		public AngularTodo(){
			
		}

		public string Name { get; set; }
		public string Details { get; set; }
		public DateTime? DueDate { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? CompleteTime { get; set; }
		public DateTime? CreateTime { get; set; }
		public bool? Complete { get; set; }
		public string Link { get; set; }
	}
}