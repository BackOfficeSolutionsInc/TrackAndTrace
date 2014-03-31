using NHibernate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RadialReview
{
    public class MultiCriteria
    {
        public class MultiCriteriaExecuted
        {
            protected List<object> Results1 { get; set; }
            protected List<object> Results2 { get; set; }
            protected List<int> Ordered { get; set; }
            protected int Iterator { get; set; }
            protected int Iterator1 { get; set; }
            protected int Iterator2 { get; set; }

            public MultiCriteriaExecuted(List<object> results1, List<object> results2,List<int> ordered)
            {
                Results1 = results1;
                Results2 = results2;
                Ordered = ordered;
                Iterator = 0;
                Iterator1 = 0;
                Iterator2 = 0;
	        }

            protected object Next()
            {
                object output = null;
                if (Ordered[Iterator]==1){
                    output= Results1[Iterator1];
                    Iterator1++;
                }else if (Ordered[Iterator] == 2){
                    output = Results2[Iterator2];
                    Iterator2++;
                }else{
                    throw new Exception("Unknown order");
                }
                Iterator++;
                return output;
            }

            public T Get<T>()
            {
                var output = ((IList)Next())[0];
                return (T)output;
            }

            public List<T> GetList<T>()
            {
                var output = ((IList)Next()).Cast<T>().ToList();
                return output;
            }


        }


        protected ISession Session { get; set; }
        protected IMultiCriteria UnderlyingCriteria { get; set; }
        protected IMultiQuery UnderlyingQuery { get; set; }

   
        //protected List<object> Outputs {get;set;}
        //protected List<Type> Types { get; set; }
        //protected List<Type> CastTypes { get; set; }
        protected bool Executed { get; set; }

        protected List<int> Ordered { get; set; }

        //protected Dictionary<string, object> Objects { get; set; }

        protected MultiCriteria(ISession session, IMultiCriteria underlyingCriteria,IMultiQuery underlyingQuery)
        {
            Session = session;
            UnderlyingCriteria = underlyingCriteria;
            UnderlyingQuery = underlyingQuery;
            Ordered = new List<int>();
            Executed = false;
            //Outputs = new List<object>();
            //Outputs = new List<object>();
            //Types = new List<Type>();
            //CastTypes = new List<Type>();
            //Objects = new Dictionary<string, object>();
        }

        public static MultiCriteria Create(ISession s)
        {
            return new MultiCriteria(s, s.CreateMultiCriteria(), s.CreateMultiQuery());
        }
        /*
        public MultiCriteria AddList<T,R>(IQueryOver<T> query,String key) where R : IEnumerable<T>
        {
            if (Executed)
                throw new Exception("Query has already been executed.");

            var rType = typeof(T);
            Underlying.Add<T>(query);
            CastTypes.Add(typeof(R));
            Types.Add(typeof(T));
            Objects.Add(key,null);
            //Update.Add(x=>result = x);

            return this;
        }
        */

        public MultiCriteria AddInt<T>(IQueryOver<T> query)
        {
            if (Executed)
                throw new Exception("Query has already been executed.");

            UnderlyingCriteria.Add<int>(query);
            Ordered.Add(1);

            return this;
        }
        public MultiCriteria Add(ICriteria query)
        {
            if (Executed)
                throw new Exception("Query has already been executed.");

            UnderlyingCriteria.Add(query);
            Ordered.Add(1);

            return this;
        }


        public MultiCriteria Add<T,R>(IQueryOver<T> query)
        {
            if (Executed)
                throw new Exception("Query has already been executed.");

            UnderlyingCriteria.Add<R>(query);
            Ordered.Add(1);

            return this;
        }
        public MultiCriteria Add(IQuery query)
        {
            if (Executed)
                throw new Exception("Query has already been executed.");

            //result = default(R);
            //Update.Add(update);
            //var rType= typeof(R);
            UnderlyingQuery.Add(query);
            Ordered.Add(2);
            //CastTypes.Add(rType);
            //Types.Add(rType);
            //Objects.Add(key, dfltValue);


            return this;
        }

        public MultiCriteria Add<T>(IQueryOver<T> query)
        {
            if (Executed)
                throw new Exception("Query has already been executed.");

            //result = default(R);
            //Update.Add(update);
            //var rType= typeof(R);
            UnderlyingCriteria.Add<T>(query);
            Ordered.Add(1);
            //CastTypes.Add(rType);
            //Types.Add(rType);
            //Objects.Add(key, dfltValue);


            return this;
        }

        public MultiCriteriaExecuted Execute()
        {
            Executed = true;
            var outputs1 = new List<object>();
            var outputs2 = new List<object>();
            if (Ordered.Any(x=>x==1))
            {
                foreach (var o in UnderlyingCriteria.List())
                {
                    outputs1.Add(o);
                }
            }
            if (Ordered.Any(x => x == 2))
            {
                foreach (var o in UnderlyingQuery.List())
                {
                    outputs2.Add(o);
                }
            }

            return new MultiCriteriaExecuted(outputs1,outputs2,Ordered);
            
            /*
            for (int i=0;i<result.Count;i++)
            {
                //var type   = Types[i];
                //var castType = CastTypes[i];
                //if (type != castType)
                //{
                    Outputs.Add(result[ Convert.ChangeType(result[i], castType);
                }
                else
                {
                    var first = ((IList)result[i])[0] ;
                    if (first == null)
                        Outputs[i] = first;
                    else
                        Outputs[i] = Convert.ChangeType(first, castType);
                }
            }*/
        }
    }
}