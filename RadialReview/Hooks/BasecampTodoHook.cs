using NHibernate;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Hooks {
    public class BasecampTodoHook : ITodoHook {
        public async Task CreateTodo(ISession s, TodoModel todo)
        {
            //if (todo.ForRecurrenceId!=null){
            //    var recurrenceType = ForModel.GetModelType<L10Recurrence>();
            //    var any = s.QueryOver<BasecampTodoCreds>()
            //            .Where(x => x.ForRGMId == todo.AccountableUserId && x.DeleteTime == null)
            //            .Where(x => x.AssociatedWith.ModelId == todo.ForRecurrenceId.Value && x.AssociatedWith.ModelType == recurrenceType)
            //            .List().ToList();

            //    foreach (var b in any) {
            //        await b.AddTodo(s, todo);
            //    }

            //}
            throw new NotImplementedException();
        }


        public async Task UpdateMessage(ISession s, TodoModel todo)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateCompletion(ISession s, TodoModel todo)
        {
            throw new NotImplementedException();
        }
    }
}