using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace RadialReview.Exceptions
{
	public class SyncException : RedirectException
	{
		public long? ClientTimestamp { get; set; }
		public SyncException(string message, long? clientTimestamp): base(message)
		{
			ClientTimestamp = clientTimestamp;
			Silent = true;
			NoErrorReport = true;
            StatusCodeOverride = HttpStatusCode.Conflict;
		}

		public SyncException(long? clientTimestamp) : 
			this(clientTimestamp == null 
					? "Client Timestamp was null" 
					: "Client Timestamp (" + clientTimestamp.Value + ") was out of order."
			,clientTimestamp)
		{
		}
	}
}