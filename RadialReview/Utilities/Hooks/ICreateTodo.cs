using NHibernate;
using RadialReview.Models.Todo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
    public interface ITodoHook : IHook {

        Task CreateTodo(ISession s, TodoModel todo);
        Task UpdateMessage(ISession s, TodoModel todo);
        Task UpdateCompletion(ISession s, TodoModel todo);
    }
}
