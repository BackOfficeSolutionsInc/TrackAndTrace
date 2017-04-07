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

namespace RadialReview.Api.V0
{
    [RoutePrefix("api/v0")]
    public class TodoController : BaseApiController
    {
        // PUT: api/Todo/5
        [Route("todo/{id}")]
        [HttpPut]
        public async Task<bool> CreateTodo(long recurrenceId, [FromBody]TodoModel todo)
        {
            return await TodoAccessor.CreateTodo(GetUser(), recurrenceId, todo);
        }

        // GET: api/Todo/5
        [Route("todo/{id}")]
        [HttpGet]
        public AngularTodo Get(long id)
        {
            return new AngularTodo(TodoAccessor.GetTodo(GetUser(), id));
        }

        // GET: api/Todo/mine
        [Route("todo/mine")]
        public IEnumerable<AngularTodo> GetMineTodos()
        {
            // need to ask for method GetMyTodos() in TodoAccessor
            return TodoAccessor.GetMyTodos(GetUser(), GetUser().Id, true).Select(x => new AngularTodo(x));
        }

        // GET: api/Todo/mine
        [Route("todo/user/{id}")]
        public IEnumerable<AngularTodo> GetUserTodos(long id)
        {
            return TodoAccessor.GetTodosForUser(GetUser(), id, true).Select(x => new AngularTodo(x));
        }

        // PUT: api/Todo/5
        [Route("todo/{id}")]
        [HttpPut]
        public void EditTodo(long id, [FromBody]string message, [FromBody]DateTime dueDate)
        {
            L10Accessor.UpdateTodo(GetUser(), id, message, null, dueDate);
        }

        // GET: api/Todo/mine
        [Route("todo/user/{recurrenceId}")]
        public IEnumerable<AngularTodo> GetRecurrenceTodos(long recurrenceId)
        {
            //L10Accessor.CreateBlankRecurrence()
            return L10Accessor.GetAllTodosForRecurrence(GetUser(), recurrenceId, false).Select(x => new AngularTodo(x));
        }

        // PUT: api/Todo/5
        [Route("todo/{id}")]
        [HttpPut]
        public AngularTodo MarkComplete(long id, [FromBody]DateTime completeTime)
        {
            return new AngularTodo(TodoAccessor.MarkComplete(GetUser(), id, completeTime));
        }
    }
}