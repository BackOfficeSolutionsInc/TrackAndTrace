using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Envers;
using NHibernate.Envers.Exceptions;

namespace RadialReview.Utilities.Extensions
{
	public static class AuditExtensions
	{
		public class Revision<T>
		{
			public long RevisionId { get; set; }
			public DateTime Date { get; set; }
			public T Object { get; set; }
		}


		public static IEnumerable<Revision<T>> GetRevisionsBetween<T>(this IAuditReader self,object id,DateTime start, DateTime end)
		{
			if (start>end)
				throw new ArgumentOutOfRangeException("start","Start must come before end.");
			long s;
			long e;

			try{
				s = self.GetRevisionNumberForDate(start);
			}catch (RevisionDoesNotExistException ex){
				s = 1;
			}
			try{
				e = self.GetRevisionNumberForDate(end);
			}catch (RevisionDoesNotExistException ex){
				e = 1;
			}

			var revisionIds = self.GetRevisions(typeof (T), id).Where(x=> s<=x && x<=e).ToList();

			return revisionIds.Select(x => new Revision<T>{
				Date = self.GetRevisionDate(x),
				Object = self.Find<T>(id,x),
				RevisionId = x
			}).ToList();
		}
	}
}