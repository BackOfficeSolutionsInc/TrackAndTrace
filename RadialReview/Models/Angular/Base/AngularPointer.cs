using Newtonsoft.Json;
using System;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Angular.Base
{
	public class AngularPointer
	{
		public string Key {
			get { return Reference.GetKey(); }
		}
		//public bool Delete { get; set; }		
		[ScriptIgnore]
		[JsonIgnore]
		public DateTime LastUpdate { get; set; }

		public int _P {get { return 1; }}

		[ScriptIgnore]
		[JsonIgnore]
		public IAngularId Reference { get; set; }

		public AngularPointer(IAngularId reference, DateTime time/*,bool delete*/)
		{
			Reference = reference;
			LastUpdate = time;
			//Delete = delete;
		}
	}
}