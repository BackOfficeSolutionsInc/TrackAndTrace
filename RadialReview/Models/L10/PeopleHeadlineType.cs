using System.ComponentModel.DataAnnotations;

namespace RadialReview.Model.Enums {
	public enum PeopleHeadlineType {
		[Display(Name = "None")]
		None = 0,
		[Display(Name = "Headlines Box")]
		HeadlinesBox = 1,
		[Display(Name = "Headlines List")]
		HeadlinesList = 2,
	}
}