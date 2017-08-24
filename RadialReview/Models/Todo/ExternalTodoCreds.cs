using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using Newtonsoft.Json.Linq;
using NHibernate;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using TrelloNet;
using RadialReview.Accessors;

namespace RadialReview.Models.Todo
{

	public abstract class AbstractTodoCreds : ILongIdentifiable, IHistorical
	{

		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual long ForRGMId { get; set; }
		public virtual ResponsibilityGroupModel ForRGM { get; set; }
		public virtual ForModel AssociatedWith { get; set; }
		public virtual String AccountName { get; set; }

		public abstract Task<bool> AddTodo(ISession s, TodoModel details);
		public abstract String GetServiceName();

		
		public AbstractTodoCreds()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class AbstractTodoCredsMap : ClassMap<AbstractTodoCreds>
		{
			public AbstractTodoCredsMap()
			{
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.AccountName);
				Map(x => x.ForRGMId).Column("ForRGMId");
				References(x => x.ForRGM).Column("ForRGMId").ReadOnly();
				Map(x => x.CreatedBy);

				Component(x => x.AssociatedWith).ColumnPrefix("AssociatedWith_");
			}
		}
	}

	public class TrelloTodoCreds : AbstractTodoCreds
	{

		public virtual string Token { get; set; }
		//public virtual string BoardId { get; set; }
		public virtual string ListId { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public override async Task<bool> AddTodo(ISession s, TodoModel todo)
        {
			var trello = new Trello(Config.GetTrelloKey());
			trello.Authorize(Token);
			var list = trello.Lists.WithId(ListId);
			var card = new NewCard(todo.Message, list);
			card.Desc = todo.Details;
			trello.Cards.Add(card);
			return true;
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		public override String GetServiceName()
		{
			return "Trello";
		}

		public class TrelloTodoCredsMap : SubclassMap<TrelloTodoCreds>
		{
			public TrelloTodoCredsMap()
			{
				Map(x => x.Token);
				//Map(x => x.BoardId);
				Map(x => x.ListId);
			}
		}
	}
	public class BasecampTodoCreds : AbstractTodoCreds
	{
		public virtual string UID { get; set; }

		public virtual long ApiId { get; set; }
		public virtual string Token { get; set; }
		public virtual string RefreshToken { get; set; }
		public virtual string Expires { get; set; }
		//public virtual string BoardId { get; set; }
		public virtual string ListId { get; set; }
		public virtual string ProjectId { get; set; }
		public virtual string BasecampAssigneeId { get; set; }
		public virtual string ApiUrl { get; set; }

        public override async Task<bool> AddTodo(ISession s, TodoModel todo)
        {
            var accessToken = Token;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(ApiUrl + "projects/" + ProjectId + "/todolists/" + ListId + "/todos.json");

            httpWebRequest.Headers.Add("Authorization", "Bearer " + accessToken);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.UserAgent = Config.Basecamp.GetUserAgent();

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream())) {
                var json = @"{
				  ""content"": """ + todo.Message.EscapeJSONString() + @""",
				  ""due_at"": """ + string.Concat(todo.DueDate.ToString("s"), "Z") + @""",
				  ""assignee"": {
					""id"": " + BasecampAssigneeId + @",
					""type"": ""Person""
				  }
				}";
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            string newId = null;
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                var result = streamReader.ReadToEnd();

                newId = (string)JObject.Parse(result)["id"];
            }

            if (!String.IsNullOrWhiteSpace(todo.Details) && !String.IsNullOrWhiteSpace(newId)) {

                var httpWebRequest2 = (HttpWebRequest)WebRequest.Create(ApiUrl + "projects/" + ProjectId + "/todos/" + newId + "/comments.json");
                httpWebRequest2.Headers.Add("Authorization", "Bearer " + accessToken);
                httpWebRequest2.ContentType = "application/json";
                httpWebRequest2.Method = "POST";
                httpWebRequest2.UserAgent = Config.Basecamp.GetUserAgent();

                using (var streamWriter = new StreamWriter(httpWebRequest2.GetRequestStream())) {

                    var padDetails = await PadAccessor.GetText(todo.PadId);

                    var json = @"{""content"": """ + padDetails.EscapeJSONString()/*todo.Details*/ + @""",""subject"": ""Details""}";
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse2 = (HttpWebResponse)httpWebRequest2.GetResponse();
                using (var streamReader = new StreamReader(httpResponse2.GetResponseStream())) {
                    var result = streamReader.ReadToEnd();
                    //var a = true;
                }
            }
            return true;


        }

		public override String GetServiceName()
		{
			return "Basecamp";
		}

		public class BasecampTodoCredsMap : SubclassMap<BasecampTodoCreds>
		{
			public BasecampTodoCredsMap()
			{
				Map(x => x.UID);
				Map(x => x.Expires);
				Map(x => x.ApiId);
				Map(x => x.BasecampAssigneeId);
				Map(x => x.Token);
				Map(x => x.RefreshToken);
				Map(x => x.ApiUrl);
				Map(x => x.ListId);
				Map(x => x.ProjectId);
			}
		}

	}
}