using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;

namespace RadialReview.Hooks {
	public class Outlook_TodoHook : ITodoHook {

		public Task CreateTodo(ISession s, TodoModel todo) {
			//s.QueryOver<
			throw new NotImplementedException();
		}

		public Task UpdateCompletion(ISession s, TodoModel todo) {
			throw new NotImplementedException();
		}

		public Task UpdateDueDate(ISession s, TodoModel todo) {
			throw new NotImplementedException();
		}

		public Task UpdateMessage(ISession s, TodoModel todo) {
			throw new NotImplementedException();
		}
	}


	public class OutlookTodoLink {
		public virtual long Id { get; set; }
		public virtual long TodoId { get; set; }
		//public virtual 
	}
}