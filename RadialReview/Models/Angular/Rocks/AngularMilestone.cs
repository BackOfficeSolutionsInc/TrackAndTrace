using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;

namespace RadialReview.Models.Angular.Rocks
{
    public class AngularMilestone : BaseAngular
    {
        public AngularMilestone() { }
        public AngularMilestone(long id) : base(id) { }
        public AngularMilestone(Milestone milestone) : base(milestone.Id)
        {
            Name = milestone.Name;
            DueDate = milestone.DueDate;
            Status = milestone.Status;
            RockId = milestone.RockId;
        }
        public DateTime? DueDate { get; set; }
        public String Name { get; set; }
        public MilestoneStatus? Status { get; set; }
        public long? RockId { get; set; }
    }
}