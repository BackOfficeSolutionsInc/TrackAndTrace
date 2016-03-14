using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public class DiscreteDistribution<T> {
        private double Low {get;set;}
        private double High {get;set;}
        private bool Coerce { get; set; }
        private Multimap<T,double> Backing { get; set; }

        public DiscreteDistribution<U> Convert<U>(Func<T, U> convert)
        {
            return new DiscreteDistribution<U>(Low,High) {
                Backing=Backing.ConvertKey(convert)
            };
        }

        public DiscreteDistribution(double lowScore,double highScore,bool coerce=false){
            Low = lowScore;
            High = highScore;
            Coerce = coerce;
            Backing = new Multimap<T, double>();
        }

        public void Add(T value,double score){
            var min = Math.Min(Low,High);
            var max = Math.Max(High,Low);
            if (Coerce)
                score = Math.Max(Math.Min(max, score), min);

            if (score > max  || score< min)
                throw new ArgumentOutOfRangeException("score", "Score is out of range");
            var newScore = (score-Low)/(High-Low);
                        
            Backing.Add(value,newScore);
        }
        //public void AddPercentage(T value, double percentage)
        //{
        //    var score = (High - Low) * percentage + Low;
        //    Add(value, score);
        //}

        public T ResolveOne()
        {
            T resolved= default(T);
            TryResolveOne(ref resolved);
            return resolved;
        }
        public int TryResolveOne(ref T resolved)
        {
            var found = GetProbabilities();
            if (!found.Any())
                return 0;
            var first = found.First();
            resolved = first.Key;
            return found.TakeWhile(x => x.Value == first.Value).Count();
        }


        public int Count()
        {
            return Backing.Sum(x=>x.Value.Count);
        }

        public List<KeyValuePair<T, double>> GetProbabilities()
        {
            var n = Count();
            var sum = Backing.Sum(x=>x.Value.Sum(y=>Math.Pow(y,n)));

            
            return Backing.Select(x => new KeyValuePair<T,double>(x.Key,x.Value.Sum(y=>Math.Pow(y,n))/sum)).OrderByDescending(x=>x.Value).ToList();// .Create(x.Item1, Math.Pow(x.Item2, n) / sum)).ToList();
        }

        public List<KeyValuePair<T, double>> GetBacking()
        {
            return Backing.SelectMany(x => x.Value.Select(y => new KeyValuePair<T, double>(x.Key, y))).ToList();
        }
       
    }
}