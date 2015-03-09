using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Todo;

namespace RadialReview.Models.L10.VM
{
	public class TodoData
	{
		public string name { get; set; }
		public string imageurl { get; set; }
		public string accountableUser { get; set; }
		public long accountableUserId{ get; set; }
		public long todo { get; set; }
		public string message { get; set; }
		public string details { get; set; }
		public bool @checked { get; set; }
		public long createtime { get; set; }

		public static TodoData FromTodo(TodoModel todo)
		{
			return new TodoData()
			{
				@checked = todo.CompleteTime != null,
				createtime = todo.CreateTime.NotNull(x => x.ToJavascriptMilliseconds()),
				details = todo.Details,
				message = todo.Message,
				imageurl = todo.AccountableUser.ImageUrl(true,ImageSize._32),
				todo = todo.Id
			};
		}
	}
	public class TodoEdit
	{
		public long? ParentTodoId { get; set; }
		public long TodoId { get; set; }
		public int Order { get; set; }
	}

	public class TodoDataList
	{
		public TodoData[] todos { get; set; }

		public string connectionId { get; set; }

		public List<long> GetAllIds()
		{
			return idsRecurse(todos).Distinct().ToList();
		}
		private IEnumerable<long> idsRecurse(IEnumerable<TodoData> data)
		{
			if (data == null)
				return new List<long>();
			var output = data.Select(x => x.todo).ToList();
			/*foreach (var d in data)
			{
				output.AddRange(idsRecurse(d.children));
			}*/
			return output;
		}

		public List<TodoEdit> GetIssueEdits()
		{
			return issuesRecurse(null, todos).ToList();
		}

		private IEnumerable<TodoEdit> issuesRecurse(long? parentIssueId, IEnumerable<TodoData> data)
		{
			if (data == null)
				return new List<TodoEdit>();
			var output = data.Select((x, i) => new TodoEdit()
			{
				TodoId = x.todo,
				ParentTodoId = parentIssueId,
				Order = i
			}).ToList();
			/*foreach (var d in data)
			{
				output.AddRange(issuesRecurse(d.todo, d.children));
			}*/
			return output;
		}

	}
}