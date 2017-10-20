using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Audit;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;

namespace RadialReview.Models.Timeline
{
	public class MeetingTimeline
	{
		public List<TimelineItem> Items { get; set; }
		public L10Meeting Meeting { get; set; }
	}

	public class TimelineLookup
	{
		public class Item
		{
			public string Icon { get; set; }
			public string FriendlyName { get; set; }
			public string Color { get; set; }

			public Func<ITimeline,string> Anchor { get; set; }

			public Item(string friendlyName, string color, string icon,Func<ITimeline,string> anchor=null)
			{
				FriendlyName = friendlyName;
				Color = color;
				Icon = icon;
				Anchor = anchor ?? (x => x.CustomAnchor());
			}
		}

		/// <summary>
		/// ORDERING MATTERS FOR GetIconOrdering()
		/// </summary>
		private static Dictionary<string, Item> _lookup = new Dictionary<string, Item>(){	
			{"StartMeeting",			new Item("Start meeting",		"#f0ad4e","glyphicon glyphicon-play")},

			{"JoinL10Meeting",			new Item("Join meeting",		"#f0ad4e","glyphicon glyphicon-user")},

			{"UpdateScore",				new Item("Update score",		"#f0ad4e","glyphicon glyphicon-scale")},
			{"UpdateScoreInMeeting",	new Item("Update score",		"#f0ad4e","glyphicon glyphicon-scale")},
			{"UpdateMeasurable",		new Item("Update measurable",	"#f0ad4e","glyphicon glyphicon-scale")},
			{"UpdateArchiveMeasurable",	new Item("Update measurable",	"#f0ad4e","glyphicon glyphicon-scale")},
			{"CreateMeasurable",        new Item("Create measurable",   "#f0ad4e","glyphicon glyphicon-scale")},
			{"DeleteMeasurable",        new Item("Delete measurable",   "#f0ad4e","glyphicon glyphicon-scale")},
			{"SetMeasurableOrdering",	new Item("Reorder measurables",	"#f0ad4e","glyphicon glyphicon-scale")},	

			{"UpdateRockCompletion",	new Item("Update rock",			"#f0ad4e","icon fontastic-icon-bullseye")},
			{"CreateRock",				new Item("Create rock",			"#f0ad4e","icon fontastic-icon-bullseye")},

			{"UpdateTodo",				new Item("Update to-do",		"#f0ad4e","glyphicon glyphicon-check")},
			{"CreateTodo",				new Item("Create to-do",		"#f0ad4e","glyphicon glyphicon-check")},
			{"UpdateTodos",				new Item("Update to-do",		"#f0ad4e","glyphicon glyphicon-check")},

			{"UpdateIssue",				new Item("Update issue",		"#f0ad4e","glyphicon glyphicon-pushpin")},
			{"CreateIssue",				new Item("Create issue",		"#f0ad4e","glyphicon glyphicon-pushpin")},
			{"UpdateIssues",			new Item("Update issue",		"#f0ad4e","glyphicon glyphicon-pushpin")},
			{"CopyIssue",				new Item("Copy issue",			"#f0ad4e","glyphicon glyphicon-pushpin")},
			

			{"ConcludeMeeting",			new Item("Conclude meeting",	"#f0ad4e","glyphicon glyphicon-stop")},

			{"UpdatePage",				new Item("Change page",			"#f0ad4e","glyphicon glyphicon-bookmark")},

			{"EditNote",				new Item("Edit note",			"#f0ad4e","glyphicon glyphicon-file")},
			{"CreateNote",				new Item("Create note",			"#f0ad4e","glyphicon glyphicon-file")},

			{"EditL10",					new Item("Edit L10",			"#f0ad4e","glyphicon glyphicon-cog")},
			{"EditL10Recurrence",		new Item("Edit L10",			"#f0ad4e","glyphicon glyphicon-cog")},
			{"DeleteL10",				new Item("Delete L10",			"#f0ad4e","glyphicon glyphicon-trash")},
		};


        public static List<Item> GetIconOrdering()
		{
			return _lookup.Distinct(x => x.Value.Icon).Select(x=>x.Value).ToList();
		} 

		public static Item Find(string key)
		{
			if (_lookup.ContainsKey(key))
			{
				return _lookup[key];
			}
			return new Item(key, "#999999", "glyphicon glyphicon-paperclip");
		}

	}

	public class TimelineItem
	{
		private List<string> _anchors;

		public enum TimelineItemType
		{
			Block,
			Marker
		}

		public List<string> Anchors
		{
			get { return _anchors.Where(x=>!String.IsNullOrWhiteSpace(x)).Distinct().ToList(); }
			set { _anchors = value; }
		}

		public DateTime Time { get; set; }
		public string TimeString { get; set; }
		public string Title { get; set; }
		public string Details { get; set; }
		public string Icon { get; set; }
		public string User { get; set; }
		public string IconColor { get; set; }
		public TimelineItemType Type { get; set; }

		public static TimelineItem Create(UserOrganizationModel viewer, Transcript t)
		{
			return new TimelineItem()
			{
				Anchors = new List<string>(){"Transcript-"+t.Id},
				Details = t.Text,
				Icon = "glyphicon glyphicon-comment",
				IconColor = "#999999",
				Time = t.CreateTime,
				TimeString = viewer.Organization.ConvertFromUTC(t.CreateTime).ToString("MM/dd/yy H:mm:ss"),
				Title = "Transcript",
				Type = TimelineItemType.Marker,
				User = t._User.NotNull(x => x.GetName()),
			};
		}

		public static TimelineItem Create(UserOrganizationModel viewer, L10AuditModel a)
		{
			var lu = TimelineLookup.Find(a.Action);
			return new TimelineItem()
			{
				Anchors =new List<String>{a.CustomAnchor(),"Audit-"+a.Id},
				Time = a.CreateTime,
				TimeString = viewer.Organization.ConvertFromUTC(a.CreateTime).ToString("MM/dd/yy H:mm:ss"),
				User = a.UserOrganization.NotNull(x => x.GetName()),
				Title = lu.FriendlyName,
				Details = a.Notes,
				Icon = lu.Icon,
				IconColor = lu.Color,
				Type = TimelineItemType.Block
			};
		}
	}
}