using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.Survey {
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
    }
}