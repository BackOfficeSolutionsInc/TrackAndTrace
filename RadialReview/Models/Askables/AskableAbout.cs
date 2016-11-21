using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;

namespace RadialReview.Models.Askables
{
    public class AskableAbout
    {
        public AboutType ReviewerIsThe { get; set; }
        public Reviewee Reviewee { get; set; }
        public Askable Askable { get; set; }

		public AskableAbout(Askable askable,Reviewee reviewee, AboutType reviewerIsThe) {
			Askable = askable;
			Reviewee = reviewee;
			ReviewerIsThe = reviewerIsThe;
		}
	}
}