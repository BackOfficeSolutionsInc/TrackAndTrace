using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using Microsoft.AspNet.Identity;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RestSharp;

namespace RadialReview
{
	public enum LifeTime
	{
		Request,
		Session,
		[Obsolete("Use caution. Entire app.")]
		AppDomain

	}

	public class Cache
	{

		public HttpContextBase Context { get; set; }

		public class CacheContext{
			public String Key { get; set; }
			public LifeTime LifeTime { get; set; }
			public Object Data { get; set; }
			public CacheContext(String key, LifeTime lifeTime){
				Key = key;
				LifeTime = lifeTime;
			}
		}

		public Cache(HttpContextBase Context)
		{
			this.Context = Context;
		}

		public Cache():this(new HttpContextWrapper(HttpContext.Current)){}

		public class CacheItem{
			public DateTime? Expires { get; set; }
			public object Object { get; set; }
		}
		
		public object Get(String key){
			return _get(_currentUser(), key);
		}

		public V GetOrGenerate<V>(String key, Func<CacheContext, V> generator, Predicate<V> forceUpdate = null, CacheContext ctx = null)
		{
			var found = _get(_currentUser(), key);
			forceUpdate = forceUpdate ?? (x => false);

			if (!(found is V) || forceUpdate((V)found))
			{
				ctx = ctx ?? new CacheContext(key, LifeTime.Request);
				var gen = generator(ctx);
				if (gen != null){
					Push(key, gen, ctx.LifeTime);
				}
				return gen;
			}
			return (V)found;
		}
		
		public V Push<V>(String key, V value,LifeTime lifetime=LifeTime.Request,DateTime? expires=null)
		{
			key = _key(key);
			return _Push(key, value, lifetime, expires);
		}

		private V _Push<V>(String key, V value, LifeTime lifetime = LifeTime.Request, DateTime? expires = null)
		{
			var toCache = new CacheItem() { Object = value, Expires = expires };

			//Remove invalidation key
			var wasRemoved = Context.Cache.Remove(_invalidateKey(_currentUser(), key)) != null;

			switch (lifetime)
			{
				case LifeTime.Session: Context.Session[key] = toCache; goto case LifeTime.Request;
				case LifeTime.AppDomain: Context.Cache.Add(key, toCache, null, expires ?? DateTime.MaxValue, System.Web.Caching.Cache.NoSlidingExpiration, CacheItemPriority.Default, null); goto case LifeTime.Request;
				case LifeTime.Request: Context.Items[key] = toCache; break;
				default: throw new ArgumentOutOfRangeException("lifetime");
			}
			return value;
		}

		public bool Contains(String key){
			return _get(_currentUser(), key)!=null;
		}

		public void Invalidate(String key)
		{
			var iKey = _invalidateKey(_currentUser(), key);
			key = _key(key);
			Context.Items[key] = null;
			Context.Cache.Remove(key);
			Context.Cache.Remove(iKey);
			Context.Session[key] = null;
		}

		public void InvalidateForUser(string userId, String key)
		{
			var ikey = _invalidateKey(userId, key);
			_Push(ikey, true, LifeTime.Request/*AppDomain*/, null);
		}
		public void InvalidateForUser(UserOrganizationModel user, String key)
		{
			if (user.User != null){
				InvalidateForUser(user.User.Id, key);
			}
		}
		
		#region Private
		private string _key(string key){
			return  "__k_" + key;
		}
		private string _invalidateKey(string userId, string key)
		{
			return key + "__i_" + userId;
		}
		private string _currentUser(){
			return Context.User.Identity.GetUserId();
		}
		private void _performInvalidate(string userId, String key){
			var ikey = _invalidateKey(userId, key);
			var found = (CacheItem)Context.Cache[ikey];
			if (found != null && found.Object is bool && ((bool)found.Object)){
				Invalidate(key);
				Invalidate(ikey);
			}
		}
		private object _get(string userId,String key){
			_performInvalidate(userId, key);

			key = _key(key);
			var found = ((CacheItem)Context.Items[key]);
			if (found == null)
				found = ((CacheItem)Context.Cache[key]);
			if (found == null)
				found = ((CacheItem)Context.Session[key]);

			if (found != null && (found.Expires == null || found.Expires >= DateTime.UtcNow))
				return found.Object;
			return null;
		}
		#endregion
	}
}