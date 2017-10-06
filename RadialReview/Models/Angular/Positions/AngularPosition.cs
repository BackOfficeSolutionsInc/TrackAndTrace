using RadialReview.Models.Angular.Base;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Positions {

    public class AngularPosition : BaseAngular {
        public AngularPosition(){
        }
        public AngularPosition(long id): base(id){
        }
        public AngularPosition(OrganizationPositionModel position):base(position.Id){
            Name = position.GetName();            
        }

        public string Name { get; set; }
		
    }
}