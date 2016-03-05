using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
    public class Histogram<T> : IEnumerable<KeyValuePair<T,int>>
    {
        private List<Tuple<T, DateTime>> Backing { get; set; }
        public Histogram()
        {
            Backing = new List<Tuple<T, DateTime>>();
        }
        public void Add(T item){
            Backing.Add(Tuple.Create(item, DateTime.UtcNow));
        }

        public bool StatisticallySignificant()
        {
            return (Backing.Count >= 32);
        }

        public List<KeyValuePair<T, int>> GetCounts()
        {
            var dict = new Dictionary<T, int>();

            foreach (var b in Backing){
                if (!dict.ContainsKey(b.Item1)){
                    dict[b.Item1] = 1;
                }else{
                    dict[b.Item1] += 1;
                }
            }
            return dict.OrderByDescending(x => x.Value).ToList();
        }


        public IEnumerator<KeyValuePair<T, int>> GetEnumerator()
        {
            return GetCounts().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetCounts().GetEnumerator();
        }
    }
}