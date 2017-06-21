using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
	public class CommClass : ICommClass {
		public IProcessDef GetProcessDefByKey(string key) {

			// Call API and get JSON
			// Serialize JSON into IProcessDef

			return new ProcessDef();
		}		
	}

	public class ProcessDef : IProcessDef {
		public string category {
			get {
				throw new NotImplementedException();
			}

			set {
				throw new NotImplementedException();
			}
		}

		public string deploymentId {
			get {
				throw new NotImplementedException();
			}

			set {
				throw new NotImplementedException();
			}
		}

		public bool suspended {
			get {
				throw new NotImplementedException();
			}

			set {
				throw new NotImplementedException();
			}
		}

		public string Id { get; set; }
		public string description { get; set; }
		public string key { get; set; }
		public string name { get; set; }
		public string Getdescription() {
			return description;
		}
		public string GetId() {
			return Id;
		}
		public string Getkey() {
			return key;
		}
		public string Getname() {
			return name;
		}
	}
}