using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.VTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {
    public partial class RealTimeUtility {
        public class RTVtoUpdater {
            
            protected List<long> _vtoIds = new List<long>();
            protected Dictionary<long, VtoModel> _vtoId_vto = new Dictionary<long, VtoModel>();
            protected RealTimeUtility rt;
            public RTVtoUpdater(IEnumerable<long> vtos, RealTimeUtility rt)
            {
                _vtoIds = vtos.Distinct().ToList();
                this.rt = rt;
            }
            protected void UpdateAll(Func<long, IAngularId> itemGenerater)
            {
                foreach (var r in _vtoIds) {
                    var updater = rt.GetUpdater<RealTimeHub>(RealTimeHub.Keys.GenerateVtoGroupId(r));
                    updater.Add(itemGenerater(r));
                }
            }
            public RTVtoUpdater Update(IAngularId item)
            {
                return Update(rid => item);
            }
            public RTVtoUpdater Update(Func<long, IAngularId> item)
            {
                rt.AddAction(() => {
                    UpdateAll(item);
                });
                return this;
            }
        }
    }
}