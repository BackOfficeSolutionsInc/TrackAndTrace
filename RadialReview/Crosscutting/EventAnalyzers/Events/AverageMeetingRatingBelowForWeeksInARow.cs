using RadialReview.Crosscutting.EventAnalyzers.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Frontend;
using RadialReview.Accessors;
using System.ComponentModel.DataAnnotations;
using NHibernate;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class AverageMeetingRatingBelowForWeeksInARow : IEventAnalyzer, IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {


		[Display(Name = "Meeting")]
		public long RecurrenceId { get; set; }
		[Display(Prompt = "Enter threshold")]
		public decimal RatingTheshold { get; set; }
		[Display(Description = "Number of weeks in a row before firing")]
		public int WeeksInARow { get; set; }
		public LessGreater Direction { get; set; }

		public string EventType { get { return "AverageMeetingRatingBelowForWeeksInARow"; } }

		public AverageMeetingRatingBelowForWeeksInARow(long recurrenceId) {
			RecurrenceId = recurrenceId;
			RatingTheshold = 7;
			WeeksInARow = 2;
			Direction = LessGreater.LessThanOrEqual;
		}


		public bool IsEnabled(IEventSettings settings) {
			return true;
		}

		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(Direction, RatingTheshold);
		}

		public EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
		}

		public int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return WeeksInARow;
		}

		public int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1;
		}

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var meetings = await settings.Lookup(new SearchRealL10Meeting(RecurrenceId));
			return EventHelper.ToBinnedEventsFromRatio(EventFrequency.Weekly, meetings, x => x.StartTime, x => x.AverageMeetingRating);
		}

		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
			return new[] { this };
		}

		public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.RatingTheshold),
				EditorField.FromProperty(this,x=>x.Direction),
				EditorField.FromProperty(this,x=>x.WeeksInARow),
			};
		}

		private string _MeetingName { get; set; }
		public async Task PreSaveOrUpdate(ISession s) {
			_MeetingName= s.Get<L10Recurrence>(RecurrenceId).Name;
		}
		/*
		  _MeetingName.NotNull(x=>" for "+x)??"");
		 */

		public string Name {
			get {
				return "Average consecutive meeting rating";
			}
		}
		public string Description {
			get {
				return string.Format("{0} for {1} weeks in a row{2}", Direction.ToDescription(RatingTheshold), WeeksInARow, _MeetingName.NotNull(x=>" for "+x)??"");
			}
		}

		//public override async Task<List<IEvent>> EventsForRecurrence(long recurrenceId, IEventSettings settings) {

		//    //var bins = EventHelper.ToBins(EventFrequency.Weekly, meetings, x => x.StartTime, x=>x.AverageMeetingRating);

		//    //var events = bins.Select(x => {
		//    //    var ratio = x.Aggregate(new Ratio(), (i, r) => i.Add(r));
		//    //    if (!ratio.IsValid())
		//    //        return null;
		//    //    return (IEvent) new BaseEvent(ratio.GetValue(null), x.Date);
		//    //}).Where(x=>x!=null).ToList();

		//    //return events;
		//    //var byWeeks = meetings.Where(x => x.StartTime.HasValue && x.AverageMeetingRating!=null).GroupBy(x => TimingUtility.GetWeekSinceEpoch(x.StartTime.Value));

		//    //var weekRatings = new List<Ratio>();
		//    //foreach(var w in byWeeks) {
		//    //    var num = w.Sum(x => x.AverageMeetingRating.Numerator);
		//    //    var den = w.Sum(x => x.AverageMeetingRating.Denominator);
		//    //    weekRatings.Add(new Ratio(num,den));
		//    //}
		//    ////var weekRatings = byWeeks.Select(x=>x.)


		//  //  return meetings.Select(x=>x.AverageMeetingRating.GetValue(null)).Where(x=>x!=null)

		//}


	}
}
