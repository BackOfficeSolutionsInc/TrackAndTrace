using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Transformers {
	public class TransformByAboutNoop : ITransformByAbout {
		public IEnumerable<IByAbout> TransformForCreation(IEnumerable<IByAbout> byAbouts) {
			return byAbouts;
		}

		public IEnumerable<IByAbout> ReconstructTransform(IEnumerable<IByAbout> byAbouts) {
			return byAbouts;
		}
	}
}