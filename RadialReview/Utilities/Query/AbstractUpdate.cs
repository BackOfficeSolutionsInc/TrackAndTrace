using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Query
{
    public interface IUpdate
    {
        void Save(object obj);
        void SaveOrUpdate(object obj);
        void Update(object obj);
    }
    public abstract class AbstractUpdate : IUpdate
    {
        public abstract void Save(object obj);

        public abstract void SaveOrUpdate(object obj);
 
        public abstract void Update(object obj);
    }
}