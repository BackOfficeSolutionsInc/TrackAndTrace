using NHibernate;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Query
{
    public class IEnumerableTransaction : ITransaction
    {
        public void Begin(IsolationLevel isolationLevel)
        {
            
        }

        public void Begin()
        {
            
        }

        public void Commit()
        {
            
        }

        public void Enlist(System.Data.IDbCommand command)
        {
            
        }

        public bool IsActive
        {
            get { return true; }
        }

        public void RegisterSynchronization(global::NHibernate.Transaction.ISynchronization synchronization)
        {
            
        }

        public void Rollback()
        {
            
        }

        public bool WasCommitted
        {
            get { return true; }
        }

        public bool WasRolledBack
        {
            get { return false; }
        }

        public void Dispose()
        {
            
        }
    }
}