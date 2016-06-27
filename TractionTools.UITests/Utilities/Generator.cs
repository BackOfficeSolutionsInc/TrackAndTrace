using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;

namespace TractionTools.UITests.Utilities {
    //public class Generator {
    //    public static L10 CreateRecurrence(string meetingName)
    //    {
    //        OrganizationModel org = null;
    //        UserOrganizationModel manager = null;
    //        UserOrganizationModel employee = null;
    //        BaseTest.DbCommit(s => {
    //            //Org
    //            org = new OrganizationModel() { };
    //            org.Settings.TimeZoneId = "GMT Standard Time";
    //            s.Save(org);

    //            //User
    //            manager = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
    //            s.Save(manager);

    //            employee = new UserOrganizationModel() { Organization = org, ManagerAtOrganization = true };
    //            s.Save(employee);
    //        });

    //        //Create The Recurrence
    //        var recur = new L10Recurrence() { Organization = org, OrganizationId = org.Id, Name = meetingName };
    //        recur._DefaultAttendees = new List<L10Recurrence.L10Recurrence_Attendee>(){new L10Recurrence.L10Recurrence_Attendee(){
    //            L10Recurrence = recur,
    //            User = manager
                
    //        }};
    //        recur._DefaultMeasurables = new List<L10Recurrence.L10Recurrence_Measurable>();
    //        recur._DefaultRocks = new List<L10Recurrence.L10Recurrence_Rocks>();
    //        L10Accessor.EditL10Recurrence(manager, recur);

    //        return L10Accessor.GetL10Recurrence(manager, recur.Id, true);

    //    }
    //}
}
