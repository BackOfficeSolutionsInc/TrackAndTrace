using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels {
    public class ProfilePictureVM {
        public long UserId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Initials { get;set;}
    }
}