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

namespace RadialReview.Utilities.NHibernate
{
	public static class AuditReaderExtensions
	{
		public static IAuditReader AuditReader(this ISession self)
		{
			if (self is SessionPerRequest)
				return ((SessionPerRequest)self).GetBackingSession().Auditer();
			return self.Auditer();
		}

	}

	
	public class SessionPerRequest : ISession
	{

		private ISession _backingSession;
		public bool WasDisposed { get; set; }

		public ISession GetBackingSession()
		{
			return _backingSession;
		}

		
		public SessionPerRequest(ISession toWrap)
		{
			_backingSession = toWrap;
		}
		public IAuditReader Auditer()
		{
			return GetBackingSession().Auditer();
		}

		#region Edited 
		public void Dispose()
		{
			WasDisposed = true;
			//skips
			//_backingSession.Dispose();
		}

		#endregion
		#region Wrapped
		public void Flush()
		{
			_backingSession.Flush();
		}

		public IDbConnection Disconnect()
		{
			return _backingSession.Disconnect();
		}

		public void Reconnect()
		{
			_backingSession.Reconnect();
		}

		public void Reconnect(IDbConnection connection)
		{
			_backingSession.Reconnect(connection);
		}

		public IDbConnection Close()
		{
			return _backingSession.Close();
		}

		public void CancelQuery()
		{
			_backingSession.CancelQuery();
		}

		public bool IsDirty()
		{
			return _backingSession.IsDirty();
		}

		public bool IsReadOnly(object entityOrProxy)
		{
			return _backingSession.IsReadOnly(entityOrProxy);
		}

		public void SetReadOnly(object entityOrProxy, bool readOnly)
		{
			_backingSession.SetReadOnly(entityOrProxy, readOnly);
		}

		public object GetIdentifier(object obj)
		{
			return _backingSession.GetIdentifier(obj);
		}

		public bool Contains(object obj)
		{
			return _backingSession.Contains(obj);
		}

		public void Evict(object obj)
		{
			_backingSession.Evict(obj);
		}

		public object Load(Type theType, object id, LockMode lockMode)
		{
			return _backingSession.Load(theType, id, lockMode);
		}

		public object Load(string entityName, object id, LockMode lockMode)
		{
			return _backingSession.Load(entityName, id, lockMode);
		}

		public object Load(Type theType, object id)
		{
			return _backingSession.Load(theType, id);
		}

		public T Load<T>(object id, LockMode lockMode)
		{
			return _backingSession.Load<T>(id, lockMode);
		}

		public T Load<T>(object id)
		{
			return _backingSession.Load<T>(id);
		}

		public object Load(string entityName, object id)
		{
			return _backingSession.Load(entityName, id);
		}

		public void Load(object obj, object id)
		{
			_backingSession.Load(obj, id);
		}

		public void Replicate(object obj, ReplicationMode replicationMode)
		{
			_backingSession.Replicate(obj, replicationMode);
		}

		public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
		{
			_backingSession.Replicate(entityName, obj, replicationMode);
		}

		public object Save(object obj)
		{
			return _backingSession.Save(obj);
		}

		public void Save(object obj, object id)
		{
			_backingSession.Save(obj, id);
		}

		public object Save(string entityName, object obj)
		{
			return _backingSession.Save(entityName, obj);
		}

		public void Save(string entityName, object obj, object id)
		{
			_backingSession.Save(entityName, obj, id);
		}

		public void SaveOrUpdate(object obj)
		{
			_backingSession.SaveOrUpdate(obj);
		}

		public void SaveOrUpdate(string entityName, object obj)
		{
			_backingSession.SaveOrUpdate(entityName, obj);
		}

		public void SaveOrUpdate(string entityName, object obj, object id)
		{
			_backingSession.SaveOrUpdate(entityName, obj, id);
		}

		public void Update(object obj)
		{
			_backingSession.Update(obj);
		}

		public void Update(object obj, object id)
		{
			_backingSession.Update(obj, id);
		}

		public void Update(string entityName, object obj)
		{
			_backingSession.Update(entityName, obj);
		}

		public void Update(string entityName, object obj, object id)
		{
			_backingSession.Update(entityName, obj, id);
		}

		public object Merge(object obj)
		{
			return _backingSession.Merge(obj);
		}

		public object Merge(string entityName, object obj)
		{
			return _backingSession.Merge(entityName, obj);
		}

		public T Merge<T>(T entity) where T : class
		{
			return _backingSession.Merge(entity);
		}

		public T Merge<T>(string entityName, T entity) where T : class
		{
			return _backingSession.Merge(entityName, entity);
		}

		public void Persist(object obj)
		{
			_backingSession.Persist(obj);
		}

		public void Persist(string entityName, object obj)
		{
			_backingSession.Persist(entityName, obj);
		}

		public void Delete(object obj)
		{
			_backingSession.Delete(obj);
		}

		public void Delete(string entityName, object obj)
		{
			_backingSession.Delete(entityName, obj);
		}

		public int Delete(string query)
		{
			return _backingSession.Delete(query);
		}

		public int Delete(string query, object value, IType type)
		{
			return _backingSession.Delete(query, value, type);
		}

		public int Delete(string query, object[] values, IType[] types)
		{
			return _backingSession.Delete(query, values, types);
		}

		public void Lock(object obj, LockMode lockMode)
		{
			_backingSession.Lock(obj, lockMode);
		}

		public void Lock(string entityName, object obj, LockMode lockMode)
		{
			_backingSession.Lock(entityName, obj, lockMode);
		}

		public void Refresh(object obj)
		{
			_backingSession.Refresh(obj);
		}

		public void Refresh(object obj, LockMode lockMode)
		{
			_backingSession.Refresh(obj, lockMode);
		}

		public LockMode GetCurrentLockMode(object obj)
		{
			return _backingSession.GetCurrentLockMode(obj);
		}

		public ITransaction BeginTransaction()
		{
			return _backingSession.BeginTransaction();
		}

		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			return _backingSession.BeginTransaction(isolationLevel);
		}

		public ICriteria CreateCriteria<T>() where T : class
		{
			return _backingSession.CreateCriteria<T>();
		}

		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			return _backingSession.CreateCriteria<T>(alias);
		}

		public ICriteria CreateCriteria(Type persistentClass)
		{
			return _backingSession.CreateCriteria(persistentClass);
		}

		public ICriteria CreateCriteria(Type persistentClass, string alias)
		{
			return _backingSession.CreateCriteria(persistentClass, alias);
		}

		public ICriteria CreateCriteria(string entityName)
		{
			return _backingSession.CreateCriteria(entityName);
		}

		public ICriteria CreateCriteria(string entityName, string alias)
		{
			return _backingSession.CreateCriteria(entityName, alias);
		}

		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			return _backingSession.QueryOver<T>();
		}

		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			return _backingSession.QueryOver(alias);
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
		{
			return _backingSession.QueryOver<T>(entityName);
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
		{
			return _backingSession.QueryOver(entityName, alias);
		}

		public IQuery CreateQuery(string queryString)
		{
			return _backingSession.CreateQuery(queryString);
		}

		public IQuery CreateFilter(object collection, string queryString)
		{
			return _backingSession.CreateFilter(collection, queryString);
		}

		public IQuery GetNamedQuery(string queryName)
		{
			return _backingSession.GetNamedQuery(queryName);
		}

		public ISQLQuery CreateSQLQuery(string queryString)
		{
			return _backingSession.CreateSQLQuery(queryString);
		}

		public void Clear()
		{
			_backingSession.Clear();
		}

		public object Get(Type clazz, object id)
		{
			return _backingSession.Get(clazz, id);
		}

		public object Get(Type clazz, object id, LockMode lockMode)
		{
			return _backingSession.Get(clazz, id, lockMode);
		}

		public object Get(string entityName, object id)
		{
			return _backingSession.Get(entityName, id);
		}

		public T Get<T>(object id)
		{
			return _backingSession.Get<T>(id);
		}

		public T Get<T>(object id, LockMode lockMode)
		{
			return _backingSession.Get<T>(id, lockMode);
		}

		public string GetEntityName(object obj)
		{
			return _backingSession.GetEntityName(obj);
		}

		public IFilter EnableFilter(string filterName)
		{
			return _backingSession.EnableFilter(filterName);
		}

		public IFilter GetEnabledFilter(string filterName)
		{
			return _backingSession.GetEnabledFilter(filterName);
		}

		public void DisableFilter(string filterName)
		{
			_backingSession.DisableFilter(filterName);
		}

		public IMultiQuery CreateMultiQuery()
		{
			return _backingSession.CreateMultiQuery();
		}

		public ISession SetBatchSize(int batchSize)
		{
			return _backingSession.SetBatchSize(batchSize);
		}

		public ISessionImplementor GetSessionImplementation()
		{
			return _backingSession.GetSessionImplementation();
		}

		public IMultiCriteria CreateMultiCriteria()
		{
			return _backingSession.CreateMultiCriteria();
		}

		public ISession GetSession(EntityMode entityMode)
		{
			return _backingSession.GetSession(entityMode);
		}

		public EntityMode ActiveEntityMode
		{
			get { return _backingSession.ActiveEntityMode; }
		}

		public FlushMode FlushMode
		{
			get { return _backingSession.FlushMode; }
			set { _backingSession.FlushMode = value; }
		}

		public CacheMode CacheMode
		{
			get { return _backingSession.CacheMode; }
			set { _backingSession.CacheMode = value; }
		}

		public ISessionFactory SessionFactory
		{
			get { return _backingSession.SessionFactory; }
		}

		public IDbConnection Connection
		{
			get { return _backingSession.Connection; }
		}

		public bool IsOpen
		{
			get { return _backingSession.IsOpen; }
		}

		public bool IsConnected
		{
			get { return _backingSession.IsConnected; }
		}

		public bool DefaultReadOnly
		{
			get { return _backingSession.DefaultReadOnly; }
			set { _backingSession.DefaultReadOnly = value; }
		}

		public ITransaction Transaction
		{
			get { return _backingSession.Transaction; }
		}

		public ISessionStatistics Statistics
		{
			get { return _backingSession.Statistics; }
		}
		#endregion
	}
}