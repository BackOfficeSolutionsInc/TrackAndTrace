using RadialReview.Models.Enums;

namespace RadialReview.Models.Askables
{
    public class AskableAbout
    {
        public Askable Askable { get; set; }
        public long AboutUserId { get; set; }
        public AboutType AboutType { get; set; }
    }
}