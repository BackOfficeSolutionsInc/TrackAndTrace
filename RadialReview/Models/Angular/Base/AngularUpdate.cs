using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Angular.Base
{
	public class AngularUpdate : IAngularUpdate, IEnumerable
	{
		[ScriptIgnore]
		protected Dictionary<string, IAngularId> _Updates { get; private set; }

		public Dictionary<string, IAngularId> Lookup { get { return _Updates; } }

		public void Add(IAngularId item)
		{
			_Updates.Add(item.GetKey(),item);
		}

		public void Remove(IAngularId item)
		{
			_Updates.Add(item.GetKey(), null);
		
		}

		public AngularUpdate(){
			_Updates = new Dictionary<string, IAngularId>();
		}

		public IEnumerator GetEnumerator(){
			return _Updates.GetEnumerator();
		}
	}
}