using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public class CacheKeys
	{
		public static readonly string USER					= "Session_User";
		public static readonly string USERORGANIZATION		= "Session_UserOrganization";
		public static readonly string USERORGANIZATION_ID	= "Session_UserOrganization_Id";
		public static readonly string UNSTARTED_TASKS		= "Session_Unstarted_Tasks";
		public static readonly string LAST_SEND_NOTE_TIME	= "LastSendNoteTime";
		public static readonly string MANAGE_PAGE			= "ManagePage";
		public static readonly string ORGANIZATION_ID		= "OrganizationId";
		public static readonly string REPORTS_PAGE			= "ReportsPage";

	}
}