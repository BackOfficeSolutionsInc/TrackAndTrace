using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {
    public partial class RealTimeUtility {
        protected class RTRecurrenceUpdater {

            protected List<long> _recurrenceIds = new List<long>();
            protected RealTimeUtility rt;
            public RTRecurrenceUpdater(IEnumerable<long> recurrences, RealTimeUtility rt)
            {
                _recurrenceIds = recurrences.Distinct().ToList();
                this.rt = rt;
            }

            protected void UpdateAll(IAngularItem item)
            {
                foreach (var r in _recurrenceIds) {
                    var updater = rt.GetUpdater<MeetingHub>(MeetingHub.GenerateMeetingGroupId(r));
                    updater.Add(item);
                }

            }

            public RTRecurrenceUpdater UpdateScorecard(List<ScoreModel> scores, AngularListType type)
            {
                rt.AddAction(() => {
                    //UpdateAngular stuff
                    var scorecard = new AngularScorecard();
                    var measurablesList = new List<AngularMeasurable>();
                    foreach (var m in scores.GroupBy(x => x.Measurable.Id)) {
                        var measurable = m.First().Measurable;
                        measurablesList.Add(new AngularMeasurable(measurable));
                        var scoresList = new List<AngularScore>();
                        foreach (var ss in scores.Where(x => x.Measurable.Id == measurable.Id)) {
                            scoresList.Add(new AngularScore(ss));
                        }
                        scorecard.Scores = AngularList.Create<AngularScore>(type, scoresList);
                    }
                    scorecard.Measurables = AngularList.Create(type, measurablesList);
                    UpdateAll(scorecard);

                });
                return this;

            }
        }
    }
}