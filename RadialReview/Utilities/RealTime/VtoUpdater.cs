﻿using RadialReview.Hubs;
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
            protected void UpdateAll(Func<long, IAngularItem> itemGenerater)
            {
                foreach (var r in _vtoIds) {
                    var updater = rt.GetUpdater<VtoHub>(VtoHub.GenerateVtoGroupId(r));
                    updater.Add(itemGenerater(r));
                }
            }
            public RTVtoUpdater Update(IAngularItem item)
            {
                return Update(rid => item);
            }
            public RTVtoUpdater Update(Func<long, IAngularItem> item)
            {
                rt.AddAction(() => {
                    UpdateAll(item);
                });
                return this;
            }
        }
    }
}