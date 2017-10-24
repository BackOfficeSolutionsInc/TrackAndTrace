using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models.Enums {
    [Flags]
    public enum AboutType : long {
        NoRelationship = 0,
        Self = 1,
        Subordinate = 2,
        Teammate = 4,
        Peer = 8,
        Manager = 16,
        Organization = 32,
    }

    public static class AboutTypeExtensions {
        public static int Order(this AboutType self) {
            

            switch (self) {
                case AboutType.Self: return 1;
                case AboutType.Manager: return 2;
                default: return 0;
            }

        }

    }

}
