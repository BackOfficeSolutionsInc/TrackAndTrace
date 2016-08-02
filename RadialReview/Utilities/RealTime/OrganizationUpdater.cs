using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {


    public partial class RealTimeUtility {
        public class RTOrganizationUpdater {

            protected long _OrganizationId { get; set; }
            protected RealTimeUtility rt;
            public RTOrganizationUpdater(long orgId, RealTimeUtility rt)
            {
                _OrganizationId = orgId;
                this.rt = rt;
            }

            protected void UpdateAll(Func<long, IAngularItem> itemGenerater)
            {
                var updater = rt.GetUpdater<OrganizationHub>(OrganizationHub.GenerateId(_OrganizationId));
                updater.Add(itemGenerater(_OrganizationId));
            }
            public RTOrganizationUpdater Update(IAngularItem item)
            {
                return Update(rid => item);
            }
            public RTOrganizationUpdater Update(Func<long, IAngularItem> item)
            {
                rt.AddAction(() => {
                    UpdateAll(item);
                });
                return this;
            }

        }
    }
}