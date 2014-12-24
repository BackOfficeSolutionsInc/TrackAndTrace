using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
	public class SchedulerException : PermissionsException
	{
		public SchedulerException(string message): base(message){
			
		}
	}
}