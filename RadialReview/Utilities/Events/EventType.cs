using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {

	/// <summary>
	/// ONLY USE ONE UNDERSCORE
	/// </summary>
	public enum EventType {
		CreateOrganization,

		EnableL10,
		DisableL10,
		EnableReview,
		DisableReview,

		SignupStep,

		CreateLeadershipMeeting,
		CreateDepartmentMeeting,

		ConcludeMeeting,

		DeleteMeeting,
		IssueReview,


		NoReview_3m,
		NoReview_4m,
		NoReview_6m,
		NoReview_8m,
		NoReview_12m,

		NoLeadershipMeetingCreated_1w,
		NoLeadershipMeetingCreated_2w,
		NoLeadershipMeetingCreated_3w,
		NoLeadershipMeetingCreated_4w,
		NoLeadershipMeetingCreated_6w,
		NoLeadershipMeetingCreated_8w,
		NoLeadershipMeetingCreated_10w,
		NoLeadershipMeetingCreated_12w,

		NoDepartmentMeetingCreated_2w,
		NoDepartmentMeetingCreated_4w,
		NoDepartmentMeetingCreated_6w,
		NoDepartmentMeetingCreated_8w,
		NoDepartmentMeetingCreated_10w,
		NoDepartmentMeetingCreated_12w,

		NoMeeting_1w,
		NoMeeting_2w,
		NoMeeting_3w,
		NoMeeting_4w,
		NoMeeting_6w,
		NoMeeting_8w,
		NoMeeting_10w,
		NoMeeting_12w,

		//Logins
		NoLogins_3d,
		NoLogins_5d,
		NoLogins_1w,
		NoLogins_2w,
		NoLogins_3w,
		NoLogins_4w,
		NoLogins_6w,
		NoLogins_8w,
		NoLogins_10w,
		NoLogins_12w,

		AccountAge_1d,
		AccountAge_2d,
		AccountAge_3d,
		AccountAge_4d,
		AccountAge_5d,
		AccountAge_6d,
		AccountAge_1w,
		AccountAge_2w,
		AccountAge_3w,
		AccountAge_monthly,

		PaymentFree,
		PaymentReceived,
		PaymentFailed,

		UndeleteMeeting,
		CreatePrimaryContact,

		CreateMeeting,

		StartLeadershipMeeting,
		StartDepartmentMeeting,
		EnablePeople,
		DisablePeople,

		PaymentEntered,

        EnableCoreProcess,
        DisableCoreProcess
        //AccountAge_SemiYearlyAnniversary,
    }

	public static class EventTypeExtensions {

		public static string Kind(this EventType t) {
			return ("" + t).Split('_')[0];
		}
		public static string Duration(this EventType t) {
			var split = ("" + t).Split('_');

			if (split.Length>1)
				return split[1];
			return null;
		}

		public static bool SameKind(this EventType t1, EventType t2) {
			//var s1 = ("" + t1).Split('_')[0];
			//var s2 = ("" + t2).Split('_')[0];
			return Kind(t1) == Kind(t2);
		}
	}
}