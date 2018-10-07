using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions {
    public class RedirectToActionException : Exception{
        public RedirectToActionException(string controller, string action) {
            Controller = controller;
            Action = action;
        }

        public String Controller { get; set; }
        public string Action { get; set; }

    }
}