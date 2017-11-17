using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.Survey {

	[DebuggerDisplay("{GetBy()} => {GetAbout()}")]
	public class ByAbout : IByAbout {
        public IForModel By { get; set; }
        public IForModel About { get; set; }

        public ByAbout(IForModel by, IForModel about) {
            By = by;
            About = about;
        }

        public IForModel GetAbout() {
            return About;
        }

        public IForModel GetBy() {
            return By;
        }

		
		public override bool Equals(object obj) {
			var f = obj as IByAbout;
			if (f != null) {
				return f.ToKey() == this.ToKey();
			}
			return false;
		}
				
		public override int GetHashCode() {
			
			return this.ToKey().GetHashCode();
		}
    }
}