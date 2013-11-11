using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class GroupViewModel
    {
        public long OrganizationId { get; set; }
        public DragDropViewModel DragDrop { get; set; }
        public GroupModel Group { get; set; }
        public QuestionsViewModel Questions { get; set; }
        
    }
}