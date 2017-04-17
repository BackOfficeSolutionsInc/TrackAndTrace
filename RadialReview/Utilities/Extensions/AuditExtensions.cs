using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Envers;
using NHibernate.Envers.Exceptions;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Envers.Query;

namespace RadialReview.Utilities.Extensions {

    public static class AuditExtensions {
        public static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public class Revision<T> {
            public long RevisionId { get; set; }
            public DateTime Date { get; set; }
            public T Object { get; set; }
        }

        public class RevisionDiff<T> {
            public Revision<T> Before { get; set; }
            public Revision<T> After { get; set; }
        }

        public static RevisionDiff<T> FindNearestDiff<T>(this IAuditReader self, object id, Func<T, T, bool> oldNew)
        {
            var revisions = self.GetRevisions(typeof(T), id).OrderByDescending(x => x).ToList();

            if (!revisions.Any())
                return null;

            var after = self.Find<T>(id, revisions[0]);

            if (revisions.Count == 1)
                return null;

            for (var i = 1; i < revisions.Count; i++) {
                var before = self.Find<T>(id, revisions[i]);
                if (oldNew(before, after))
                    return new RevisionDiff<T>() {
                        After = new Revision<T> {
                            Date = self.GetRevisionDate(revisions[i - 1]),
                            Object = after,
                            RevisionId = revisions[i - 1]
                        },
                        Before = new Revision<T> {
                            Date = self.GetRevisionDate(revisions[i]),
                            Object = before,
                            RevisionId = revisions[i]
                        },
                    };
                after = before;
            }
			return null;
        }

		//[Obsolete("Does not work",true)]
        public static IEnumerable<Revision<T>> GetRevisionsBetween<T>(this IAuditReader self,ISession session, object id, DateTime start, DateTime end) where T : class
        {
			if (start > end)
				throw new ArgumentOutOfRangeException("start", "Start must come before end.");
			long s;
			long e;

			try {
				s = self.GetRevisionNumberForDate(start);
			} catch (RevisionDoesNotExistException) {
				s = 1;
			}
			try {
				e = self.GetRevisionNumberForDate(end);
			} catch (RevisionDoesNotExistException) {
				e = 1;
			}
			if (s > e)
				throw new ArgumentOutOfRangeException("start", "Start must come before end.");

			var revisions = self.GetRevisions(typeof(T), id).ToList();
			var revisionIds = revisions.Where(x => s <= x && x <= e).OrderBy(x => x).ToList();

			//     ----|--> ------> --->|
			//----x----|---x-------x----|---x------

			//Still need to add the one before the start.
			var startId = s;
			if (revisionIds.Any())
				startId = revisionIds.First();
			var additional = revisions.Where(x => x < startId).ToList();
			if (additional.Any()) {
				revisionIds.Add(additional.Max());
			}

			var low = revisionIds.Min();
			var high = revisionIds.Max();

			var revisionModels = self.CreateQuery().ForHistoryOf<T ,DefaultRevisionEntity>(true).Add(AuditEntity.Id().Eq(id)).Add(AuditEntity.RevisionNumber().Ge(low)).Add(AuditEntity.RevisionNumber().Le(high)).Results();

			return revisionModels.Select(x=>new Revision<T>() {
				Date = x.RevisionEntity.RevisionDate,
				RevisionId= x.RevisionEntity.Id,
				Object = x.Entity
			});


			//if (after.Any()) {
			//	revisionIds.Add(after.First());
			//}

			//var revisionModels = revisionIds.SelectMany(x => {
			//	try {
			//		var date = self.GetRevisionDate(x);
			//		var obj = self.Find<T>(id, x);
			//		return new Revision<T> {
			//			Date = date,
			//			Object = obj,
			//			RevisionId = x
			//		}.AsList();
			//	} catch (Exception ex) {
			//		try {
			//			var obj = self.Find<T>(id, x);
			//			var date = self.GetRevisionDate(x);
			//			return new Revision<T> {
			//				Date = date,
			//				Object = obj,
			//				RevisionId = x
			//			}.AsList();
			//		} catch (Exception ex2) {
			//			log.Error("AuditExtension " + id + "," + x, ex2);
			//			throw ex2;
			//		}

			//	}
			//	return new List<Revision<T>>();
			//}).ToList();


			//var after = revisions.Where(y => y > e).OrderBy(x=>x);
			//if (!after.Any()) {
			//	var last = session.Get<T>(id);
			//	if (last != null) {
			//		revisionModels.Add(new Revision<T> {
			//			Date = end,
			//			Object = last,
			//		});
			//	}
			//}

			//return revisionModels;
		}
    }
}