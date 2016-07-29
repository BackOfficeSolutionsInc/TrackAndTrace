using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public class AccountabilityTree : TreeModel<AccountabilityTree> , IAngularItem {
        public IEnumerable<AngularRole> roles { get; set; }
        public AngularUser user { get; set; } 


        //public long? parentId { get; set; }
        //public new List<AccountabilityTree> children { get; set; }
       
        //protected static AccountabilityTree Dive<T>(TreeModel<T> tree,long? pId,LONG id) where T : TreeModel<T>
        //{
        //    List<AngularRole> roles = null;
        //    if (tree.data != null && tree.data.ContainsKey("Roles") && tree.data["Roles"] is IEnumerable<RoleModel>) {
        //        var dataRoles = (IEnumerable<RoleModel>)tree.data["Roles"];
        //        roles = dataRoles.Select(x => new AngularRole(x)).ToList();
        //        tree.data["Roles"] = null;                
        //    }
        //    var children = new List<AccountabilityTree>();

        //    if (tree.children != null) {
        //        foreach (var c in tree.children) {
        //            children.Add(Dive(c, tree.id, id));
        //        }
        //    }

        //    id.id += 1;

        //    return new AccountabilityTree() {
        //        id = id.id,
        //        name = tree.name,
        //        data = tree.data,
        //        @class = tree.@class,
        //        subtext = tree.subtext,
        //        manager = tree.manager,
        //        children = children,
        //        managing = tree.managing,
        //        roles = roles,
        //        //parentId = pId
        //    };
        //}

        //public static AccountabilityTree From<T>(TreeModel<T> tree) where T : TreeModel<T>
        //{
        //    var id = new LONG();
        //    return Dive(tree,null,id);
        //}

        public long Id{get { return this.id; }}

        public string Type{get { return "AccountabilityTree"; }}

        public bool Hide{get { return false; }}
    }
}