using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ImageUploadViewModel
    {
        public String Url {protected get; set; }
        public String ForType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public String GetUrl()
        {
            var dim = "";
            if (Width != 0 && Height != 0)
                dim= "?dim="+Width + "x" + Height;
            return Url + dim;

        }

    }
}