using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Envers;
using NHibernate.Envers.Exceptions;
using log4net;


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


        public static IEnumerable<Revision<T>> GetRevisionsBetween<T>(this IAuditReader self, object id, DateTime start, DateTime end)
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

            var revisions = self.GetRevisions(typeof(T), id).ToList();
            var revisionIds = revisions.Where(x => s <= x && x <= e).OrderBy(x => x).ToList();

            //     ----|--> ------> --->|
            //----x----|---x-------x----|---x------

            //Still need to add the one before the start.
            var startId = long.MaxValue;
            if (revisionIds.Any())
                startId = revisionIds.First();
            var additional = revisions.Where(x => x < startId).ToList();
            if (additional.Any()) {
                revisionIds.Add(additional.Max());
            }

            return revisionIds.SelectMany(x =>{ 
                try{
                    return new Revision<T> {
                        Date = self.GetRevisionDate(x),
                        Object = self.Find<T>(id, x),
                        RevisionId = x
                    }.AsList();
                }catch(Exception ex){
                    log.Error("AuditExtension "+id+","+x,ex);
                    
                }
                return new List<Revision<T>>();
            }).ToList();
        }
    }
}