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
			_PrettyString = model.NotNull(x => x.ToPrettyString());
        }

        public long ModelId { get; set; }
        public string ModelType { get; set; }

		private string _PrettyString;


        public bool Is<T>() {
#pragma warning disable CS0618 // Type or member is obsolete
			return ForModel.GetModelType(typeof(T))==ModelType;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public string ToPrettyString() {
			return _PrettyString;
		}
	}
}