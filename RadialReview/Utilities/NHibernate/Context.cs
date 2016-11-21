using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview {
	public class Context : IDisposable {
		private ISession _s { get; set; }
		private ITransaction _tx { get; set; }
		private PermissionsUtility _perms { get; set; }
		private UserOrganizationModel _caller { get; set; }
		private RealTimeUtility _rt { get; set; }
		

		private void EnsureSessionCreated() {
			if (_s == null) {
				_s = HibernateSession.GetCurrentSession();
				_tx = _s.BeginTransaction();
			}
		}

		public ISession s {
			get {
				EnsureSessionCreated();
				return _s;
			}
		}

		public ITransaction tx {get {
				EnsureSessionCreated();
				return _tx;
			}
		}

		public Context(ISession s,PermissionsUtility perms) {
			_s = s;
			_perms = perms;
			_caller = perms.GetCaller();
		}

		public Context(UserOrganizationModel caller) {
			_caller = caller;
		}

		public PermissionsUtility perms {
			get {
				EnsureSessionCreated();
				if (_perms == null) {
					if (_caller == null)
						throw new Exception("caller was null");
					_perms = PermissionsUtility.Create(s, _caller);
				}

				return _perms;
			}
		}

		public UserOrganizationModel caller {
			get { return _caller; }
		}

		public RealTimeUtility rt {
			get {
				if (_rt == null) {
					_rt = RealTimeUtility.Create();
				}
				return _rt;
			}
		}

		public RealTimeUtility InitializeRT(string exclude) {
			if (_rt != null)
				throw new Exception("RealTime already initialized");
			_rt = RealTimeUtility.Create(exclude);
			return _rt;
		}


		public void Dispose() {
			if (_rt != null)
				_tx.Dispose();
			if (_tx != null)
				_tx.Dispose();
			if (_s != null)
				_s.Dispose();
		}
	}
}