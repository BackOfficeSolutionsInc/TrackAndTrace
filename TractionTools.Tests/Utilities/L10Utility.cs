using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;

namespace TractionTools.Tests.Utilities {
    public class L10 {
        public long Id { get { return Recur.Id; } }
        public L10Recurrence Recur { get; set; }
        public OrganizationModel Org { get; set; }
        public UserOrganizationModel Creator { get; set; }
        public UserOrganizationModel Employee { get; set; }

    }
    public class L10Utility {

        public static async Task<L10> CreateRecurrence(string name)
        {
            return await CreateRecurrence(existing:null,name: name);
        }

        public static async Task<L10> CreateRecurrence(L10 existing = null,string name =null)
        {
            UserOrganizationModel employee = null;
            UserOrganizationModel manager = null;
            OrganizationModel o = null;
            if (existing == null) {

                BaseTest.DbCommit(s => {
                    //Org
                    o = new OrganizationModel() { };
                    o.Settings.TimeZoneId = "GMT Standard Time";
                    s.Save(o);

#pragma warning disable CS0618 // Type or member is obsolete
					var plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Professional_Monthly_March2016, new DateTime(2016,5,14));
#pragma warning restore CS0618 // Type or member is obsolete
					PaymentAccessor.AttachPlan(s, o, plan);
                    //User
                    var u = new UserOrganizationModel() { Organization = o };
                    s.Save(u);
                    employee = u;
                    manager = new UserOrganizationModel() { Organization = o, ManagerAtOrganization = true };
                    s.Save(manager);
                });
            } else {
                o = existing.Org;
                manager = existing.Creator;
            }

            var recur = await L10Accessor.CreateBlankRecurrence(manager, o.Id);
            if (name != null) {
                BaseTest.DbCommit(s => {
                    recur = s.Get<L10Recurrence>(recur.Id);
                    recur.Name = name;
                    s.Update(recur);
                });
            }

            return new L10 {
                Employee = employee,
                Creator = manager,
                Org = o,
                Recur = recur
            };
        }
    }
}
