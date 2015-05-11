using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using NHibernate.Type;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.Extensions;

namespace RadialReview.Models.Angular.Base
{
	public class Angularizer : BaseAngular
	{
		public Dictionary<string, object> ToSerialize { get; private set; }

		public Angularizer(long id) : base(id)
		{
			ToSerialize = new Dictionary<string, object>();
		}

		public static Angularizer<T> Create<T>(T baseObject) where T : ILongIdentifiable
		{
			return new Angularizer<T>(baseObject);
		}
	}

	public class Angularizer<T> : Angularizer where T : ILongIdentifiable
	{
		protected T Backing { get; set; }

		public Angularizer(T obj) : base(obj.Id)
		{
			Backing = obj;
		}
		public Angularizer Add<TProp>(Expression<Func<T, TProp>> property){
			return Add(property.GetMemberName(), property.Compile());
		}

		public Angularizer Add<TProp>(string name, Func<T, TProp> property)
		{
			return Add(name,property(Backing));
		}

		public Angularizer Add<TProp>(string name, TProp value)
		{
			if (!ToSerialize.ContainsKey(name))
				ToSerialize[name] = value;
			else 
				throw new SerializationException("Key already exists:"+name);
			return this;
		}


	}
}