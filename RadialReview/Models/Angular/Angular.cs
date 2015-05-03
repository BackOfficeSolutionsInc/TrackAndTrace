using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Angular
{
	public class Angular : IAngularItem
	{
		public long Id { get; set; }
		public string Type {
			get { return GetType().Name; }
		}

		public Angular(long id){
			Id = id;
		}
		/*public long Id { get; set; }
		public long? Order { get; set; }
		public Dictionary<string, Angular> Lookup { get; private set; }
		
		public Angular(long id,Angular parent)
		{
			Id = id;
			_Parent = parent;
			if (parent != null){
				while (_Parent._Parent != null){
					_Parent = _Parent._Parent;
				}
			}
			Lookup = new Dictionary<string, Angular>();
		}

		#region Generated
		public string Key {get { return Type + "_" + Id; }}
		public string Type {get { return GetType().Name; }}
		#endregion

		
		private Angular _Parent;
		internal void AddItem(Angular item)
		{
			if (_Parent==null)
				Lookup.Add(item.Key,item);
			else
				_Parent.AddItem(item);
		}
		internal void RemoveItem(Angular item)
		{
			//Keep it in the lookup in case we have mulitple references

			/*if (_Parent == null)
				Lookup[item.Key] = new Removed(_Parent);
			else
				_Parent.RemoveItem(item);*
		}
		*/
	
	}

	public class Removed : Angular
	{
		public Removed() : base(0) { }
	}
}