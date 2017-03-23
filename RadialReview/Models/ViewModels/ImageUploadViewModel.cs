using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ImageUploadViewModel {
		public String ImageUrl { protected get; set; }
		public String DeleteUrl { get; set; }
		public String ForType { get; set; }

		public string UploadUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public String GetUrl()
        {
            var dim = "";
            /*if (Width != 0 && Height != 0)
                dim= "?dim="+Width + "x" + Height;*/
            return ImageUrl + dim;

        }

    }
}