﻿using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Accountability {

    public class AngularAccountabilityChart : BaseAngular {
        
        public AngularAccountabilityChart(){
        }
        public AngularAccountabilityChart(long id): base(id)
        {
        }
        public AngularAccountabilityNode Root { get;set; }
        public IEnumerable<AngularUser> AllUsers { get; set; }
        public long? CenterNode { get; set; }

    }

    public class AngularAccountabilityNode : AngularTreeNode<AngularAccountabilityNode> {
        public AngularAccountabilityNode(){
        }
        public AngularAccountabilityNode(long id):base(id){
        }
        public AngularAccountabilityNode(AccountabilityNode node,bool collapse=false) : base(node.Id){
            User = node.User.NotNull(x=>AngularUser.CreateUser(node.User));
            Group = node.AccountabilityRolesGroup.NotNull(x => new AngularAccountabilityGroup(x));
            
            var childrens = node._Children.NotNull(x => x.Select(y => new AngularAccountabilityNode(y)).ToList());

            if (collapse)
                _children = childrens;
            else
                children = childrens;

        }

        public AngularUser User { get; set; }
        public AngularAccountabilityGroup Group { get; set; }     

    }
    public class AngularAccountabilityGroup : BaseAngular {  
        public AngularAccountabilityGroup(){
        }
        public AngularAccountabilityGroup(long id):base(id){
        }
        public AngularAccountabilityGroup(AccountabilityRolesGroup group) : base(group.Id){
            Roles = group._Roles.NotNull(x => x.Select(y => y.Role).Where(y=>y!=null).Select(y=>new AngularRole(y)).ToList());
            Position = group.Position.NotNull(x=>new AngularPosition(x));
        }

        public AngularPosition Position { get; set; }
        public IEnumerable<AngularRole> Roles { get; set; }


    }
}