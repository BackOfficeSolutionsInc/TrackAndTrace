using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Interfaces {
	public interface IProcessDef {
		//string id { get; set; }
		//string key { get; set; }
		//string name { get; set; }
		//string description { get; set; }
		string category { get; set; }
		string deploymentId { get; set; }
		bool suspended { get; set; }

		string GetId();
		string Getkey();
		string Getname();
		string Getdescription();
	}
}
