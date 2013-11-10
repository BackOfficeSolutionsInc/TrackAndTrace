using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class DragDropViewModel
    {
        public String StartName { get; set; }
        public String EndName { get; set; }

        public virtual List<DragDropItem> Start { get;set;}
        public virtual List<DragDropItem> End { get; set; }
        public string DragItems { get; set; }
        public string DropItems { get; set; }

        public DragDropViewModel(){
            Start = new List<DragDropItem>();
            End = new List<DragDropItem>();
        }
    }

    public class DragDropItem{


        public long Id { get; set; }
        public String DisplayName { get; set; }
        public String AltText { get; set; }
        public String ImageUrl { get; set; }
        public String Classes { get; set; }
    }
}