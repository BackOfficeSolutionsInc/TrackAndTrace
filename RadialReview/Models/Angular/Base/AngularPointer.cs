using System;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Angular.Base
{
	public class AngularPointer
	{
		public string Key {
			get { return Reference.GetKey(); }
		}
		public bool Delete { get; set; }
		public DateTime LastUpdate { get; set; }
		public bool _Pointer {get { return true; }}

		[ScriptIgnore]
		public IAngularItem Reference { get; set; }

		public AngularPointer(IAngularItem reference,DateTime time,bool delete)
		{
			Reference = reference;
			Delete = delete;
			LastUpdate = time;
		}
	}
}