﻿using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;
using RadialReview.Models.Frontend;
using NHibernate;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {


	public class MissL10PastQuarterGenerator : BaseL10EventAnaylzerGenerators {
		public MissL10PastQuarterGenerator(long recurrenceId) : base(recurrenceId,true) {
			NumberMissed = 3;
		}
		
		public override string EventType { get { return "MissL10PastQuarter"; } }

		public int NumberMissed { get; set; }

		public override IEventAnalyzer EventAnalyzerConstructor(long recurrenceId, L10Recurrence.L10Recurrence_Attendee attendee) {
			return new MissL10PastQuarter(attendee, recurrenceId, NumberMissed);
		}

		public override string Name {
			get {
				return "Level 10's missed last quarter";
			}
		}

		public override string Description {
			get {
				return string.Format("{0} Level 10's missed{1}", NumberMissed,_MeetingName.NotNull(x=>" for "+x)??"");
			}
		}
		private string _MeetingName { get; set; }
		public override async Task PreSaveOrUpdate(ISession s) {
			_MeetingName = s.Get<L10Recurrence>(RecurrenceId).Name;
		}

		public override async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			//todo EditorField
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.NumberMissed),
			};
		}

		public override EventFrequency GetExecutionFrequency() {
			return EventFrequency.Quarterly;
		}
	}

	public class MissL10PastQuarter : IEventAnalyzer {

		public L10Recurrence.L10Recurrence_Attendee Attendee { get; set; }
		public long RecurrenceId { get; set; }
		public TimeSpan Range { get; set; }
		public int NumberMissed { get; set; }

		public MissL10PastQuarter(L10Recurrence.L10Recurrence_Attendee attendeeUser,long recurrenceId,int numberMissed) :base() {
			Attendee = attendeeUser;
			RecurrenceId = recurrenceId;
			Range = TimeSpan.FromDays(13 * 7);
			NumberMissed = numberMissed;
		}

        public EventFrequency GetExecutionFrequency() {
            return EventFrequency.Quarterly;
        }

        public IThreshold GetFireThreshold(IEventSettings settings) {
            return new EventThreshold(LessGreater.GreaterThan, NumberMissed);
        }

        public int GetNumberOfFailsToTrigger(IEventSettings settings) {
            return 1;
        }

        public int GetNumberOfPassesToReset(IEventSettings settings) {
            return 0;
        }

        public bool IsEnabled(IEventSettings settings) {
            return true;
        }

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var allMeetings = (await settings.Lookup(new SearchRealL10Meeting(RecurrenceId)));
			var meetings = allMeetings	.Where(x=>x.CompleteTime!=null && x.CompleteTime > settings.RunTime.Add(-Range))
										.Where(x=>x.StartTime !=null && Attendee.AliveAt(x.StartTime.Value))
										.ToList();

			var meetingsAttended = (await settings.Lookup(new SearchAliveMeetingAttendees(RecurrenceId)))
				.Where(x => x.UserId == Attendee.User.Id && x.L10Meeting.CompleteTime != null && x.L10Meeting.CompleteTime > settings.RunTime.Add(-Range))
				.Where(x => x.L10Meeting.StartTime != null && Attendee.AliveAt(x.L10Meeting.StartTime.Value))
				.ToList();
			//Totally incorrect. meeting attendees does not filter out the search range, nor match attendee with L10Meeting.Id
			
			return new[] { new BaseEvent(meetings.Count() - meetingsAttended.Count(), settings.RunTime) };
		}


		//public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
		//	return new[] { this };
		//}

		//public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
		//	//todo EditorField
		//	throw new NotImplementedException();
		//}

		//public string GetFriendlyName() {

		//}
	}
}