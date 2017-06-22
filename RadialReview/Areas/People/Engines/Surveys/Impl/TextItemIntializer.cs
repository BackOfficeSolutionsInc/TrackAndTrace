using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Impl {
	public class TextItemIntializer : IItemInitializer {
		public string Name { get; set; }
		public bool Disabled { get; set; }
		public TextItemIntializer(string name,bool disabled) {
			Name = name;
			Disabled = disabled;
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			var objs = new List<KV>();
			if (Disabled)
				objs.Add(new KV("disabled", true));

			return ctx.RegistrationItemFormat(false, () => new SurveyItemFormat(ctx, SurveyItemType.Text,objs));
		}

		public bool HasResponse(IResponseInitializerCtx ctx) {
			return false;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, Name, null);
		}

		public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
			throw new NotImplementedException();
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}

}