using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ViewModels
{
    public class UserPositionViewModel
    {
        public long PositionId { get; set; }
        public long UserId { get; set; }
        public List<SelectListItem> OrgPositions { get; set; }
        public List<SelectListItem> Positions { get; set; }
        public String CustomPosition { get; set; }
        public long CustomPositionId { get; set; }
    }
}