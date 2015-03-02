using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon.IdentityManagement.Model;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Utilities;

namespace RadialReview.Hubs
{
	public class MeetingHub : BaseHub
	{
		/*public void Hello()
		{
			Clients.All.hello();
		}

		public string Initialize(string connectionId)
		{
			var user =GetUser();
			var meeting = _MeetingAccessor.CreateMeeting(user, connectionId);
			/*
			if (userMeeting.ConnectionIds == null)
				userMeeting.ConnectionIds = new List<string>();
			userMeeting.ConnectionIds.Add(Context.ConnectionId);
			*
			Groups.Add(connectionId, meeting.Id.ToString());
			return meeting.Id.ToString();
		}*/

		public class UserMeetingModel
		{
			public long UserId { get; set; }
			public long StartSelection { get; set; }
			public long EndSelection { get; set; }
			public string FocusId { get; set; }

			public UserMeetingModel(L10Meeting.L10Meeting_Connection conn)
			{
				UserId = conn.User.Id;
				StartSelection = conn.StartSelection;
				EndSelection = conn.EndSelection;
				FocusId = conn.FocusId;

			}
		}

		public UserMeetingModel Join(long meetingId, string connectionId)
		{
			var meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), meetingId);
			var conn= L10Accessor.JoinL10Meeting(GetUser(), meetingId, connectionId);
			return new UserMeetingModel(conn);
		}

		public ResultObject UpdateUserFocus(long meetingId, string domId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var us = GetUserStatus(s, meetingId);

					us.FocusId = domId;
					tx.Commit();
					s.Flush();
					Clients.OthersInGroup(GenerateMeetingGroupId(us.L10Meeting))
						.UpdateUserFocus(domId, us.User.Id);
					return ResultObject.SilentSuccess();
				}
			}
		}

		public ResultObject UpdateTextContents(long meetingId, string domId, string contents)
		{
			using(var s = HibernateSession.GetCurrentSession())
			{
				using(var tx=s.BeginTransaction()){
					var us = GetUserStatus(s,meetingId);
					Clients.OthersInGroup(GenerateMeetingGroupId(us.L10Meeting))
						.updateTextContents(domId,contents);
					return ResultObject.SilentSuccess();
				}
			}
		}

		
		private L10Meeting.L10Meeting_Connection GetUserStatus(ISession s,long meetingId)
		{
			return L10Accessor.GetConnection(s,GetUser(), meetingId);
		}


		public static string GenerateMeetingGroupId(long recurrenceId)
		{
			if (recurrenceId == 0)
				throw new Exception();
			return "L10MeetingRecurrence_" + recurrenceId;
		}
		public static string GenerateMeetingGroupId(L10Meeting meeting)
		{
			var id = meeting.L10Recurrence.NotNull(x => x.Id);
			if (id == 0)
				id = meeting.L10RecurrenceId;
			return GenerateMeetingGroupId(id);
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			//L10Accessor.UpdatePage(GetUser(),GetUser().Id,);

			return base.OnDisconnected(stopCalled);
		}

		/*
		public UserMeetingModel Join(String userId, String meetingId)
		{
			var userMeeting = GetUserStatus(userId, meetingId);
			if (userMeeting.ConnectionIds == null)
				userMeeting.ConnectionIds = new List<string>();
			userMeeting.ConnectionIds.Add(Context.ConnectionId);
			Groups.Add(Context.ConnectionId, meetingId.ToString());
			return userMeeting;
		}

		public void Send(string name, string message)
		{
			// Call the addNewMessageToPage method to update clients.
			Clients.All.addNewMessageToPage(name, message);
		}

		public UserMeetingModel Shift(long meetingId, int start, int end)
		{
			var userStat = GetUserStatus(meetingId);
			if (userStat.EndSelection == start)
			{
				userStat.StartSelection = end;
				userStat.EndSelection = end;
			}
			else
			{
				//Out of sync
			}
			return new UserMeetingModel(userStat);
		}

		public UserMeetingModel SelectionStart(long meetingId, int start, int end)
		{
			var userStat = GetUserStatus(meetingId);
			if (userStat.EndSelection == start)
			{
				userStat.StartSelection = end;
			}
			else
			{
				//Out of sync
			}
			return new UserMeetingModel(userStat);
		}

		public UserMeetingModel SelectionEnd(long meetingId, int start, int end)
		{
			var userStat = GetUserStatus(meetingId);
			if (userStat.EndSelection == start)
			{
				userStat.EndSelection = end;
			}
			else
			{
				//Out of sync
			}
			return new UserMeetingModel(userStat);
		}

		public UserMeetingModel Removal(string selfConnectionId, long meetingId, String userId, long meetingItemId, int startIndex, int endIndex)
		{
			var userStat = GetUserStatus(meetingId);
			var text = userStat.SelectedItem.Text;
			if (userStat.EndSelection == endIndex && userStat.StartSelection == startIndex)
			{
				lock (userStat.SelectedItem.ItemLock)
				{
					userStat.SelectedItem.Text = text.Substring(0, startIndex) + text.Substring(endIndex);
					userStat.EndSelection = startIndex;
					userStat.StartSelection = startIndex;
					var meeting = _MeetingAccessor.GetMeeting(Guid.Parse(userId), Guid.Parse(meetingId));
					foreach (var user in meeting.UserStatus)
					{
						if (user.Value.UserId != userStat.UserId)
						{
							var status = user.Value;
							if (status.StartSelection < endIndex && status.StartSelection > startIndex)
								user.Value.StartSelection = startIndex;
							if (startIndex < status.EndSelection && status.StartSelection < endIndex)
								user.Value.EndSelection = startIndex;
							/*if (status.SelectedItem.MeetingItemId == meetingItemId && status.StartSelection < startIndex) {
								status.StartSelection -= characters.Length;
							}
							if (status.SelectedItem.MeetingItemId == meetingItemId && status.EndSelection < startIndex) {
								status.EndSelection += characters.Length;
							}*
						}
						foreach (var connectionId in user.Value.ConnectionIds)
						{
							if (connectionId != selfConnectionId)
								Clients.Client(connectionId).remove(userStat, startIndex, endIndex);
						}
					}
					//Clients.Group(meetingId).insert(userStat, characters);
				}
			}
			else
			{
				//Out of sync
			}
			return userStat;
		}

		public UserMeetingModel Insertion(String selfConnectionId, long meetingId, String userId, long meetingItemId, int index, String characters)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var userStat = GetUserStatus(s,meetingId);
					userStat.FocusId
					var text = userStat.SelectedItem.Text;
					if (userStat.EndSelection == index)
					{
						lock (userStat.SelectedItem.ItemLock)
						{
							userStat.SelectedItem.Text = text.Substring(0, index) + characters + text.Substring(index);
							userStat.EndSelection = index + characters.Length;
							userStat.StartSelection = index + characters.Length;
							var meeting = _MeetingAccessor.GetMeeting(Guid.Parse(userId), Guid.Parse(meetingId));
							foreach (var user in meeting.UserStatus)
							{
								var status = user.Value;
								if (status.SelectedItem.MeetingItemId == meetingItemId && status.StartSelection < index)
								{
									status.StartSelection += characters.Length;
								}
								if (status.SelectedItem.MeetingItemId == meetingItemId && status.EndSelection < index)
								{
									status.EndSelection += characters.Length;
								}
								foreach (var connectionId in user.Value.ConnectionIds)
								{
									if (connectionId != selfConnectionId)
										Clients.Client(connectionId).insert(status, index, characters);
								}
							}
							//Clients.Group(meetingId).insert(userStat, characters);
						}
					}
					else
					{
						//Out of sync
						//userStat.EndSelection = index;
						//userStat.StartSelection = index;
					}
					return userStat;

				}
			}
		}*/
	}
}