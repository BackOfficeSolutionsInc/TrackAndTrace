using System.Net;
using ImageResizer.Configuration;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Todo;
using Config = RadialReview.Utilities.Config;
using RadialReview.Models.Rocks;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RadialReview.Models.Angular.Todos {
    public class AngularTodo : BaseAngular {
        public AngularTodo(TodoModel todo) : base(todo.Id) {
            Name = todo.Message;
            DetailsUrl = Config.BaseUrl(null, "/Todo/Pad/" + todo.Id); //Config.NotesUrl() + "p/" + todo.PadId + "?showControls=true&showChat=false";
			_PadId = todo.PadId;
			DeleteTime = todo.DeleteTime;

			//Details = todo.Details;
			DueDate = todo.DueDate;
            Owner = AngularUser.CreateUser(todo.AccountableUser);
            CompleteTime = todo.CompleteTime;
            CreateTime = todo.CreateTime;
            Complete = todo.CompleteTime != null;
            TodoType = todo.TodoType;
            Ordering = todo.Ordering;

            Origin = todo.ForRecurrence.NotNull(x => x.Name);
            if (Origin == null && todo.TodoType == Todo.TodoType.Personal) {
                Origin = "Individual";
            }

            if (todo.ForRecurrenceId != null) {
                var id = todo.ForModelId == -1 ? todo.Id : todo.ForModelId;
                var mod = (todo.ForModel == "Transcript") ? "Transcript" : "TodoModel";

                Link = "/L10/Timeline/" + todo.ForRecurrenceId + "#" + mod + "-" + id;
            }
        }

        public AngularTodo(long Id) : base(Id) {
        }
        public AngularTodo(Milestone milestone, UserOrganizationModel owner, string origin = null) : base(-milestone.Id) {
            Name = milestone.Name;
            //DetailsUrl = Config.NotesUrl() + "p/" + todo.PadId + "?showControls=true&showChat=false";

            //Details = todo.Details;
            DueDate = milestone.DueDate;
            Owner = AngularUser.CreateUser(owner);
            CompleteTime = milestone.CompleteTime;
            CreateTime = milestone.CreateTime;
            Complete = milestone.Status == MilestoneStatus.Done;
            TodoType = Todo.TodoType.Milestone;
            Ordering = -10;

            Origin = origin ?? "Milestone";

        }

        public AngularTodo() {
        }

        public string Name { get; set; }
        //public string Details { get; set; }
        public string DetailsUrl { get; set; }
        public string Origin { get; set; }
        public DateTime? DueDate { get; set; }
        public AngularUser Owner { get; set; }
        public DateTime? CompleteTime { get; set; }
		public DateTime? DeleteTime { get; set; }
		public DateTime? CreateTime { get; set; }
        public bool? Complete { get; set; }

        [IgnoreDataMember]
        public string Link { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TodoType? TodoType { get; set; }
        public long Ordering { get; set; }
		private string _PadId { get; set; }

		public string GetPadId() {
			return _PadId;
		}
	}
}