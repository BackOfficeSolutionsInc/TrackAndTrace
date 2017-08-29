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

namespace RadialReview.Api.V0 {
	[RoutePrefix("api/v0")]
	public class TodosController : BaseApiController {
		// PUT: api/Todo/5
		[Route("todo")]
		[HttpPost]
		public async Task<bool> CreateTodo([FromBody]string message, [FromBody]DateTime dueDate) {
			return await TodoAccessor.CreateTodo(GetUser(), -3, new TodoModel() { Message = message, DueDate = dueDate });  // -3 for personal TODO
		}

		// GET: api/Todo/5
		[Route("todo/{id}")]
		[HttpGet]
		public AngularTodo Get(long id) {
			return new AngularTodo(TodoAccessor.GetTodo(GetUser(), id));
		}

		// GET: api/Todo/mine
		[Route("todo/users/mine")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetMineTodos() {
			// need to ask for method GetMyTodos() in TodoAccessor
			return TodoAccessor.GetMyTodos(GetUser(), GetUser().Id, true);//.Select(x => new AngularTodo(x));
		}

		// GET: api/Todo/mine
		[Route("todo/user/{id}")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetUserTodos(long id) {
			return TodoAccessor.GetTodosForUser(GetUser(), id, true);//.Select(x => new AngularTodo(x));
		}

		// PUT: api/Todo/5
		[Route("todo/{id}")]
		[HttpPut]
		public async Task EditTodo(long id, [FromBody]string message, [FromBody]DateTime? dueDate) {
			await L10Accessor.UpdateTodo(GetUser(), id, message, null, dueDate);
		}

		// GET: api/Todo/mine
		[Route("todo/recurrence/{recurrenceId}")]
		[HttpGet]
		public IEnumerable<AngularTodo> GetRecurrenceTodos(long recurrenceId) {
			//await L10Accessor.CreateBlankRecurrence()
			return L10Accessor.GetAllTodosForRecurrence(GetUser(), recurrenceId, false).Select(x => new AngularTodo(x));
		}

		// PUT: api/Todo/5
		[Route("todo/complete/{id}")]
		[HttpPut]
		public AngularTodo MarkComplete(long id, [FromBody]DateTime? completeTime = null) {
			if (!completeTime.HasValue)
				completeTime = DateTime.UtcNow;
			return new AngularTodo(TodoAccessor.MarkComplete(GetUser(), id, completeTime.Value));
		}
	}
}