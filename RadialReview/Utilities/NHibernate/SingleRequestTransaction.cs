using System.Data;
using System.Linq.Expressions;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Engine;
using NHibernate.Envers;
using NHibernate.Stat;
using NHibernate.Type;
using NHibernate.Transaction;

namespace RadialReview.Utilities.NHibernate {



	public class SingleRequestTransaction : ITransaction {

		private ITransaction _backingTransaction;
		public SingleRequestSession _request { get; set; }

		public int CommitCount { get; private set; }

		public bool IsActive {
			get {
				return _backingTransaction.IsActive;
			}
		}

		public bool WasRolledBack {
			get {
				return _backingTransaction.WasRolledBack;
			}
		}

		public bool WasCommitted {
			get {
				return _backingTransaction.WasCommitted;
			}
		}

		public ITransaction GetBackingTransaction() {
			return _backingTransaction;
		}

		public void Begin() {
			_backingTransaction.Begin();
		}

		public void Begin(IsolationLevel isolationLevel) {
			_backingTransaction.Begin(isolationLevel);
		}

		private bool _IsCommitted { get; set; }
		public void Commit() {
			//_request.TransactionDepth -= 1;
			if (_request.TransactionDepth == 1) {
				_backingTransaction.Commit();
			}
			CommitCount += 1;
			_request.GetCurrentContext().TransactionCommitted = true;
			//_IsCommitted = true;
		}

		public void Rollback() {
			_request.GetCurrentContext().TransactionRolledBack = true;
			_backingTransaction.Rollback();
		}

		public void Enlist(IDbCommand command) {
			_backingTransaction.Enlist(command);
		}

		public void RegisterSynchronization(ISynchronization synchronization) {
			_backingTransaction.RegisterSynchronization(synchronization);
		}

		public void Dispose() {
			//if (!_IsCommitted) {
			//	_request.TransactionDepth -= 1;
			//}
			_request.GetCurrentContext().TransactionDisposed = true;
			_request.TransactionDepth -= 1;
			if (_request.TransactionDepth == 0) {
				_backingTransaction.Dispose();
			}
		}
		public SingleRequestTransaction(ITransaction toWrap, SingleRequestSession request) {
			_backingTransaction = toWrap;
			_request = request;
		}


	}
}
