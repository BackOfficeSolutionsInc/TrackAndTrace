using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Angular.Base
{
	public class AngularUpdate : IAngularUpdate, IEnumerable
	{
		[ScriptIgnore]
		protected Dictionary<string, IAngularItem> _Updates { get; private set; }

		public Dictionary<string, IAngularItem> Lookup { get { return _Updates; } }

		public void Add(IAngularItem item)
		{
			_Updates.Add(item.GetKey(),item);
		}

		public void Remove(IAngularItem item)
		{
			_Updates.Add(item.GetKey(), null);
		
		}

		public AngularUpdate(){
			_Updates = new Dictionary<string, IAngularItem>();
		}

		public IEnumerator GetEnumerator(){
			return _Updates.GetEnumerator();
		}
	}
}