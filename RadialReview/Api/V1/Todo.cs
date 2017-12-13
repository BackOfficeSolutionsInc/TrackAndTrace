using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Api.V1 {
	[RoutePrefix("api/v1")]
	public class TodosController : BaseApiController {

		public class CreateTodoModel {
			/// <summary>
			/// To-do title
			/// </summary>
			[Required]
			public string title { get; set; }
            /// <summary>
            /// Optional notes 
            /// </summary>
            public string notes { get; set; }
            /// <summary>
            /// To-do due date (Default: 7 days)
            /// </summary>
            public DateTime? dueDate { get; set; }
		}
		/// <summary>
		/// Create a personal to-do
		/// </summary>
		/// <returns></returns>
		// PUT: api/Todo/5
		[Route("todo/create")]
		[HttpPost]
		public async Task<AngularTodo> CreateTodo([FromBody]CreateTodoModel body) {
			var duedate = body.dueDate ?? DateTime.UtcNow.AddDays(7);
			//var todo = new TodoModel() { Message = body.title, DueDate = duedate, TodoType = TodoType.Personal };
			//await TodoAccessor.CreateTodo(GetUser(), -2, todo);  // -2 for personal TODO

			var todoModel = TodoCreation.CreatePersonalTodo(body.title, body.notes, GetUser().Id, duedate);
			var todo = await TodoAccessor.CreateTodo(GetUser(), todoModel); 

			return new AngularTodo(todo);
		}

		/// <summary>
		/// Get a particular to-do
		/// </summary>
		/// <param name="TODO_ID">Todo ID</param>
		/// <returns></returns>
		// GET: api/Todo/5
		[Route("todo/{TODO_ID:long}")]
		[HttpGet]
		public AngularTodo Get(long TODO_ID) {
			return new AngularTodo(TodoAccessor.GetTodo(GetUser(), TODO_ID));
		}

		/// <summary>
		/// Get your to-dos
		/// </summary>
		/// <returns></returns>
		// GET: api/Todo/mine
		[Route("todo/users/mine")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetMineTodos() {
			// need to ask for method GetMyTodos() in TodoAccessor

			var range = new DateRange(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow);

			return TodoAccessor.GetMyTodosAndMilestones(GetUser(), GetUser().Id, true, range);//.Select(x => new AngularTodo(x));
		}
		/// <summary>
		/// Get to-dos for a user
		/// </summary>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
		// GET: api/Todo/mine
		[Route("todo/user/{USER_ID:long}")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetUserTodos(long USER_ID) {
			return TodoAccessor.GetTodosForUser(GetUser(), USER_ID, true);
		}
		public class UpdateTodoModel {
			/// <summary>
			/// To-do title
			/// </summary>
			public string title { get; set; }
			/// <summary>
			/// To-do due date
			/// </summary>
			public DateTime? dueDate { get; set; }
		}
		// PUT: api/Todo/5
		/// <summary>
		/// Update a to-do
		/// </summary>
		/// <param name="TODO_ID">Todo ID</param>
		/// <param name="body"></param>
		/// <returns></returns>
		[Route("todo/{TODO_ID:long}")]
		[HttpPut]
		public async Task EditTodo(long TODO_ID, [FromBody]UpdateTodoModel body) {
			//await L10Accessor.UpdateTodo(GetUser(), id, message, null, dueDate);
			await TodoAccessor.UpdateTodo(GetUser(), TODO_ID, body.title, body.dueDate);
		}

		/// <summary>
		/// Update the completion of a to-do
		/// </summary>
		/// <param name="TODO_ID">Todo ID</param>
		/// <param name="status">Is completed (Default: true)</param>
		/// <returns></returns>
		// PUT: api/Todo/5
		[Route("todo/{TODO_ID}/complete")]
		[HttpPost]
		//[HttpGet]
		public async Task<bool> MarkComplete(long TODO_ID,bool status=true) {
			//if (!completeTime.HasValue)
			var completeTime = DateTime.UtcNow;
			await TodoAccessor.CompleteTodo(GetUser(), TODO_ID, status);
			return true;
			//return new AngularTodo(TodoAccessor.MarkComplete(GetUser(), id, completeTime.Value));
		}


		//// GET: api/Todo/mine
		//[Route("todo/l10/{recurrenceId}")]
		//      [HttpGet]
		//      public IEnumerable<AngularTodo> GetRecurrenceTodos(long recurrenceId) {
		//          //await L10Accessor.CreateBlankRecurrence()
		//          return L10Accessor.GetAllTodosForRecurrence(GetUser(), recurrenceId, false).Select(x => new AngularTodo(x));
		//      }

	}
}