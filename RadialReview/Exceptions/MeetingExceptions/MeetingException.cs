using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Properties;

namespace RadialReview.Exceptions.MeetingExceptions
{
	public enum MeetingExceptionType
	{
		Invalid = 0,
		Unstarted = 1,
		TooMany = 2,
		AlreadyStarted=3,

		Error = 100,
	}

	public class MeetingException : RedirectException
    {
		public MeetingExceptionType MeetingExceptionType { get; set; }

        public MeetingException(String message,MeetingExceptionType exceptionType): base(message)
        {
	        MeetingExceptionType = exceptionType;
        }

		public MeetingException(MeetingExceptionType exceptionType): this(ExceptionStrings.DefaultPermissionsException, exceptionType)
        {
        }
	}
}