using RadialReview.Models.Angular.Base;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Angular.Survey {
    public class AngularForModel : IForModel {
        public AngularForModel() { }
        public AngularForModel(IForModel model) {
            ModelId = model.NotNull(x=>x.ModelId);
            ModelType = model.NotNull(x => x.ModelType);
        }

        public long ModelId { get; set; }

        public string ModelType { get; set; }

        public bool Is<T>() {
            return ForModel.GetModelType(typeof(T))==ModelType;
        }

		public string ToPrettyString() {
			return "";
		}
	}
}