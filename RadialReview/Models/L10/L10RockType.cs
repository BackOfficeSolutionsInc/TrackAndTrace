using System.ComponentModel.DataAnnotations;

namespace RadialReview.Model.Enums {
	public enum L10RockType {
		[Display(Name = "No Milestones")]
		Original = 0,
		[Display(Name = "Milestones")]
		Milestones = 1,
	}
}