using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Audit;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RestSharp.Extensions;

namespace RadialReview.Utilities
{
	public class Audit
	{

		/*public static void Message(ISession s, UserOrganizationModel caller,LogType type, ILongIdentifiable about, string title, string message = null)
		{
			s.Save(LogModel.Create(caller, type, about, title));
		}*/

		public static void Log(ISession s, UserOrganizationModel caller)
		{
			try
			{
				var audit = new AuditModel();
				if (HttpContext.Current != null && HttpContext.Current.Request != null)
				{
					var r = HttpContext.Current.Request;
					r.InputStream.Seek(0, SeekOrigin.Begin);
					var oSR = new StreamReader(r.InputStream);
					var sContent = oSR.ReadToEnd();
					r.InputStream.Seek(0, SeekOrigin.Begin);

					audit.Method = r.HttpMethod;
					audit.Data = sContent;
					audit.Path = r.Url.LocalPath;
					audit.Query = r.Url.Query;
					audit.UserAgent = r.UserAgent;
				}

				audit.User = caller.User;
				audit.UserOrganization = caller;
				s.Save(audit);
			}
			catch (Exception)
			{

			}
		}

		public static void VtoLog(ISession s, UserOrganizationModel caller, long vtoId, string action, string notes = null)
		{
			try
			{
				var audit = new VtoAuditModel();
				if (HttpContext.Current != null && HttpContext.Current.Request != null)
				{
					var r = HttpContext.Current.Request;
					r.InputStream.Seek(0, SeekOrigin.Begin);
					var oSR = new StreamReader(r.InputStream);
					var sContent = oSR.ReadToEnd();
					r.InputStream.Seek(0, SeekOrigin.Begin);

					audit.Method = r.HttpMethod;
					audit.Data = sContent;
					audit.Path = r.Url.LocalPath;
					audit.Query = r.Url.Query;
					audit.UserAgent = r.UserAgent;
				}

				audit.Action = action;
				audit.Vto = s.Load<VtoModel>(vtoId);

				audit.User = caller.User;
				audit.UserOrganization = caller;
				audit.Notes = notes;
				s.Save(audit);
			}
			catch (Exception)
			{

			}
		}

		public static void L10Log(ISession s, UserOrganizationModel caller, long recurrenceId, string action, ForModel forModel, string notes=null)
		{
			try
			{
				var audit = new L10AuditModel();
				if (HttpContext.Current != null && HttpContext.Current.Request != null)
				{
					var r = HttpContext.Current.Request;
					r.InputStream.Seek(0, SeekOrigin.Begin);
					var oSR = new StreamReader(r.InputStream);
					var sContent = oSR.ReadToEnd();
					r.InputStream.Seek(0, SeekOrigin.Begin);

					audit.Method = r.HttpMethod;
					audit.Data = sContent;
					audit.Path = r.Url.LocalPath;
					audit.Query = r.Url.Query;
					audit.UserAgent = r.UserAgent;
				}

				audit.ForModel = forModel;
				audit.Action = action;
				audit.Recurrence = s.Load<L10Recurrence>(recurrenceId);



				audit.User = caller.User;
				audit.UserOrganization = caller;
				audit.Notes = notes;
				s.Save(audit);

				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
				var type = forModel.FriendlyType();
				var html = "<div><span class='log-action'>" + action + "</span><span class='log-notes'>" + notes + "</span></div>";
				meetingHub.addOrEditLogRow(type + "_" + forModel.ModelId, html, type);

			}
			catch (Exception)
			{

			}
		}

	}
}