using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities.Attributes;

namespace RadialReview.Models.Enums
{
    public enum ThumbsType 
    {

        None=0,
		[Icon(BootstrapGlyphs.thumbs_up)]
		Up = 1,
		[Icon(BootstrapGlyphs.thumbs_down)]
        Down=2

    }
}