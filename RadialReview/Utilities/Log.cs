using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Ajax.Utilities;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Views.Log;

namespace RadialReview.Utilities
{
	public class Log
	{

		public static void Message(ISession s, UserOrganizationModel caller,LogType type, ILongIdentifiable about, string title, string message = null)
		{
			s.Save(LogModel.Create(caller, type, about, title));
		}
	}
}