using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;

namespace RadialReview.Models.Angular.Team
{
    public class AngularTeam : BaseAngular
    {
        public AngularTeam() { }
        public AngularTeam(long id):base(id) { }      
        public AngularTeam(OrganizationTeamModel model) : base(model.Id)
		{
            Name = model.Name;
            ManagedBy = model.ManagedBy;
            TeamType = model.Type;                        
        }
        public string Name { get; set; }
        public long? ManagedBy { get; set; }
        public TeamType? TeamType { get; set; }
    }
}