using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using Amazon.Auth.AccessControlPolicy;
using Amazon.EC2;
using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using Microsoft.Ajax.Utilities;
using RadialReview.Accessors;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;

namespace RadialReview.Utilities
{
	public partial class PermissionsUtility
	{


		
        public void UnsafeAllow(PermItem.AccessLevel level,PermItem.ResourceType resourceType,long id){
            string key;
            switch(level){
                case PermItem.AccessLevel.View : key = "CanView_"+resourceType+"~"+id;break;
                case PermItem.AccessLevel.Edit : key = "CanEdit_"+resourceType+"~"+id;break;
                case PermItem.AccessLevel.Admin : key = "CanAdmin_"+resourceType+"~"+id;break;
                default: throw new ArgumentOutOfRangeException(""+level.ToString());
            }
            new CacheChecker(key, this).Execute(()=>this);
            //this.cache[key] = new CacheResult() { };
        }
		
		/*protected class PAccess
		{
			public long AccessId { get; set; }
			public PermItem.AccessType AccessType { get; set; }

			public PAccess(long accessId, PermItem.AccessType accessType)
			{
				AccessId = accessId;
				AccessType = accessType;
			}
		}
		*/

		public void EnsureAdminExists(PermItem.ResourceType resourceType, long resourceId)
		{
			var items = session.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.CanAdmin && x.ResId == resourceId && x.ResType == resourceType).List();
			if (!items.Any())
				throw new PermissionsException("You must have an admin. Reverting setting change.");
			//Cheapest first..
			foreach (var i in items.OrderBy(x=>(int)x.AccessorType)){
				switch(i.AccessorType){
					case PermItem.AccessType.RGM:
						var users = ResponsibilitiesAccessor.GetResponsibilityGroupMembers(session, this, i.AccessorId);
						if (users.Any())
							return;
						break;
					case PermItem.AccessType.Members:
						var ids =GetMemeberIds(resourceType, resourceId);
						var idsAlive = session.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).Select(x => x.Id).List<long>();
						if (idsAlive.Any())
							return;
						break;
					case PermItem.AccessType.Creator:
						var creator =session.Get<UserOrganizationModel>(i.AccessorId);
						if (creator.DeleteTime == null)
							return;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			throw new PermissionsException("You must have an admin. Reverting setting change.");
		}
		public PermissionsUtility CanViewPermissions(PermItem.ResourceType resourceType, long resourceId)
		{
			return CheckCacheFirst("CanViewPermissions_" + resourceType, resourceId).Execute(() =>
			{
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.View, resourceType, resourceId, null, ref result))
					return result;
				throw new PermissionsException("Can not view this item.");
			});
		}


		public PermissionsUtility CanView(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null)
		{
			return CheckCacheFirst("CanView_" + resourceType, resourceId).Execute(() =>
			{
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.View, resourceType, resourceId, defaultAction, ref result))
					return result;
				throw new PermissionsException("Can not view this item.");
			});
		}

		public PermissionsUtility CanEdit(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null)
		{
			return CheckCacheFirst("CanEdit_" + resourceType, resourceId).Execute(() =>
			{
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.Edit, resourceType, resourceId, defaultAction, ref result))
					return result;

				throw new PermissionsException("Can not edit this item.");
			});
		}
		public PermissionsUtility CanAdmin(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null)
		{
			return CheckCacheFirst("CanAdmin_" + resourceType, resourceId).Execute(() =>
			{
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.Admin, resourceType, resourceId, defaultAction, ref result))
					return result;

				throw new PermissionsException("Can not administrate this item.");
			});
		}

		protected bool CanAccessItem(PermItem.AccessLevel level, PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction, ref PermissionsUtility result, bool and = true)
		{
			if (IsRadialAdmin(caller))
				return true;

			var permItems = session.QueryOver<PermItem>().Where(x => x.ResId == resourceId && x.ResType == resourceType && x.DeleteTime == null).List().ToList();

			//Only want true if we actually have permissions...
			if (!permItems.Any()){
				//This might be redundant with the !anyFlags check.
				if (defaultAction != null){
					result = defaultAction(this);
					return true;
				}
				return false;
			}

			List<ResponsibilityGroupModel> groups = null;
			var anyFlags = false;

			foreach(var flag in new[]{PermItem.AccessLevel.View,PermItem.AccessLevel.Edit,PermItem.AccessLevel.Admin})
			{
				/*Ordered by cheapest first...*/
				if (level.HasFlag(flag)){
					//only want to return if we handled some flags.
					anyFlags = true;
					var currentTrue = false;
					//Special UserIds
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.RGM && x.AccessorId == caller.Id));
					
					//Is creator
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.Creator && IsCreator(resourceType, resourceId)));
					//Is a Member
					currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.Members && IsMember(resourceType, resourceId)));
					//Special Teams/Positions/Etc (only call if necessary)
					if (!currentTrue && permItems.Any(x =>x.HasFlags(flag) && x.AccessorType == PermItem.AccessType.RGM  && x.AccessorId != caller.Id)){
						//Expensive, only call once.
						groups = groups ?? ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(session.ToQueryProvider(true), this, caller, caller.Id);
						currentTrue = currentTrue || permItems.Any(pItem => pItem.HasFlags(flag) && pItem.AccessorType == PermItem.AccessType.RGM && groups.Any(group => pItem.AccessorId == group.Id));
					}
					if (!currentTrue && and)
						return false;

					if (currentTrue && !and)
						return true;
				}
			}

			if (!anyFlags) 
				return false;

			//Everything passed.
			if (and)
				return true;
			else
				return false;
		}

		protected List<long> GetMemeberIds(PermItem.ResourceType resourceType, long resourceId)
		{
			switch (resourceType)
			{
				case PermItem.ResourceType.L10Recurrence:
					var isMember_ids = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.L10Recurrence.Id == resourceId && x.DeleteTime == null && x.User.Id == caller.Id).Select(x => x.User.Id).List<long>().ToList();
					return isMember_ids;
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
		}

		protected bool IsMember(PermItem.ResourceType resourceType, long resourceId)
		{
			var isMember_ids = GetMemeberIds(resourceType, resourceId);
			if (isMember_ids.Any(id=>id==caller.Id))
				return true;
			return false;
		}
		protected bool IsCreator(PermItem.ResourceType resourceType, long resourceId)
		{
			switch (resourceType)
			{
				case PermItem.ResourceType.L10Recurrence:
					if (session.Get<L10Recurrence>(resourceId).CreatedById == caller.Id)
						return true;
					break;
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}
			return false;
		}

		/*
		protected List<PAccess> GetPAccess(PermItem.ResourceType resourceType, long resourceId)
		{

			var o = new List<PAccess>();

			switch (resourceType)
			{
				case PermItem.ResourceType.L10:
					var isMember_ids = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.L10Recurrence.Id == resourceId && x.DeleteTime == null && x.User.Id== caller.Id).Select(x => x.User.Id).List<long>().ToList();
					o.AddRange(isMember_ids.Select(id => new PAccess(id, PermItem.AccessType.Members)));
					o.Add(new PAccess(session.Get<L10Recurrence>(resourceId).CreatedById,PermItem.AccessType.Creator));
					break;
				default:
					throw new ArgumentOutOfRangeException("resourceType");
			}

			return o;
		}*/


        public PermissionsUtility CanUpload()
        {
            if (caller == null || caller.DeleteTime != null)
                throw new PermissionsException("You cannot upload documents");
            return this;
        }
    }
}