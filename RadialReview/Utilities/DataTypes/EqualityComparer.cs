using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public class EqualityComparer<T> : IEqualityComparer<T> 
    {
        protected readonly Func<T, T, bool> equals; 
        protected readonly Func<T, int> getHashCode; 

        public EqualityComparer(Func<T, T, bool> equals, Func<T, int> getHashCode) 
        { 
            this.equals = equals; 
            this.getHashCode = getHashCode; 
        } 
 
        public bool Equals(T x, T y) 
        { 
            return equals(x, y); 
        } 
 
        public int GetHashCode(T obj) 
        { 
            return getHashCode(obj); 
        } 
    } 
}