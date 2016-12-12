using NHibernate;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Application;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {
    public partial class RealTimeUtility {
        public class RTRecurrenceUpdater {

            protected List<long> _recurrenceIds = new List<long>();
            protected Dictionary<long, L10Meeting> _recurrenceId_meeting = new Dictionary<long, L10Meeting>();
            protected RealTimeUtility rt;
            public RTRecurrenceUpdater(IEnumerable<long> recurrences, RealTimeUtility rt) {
                _recurrenceIds = recurrences.Distinct().ToList();
                this.rt = rt;
            }

            protected void UpdateAll(Func<long, IAngularItem> itemGenerater) {
                foreach (var r in _recurrenceIds) {
                    var updater = rt.GetUpdater<MeetingHub>(MeetingHub.GenerateMeetingGroupId(r));
                    updater.Add(itemGenerater(r));
                }
            }

            public RTRecurrenceUpdater AddLowLevelAction(Action<dynamic> action) {
                rt.AddAction(() => {
                    foreach (var r in _recurrenceIds) {
                        var g = rt.GetGroup<MeetingHub>(MeetingHub.GenerateMeetingGroupId(r));
                        action(g);
                    }
                });
                return this;
            }

            public RTRecurrenceUpdater UpdateMeasurable(MeasurableModel measurable, AngularListType type = AngularListType.ReplaceIfNewer) {
                rt.AddAction(() => {
                    UpdateAll(rid => new AngularMeasurable(measurable));
                });
                return this;
            }
            public RTRecurrenceUpdater UpdateMeasurable(MeasurableModel measurable, IEnumerable<ScoreModel> scores, AngularListType type = AngularListType.ReplaceIfNewer) {
                rt.AddAction(() => {
                    UpdateAll(rid => new AngularMeasurable(measurable));
                });
                return UpdateScorecard(scores.Where(x => x.Measurable.Id == measurable.Id), type);
            }

            public RTRecurrenceUpdater Update(IAngularItem item) {
                return Update(rid => item);
            }
            public RTRecurrenceUpdater Update(Func<long, IAngularItem> item) {
                rt.AddAction(() => {
                    UpdateAll(item);
                });
                return this;
            }

            public RTRecurrenceUpdater UpdateScorecard(IEnumerable<ScoreModel> scores, AngularListType type = AngularListType.ReplaceIfNewer) {
                rt.AddAction(() => {
                    //UpdateAngular stuff
                    var scorecard = new AngularScorecard();
                    var measurablesList = new List<AngularMeasurable>();
                    foreach (var m in scores.GroupBy(x => x.Measurable.Id)) {
                        var measurable = m.First().Measurable;
                        measurablesList.Add(new AngularMeasurable(measurable));
                        var scoresList = new List<AngularScore>();
                        foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id)) {

                            scoresList.Add(new AngularScore(ss, false));
                        }
                        scorecard.Scores = AngularList.Create<AngularScore>(type, scoresList);
                    }
                    scorecard.Measurables = AngularList.Create(type, measurablesList);
                    UpdateAll(rid => {
                        scorecard.Id = rid;
                        return scorecard;
                    });
                });
                return this;
            }

            public void SetFocus(string selector) {

                rt.AddAction(() => {
                    UpdateAll(x => new AngularRecurrence(x) {
                       Focus = selector
                    });
                });
            }
        }
    }
}