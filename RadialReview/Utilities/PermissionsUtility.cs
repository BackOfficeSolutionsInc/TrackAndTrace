﻿using System.Linq.Expressions;
using log4net;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.UserModels;
using RadialReview.Models.Prereview;

namespace RadialReview.Utilities
{
    //[Obsolete("Not really obsolete. I just want this to stick out.", false)]
    public class PermissionsUtility
    {
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected ISession session;
        protected UserOrganizationModel caller;

        public PermissionsUtility RadialAdmin()
        {
            if (IsRadialAdmin(caller))
                return this;
            throw new PermissionsException();
        }
        protected static Boolean IsRadialAdmin(UserOrganizationModel caller)
        {
            if (caller != null && (caller.IsRadialAdmin || (caller.User != null && caller.User.IsRadialAdmin)))
                return true;
            return false;
        }
                
        #region Construction
        protected PermissionsUtility(ISession session, UserOrganizationModel caller)
        {
            this.session = session;
            this.caller = caller;
        }

        public static PermissionsUtility Create(ISession session, UserOrganizationModel caller)
        {
            var attached = caller;
            if (!session.Contains(caller) && caller.Id!=UserOrganizationModel.ADMIN_ID)
                attached = session.Get<UserOrganizationModel>(caller.Id);
            return new PermissionsUtility(session, attached);
        }
        #endregion

        #region User
        public PermissionsUtility EditUserModel(string userId)
        {
            if (IsRadialAdmin(caller))
                return this;

            if (caller.User.Id == userId)
                return this;
            throw new PermissionsException();
        }
        public PermissionsUtility EditUserOrganization(long userId)
        {
            if (caller.Id == userId)
                return this;

            return ManagesUserOrganization(userId,false);

            /*
            if (IsRadialAdmin(caller))
                return this;

            var user = session.Get<UserOrganizationModel>(userId);
            if (IsManagingOrganization(user.Organization.Id))
                return this;

            if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.Id == userId))
                return this;
            //caller.AllSubordinates.Any(x => x.Id == userId) && caller.IsManager()) //IsManager may be too much
            //return this;
            //Could do some cascading here if we want.

            throw new PermissionsException("You don't manage this user.");*/
        }
        public PermissionsUtility ViewUserOrganization(long userOrganizationId, Boolean sensitive)
        {
            if (IsRadialAdmin(caller))
                return this;
            var userOrg = session.Get<UserOrganizationModel>(userOrganizationId);

            if (IsManagingOrganization(userOrg.Organization.Id))
                return this;

            if (sensitive)
            {
                /*if (!userOrg.Organization.StrictHierarchy && userOrg.Organization.Id == caller.Organization.Id)
                    return this;*/

                if (IsOwnedBelowOrEqual(caller, x => x.Id == userOrganizationId))
                    return this;
            }
            else
            {
                if (userOrg.Organization.Id == caller.Organization.Id)
                    return this;
            }

            throw new PermissionsException();
        }
        public PermissionsUtility ManagesUserOrganization(long userOrganizationId,bool disableIfSelf)
        {
            if (IsRadialAdmin(caller))
                return this;
            //Confirm allowed if we manage organization.. was below
            if (caller.ManagingOrganization)
            {
                var subordinate = session.Get<UserOrganizationModel>(userOrganizationId);
                if (subordinate != null && subordinate.Organization.Id == caller.Organization.Id)
                    return this;
            }

            if (disableIfSelf && caller.Id == userOrganizationId)
                throw new PermissionsException("You cannot do this to yourself.");

            //..was here

            //Confirm this is correct. Do you want children to also be managers?
            if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.Id == userOrganizationId))
                return this;
            throw new PermissionsException();
        }
        #endregion

        #region Organization
        public PermissionsUtility EditOrganization(long organizationId)
        {
            if (IsRadialAdmin(caller))
                return this;

            if (caller.Organization.Id == organizationId && caller.IsManagerCanEditOrganization())
                return this;
            throw new PermissionsException();
        }

		[Obsolete("should never be caller.organization.id",false)]
		private bool IsManagingOrganization(long organizationId,bool allowManagers=false)
		{
			if (caller.Organization.Id == organizationId)
				return caller.ManagingOrganization || (allowManagers && caller.ManagerAtOrganization && caller.Organization.ManagersCanEdit);
			return false;
		}
		private bool IsManager(long organizationId)
		{
			if (caller.Organization.Id == organizationId)
				return caller.ManagingOrganization || caller.ManagerAtOrganization;
			return false;
		}
        public PermissionsUtility ViewOrganization(long organizationId)
        {
            if (IsRadialAdmin(caller))
                return this;
            if (caller.Organization.Id == organizationId)
                return this;
            throw new PermissionsException();
        }

        #endregion

        #region Group
        public PermissionsUtility EditGroup(long groupId)
        {
            if (IsRadialAdmin(caller))
                return this;

            if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.ManagingGroups.Any(y => y.Id == groupId)))
                return this;

            throw new PermissionsException();
        }

        public PermissionsUtility ViewGroup(long groupId)
        {
            if (IsRadialAdmin(caller))
                return this;
            if (caller.Groups.Any(x => x.Id == groupId))
                return this;
            if (IsOwnedBelowOrEqual(caller, x => x.ManagingGroups.Any(y => y.Id == groupId)))
                return this;
            throw new PermissionsException();
        }
        #endregion

        #region Application
        public PermissionsUtility EditApplication(long forId)
        {
            if (IsRadialAdmin(caller))
                return this;
            throw new PermissionsException();
        }
        public PermissionsUtility ViewApplication(long applicationId)
        {
            log.Info("ViewApplication always returns true.");
            return this;
        }
        #endregion

        #region Industry
        public PermissionsUtility EditIndustry(long forId)
        {
            if (IsRadialAdmin(caller))
                return this;
            throw new PermissionsException();
        }
        public PermissionsUtility ViewIndustry(long industryId)
        {
            log.Info("ViewIndustry always returns true.");
            return this;
        }
        #endregion

        #region Question
        public PermissionsUtility EditQuestion(QuestionModel question)
        {
            if (IsRadialAdmin(caller))
                return this;

            var createdById = question.CreatedById;

            if (caller.IsManager() && IsOwnedBelowOrEqual(caller, x => x.Id == createdById))
                return this;

            throw new PermissionsException();
        }

        public PermissionsUtility ViewQuestion(QuestionModel question)
        {
            if (IsRadialAdmin(caller))
                return this;

            switch (question.OriginType)
            {
                case OriginType.User: if (!IsOwnedBelowOrEqual(caller, x => x.CustomQuestions.Any(y => y.Id == question.Id))) throw new PermissionsException(); break;
                case OriginType.Group: if (!IsOwnedBelowOrEqual(caller, x => x.Groups.Any(y => y.CustomQuestions.Any(z => z.Id == question.Id)) || x.ManagingGroups.Any(y => y.CustomQuestions.Any(z => z.Id == question.Id)))) throw new PermissionsException(); break;
                case OriginType.Organization: if (caller.Organization.Id != question.OriginId) throw new PermissionsException(); break;
                case OriginType.Industry: break;
                case OriginType.Application: break;
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
            return this;
        }
        #endregion

        #region Origin
        public PermissionsUtility EditOrigin(Origin origin)
        {
            return EditOrigin(origin.OriginType, origin.OriginId);
        }

        public PermissionsUtility EditOrigin(OriginType origin, long originId)
        {
            switch (origin)
            {
                case OriginType.User: return EditUserOrganization(originId);
                case OriginType.Group: return EditGroup(originId);
                case OriginType.Organization: return EditOrganization(originId);
                case OriginType.Industry: return EditIndustry(originId);
                case OriginType.Application: return EditApplication(originId);
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
        }
        public PermissionsUtility ViewOrigin(OriginType originType, long originId)
        {
            switch (originType)
            {
                case OriginType.User: return ViewUserOrganization(originId, false);
                case OriginType.Group: return ViewGroup(originId);
                case OriginType.Organization: return ViewOrganization(originId);
                case OriginType.Industry: return ViewIndustry(originId);
                case OriginType.Application: return ViewApplication(originId);
                case OriginType.Invalid: throw new PermissionsException();
                default: throw new PermissionsException();
            }
        }
        #endregion

        #region Category
        public PermissionsUtility ViewCategory(long id)
        {
            if (IsRadialAdmin(caller))
                return this;

            var category = session.Get<QuestionCategoryModel>(id);
            if (category.OriginType == OriginType.Application)
                return this;

            if (category.OriginType == OriginType.Organization && IsOwnedBelowOrEqualOrganizational(caller.Organization, new Origin(category.OriginType, category.OriginId)))
                return this;

            throw new PermissionsException();
        }
        public PermissionsUtility PairCategoryToQuestion(long categoryId, long questionId)
        {
            if (IsRadialAdmin(caller))
                return this;

            var category = session.Get<QuestionCategoryModel>(categoryId);
            if (questionId == 0 && category.OriginType == OriginType.Organization)
                return this;

            var question = session.Get<QuestionModel>(questionId);

            //Cant attach questions to application categories
            if (category.OriginType == OriginType.Application && !caller.IsRadialAdmin)
                throw new PermissionsException();
            //Belongs to the same organization
            if (category.OriginType == OriginType.Organization && question.OriginType == OriginType.Organization && question.OriginId == category.OriginId)
                return this;

            //TODO any other special permissions here.

            throw new PermissionsException();
        }
        #endregion

        #region Managers
        public PermissionsUtility ManagerAtOrganization(long userOrganizationId, long organizationId)
        {
            var user = session.Get<UserOrganizationModel>(userOrganizationId);
            //var org = session.Get<OrganizationModel>(organizationId);

            if (user.Organization.Id == organizationId && (user.ManagerAtOrganization || user.ManagingOrganization))
                return this;

            throw new PermissionsException();
        }
        public PermissionsUtility ManageUserReview(long reviewId, bool userCanManageOwnReview)
        {
            ViewReview(reviewId);
            var review = session.Get<ReviewModel>(reviewId);
            var userId = review.ForUserId;

			if (userCanManageOwnReview && review.ForUserId == caller.Id)
				return this;
			
			return ManagesUserOrganization(userId,false);
        }
        public PermissionsUtility ManagingOrganization()
        {
            if (IsRadialAdmin(caller))
                return this;

            if (IsManagingOrganization(caller.Organization.Id,true))
                return this;

            throw new PermissionsException();
        }

        #endregion

        #region Teams
        public PermissionsUtility ViewTeam(long teamId)
        {
            // Subordinates Team
            //if (teamId == -5 && caller.IsManager()) 
            //    return this;

            if (IsRadialAdmin(caller))
                return this;

            var team = session.Get<OrganizationTeamModel>(teamId);


            if (team == null)
                throw new PermissionsException();


            if (!team.Secret && team.Organization.Id == caller.Organization.Id)//&& team.Members.Any(x => x.UserOrganization.Organization.Id == caller.Organization.Id))
                return this;


            if (team.Secret && (team.CreatedBy == caller.Id || team.ManagedBy == caller.Id))
                return this;

            var members = session.QueryOver<TeamDurationModel>().Where(x => x.TeamId == teamId && x.UserId == caller.Id).List().ToList();
            if (team.Secret && members.Any())
                return this;

			//if (team.Secret && IsOwnedBelowOrEqual(caller, x => (team.CreatedBy == x.Id || team.ManagedBy == x.Id)))
		    //   return this;


            throw new PermissionsException();
        }

        public PermissionsUtility EditTeam(long teamId)
        {
            if (IsRadialAdmin(caller))
                return this;

            //Creating
            if (teamId == 0 && caller.IsManager())
                return this;

            //if (teamId == -5 && caller.IsManager()) // Subordinates Team
            //    return this;

            var team = session.Get<OrganizationTeamModel>(teamId);
            if (team.Type != TeamType.Standard)
                throw new PermissionsException("Cannot edit auto-populated team.");

            if (caller.IsManager() || !team.OnlyManagersEdit)
            {
                if (team.Organization.Id == caller.Organization.Id)
                {
                    if (!team.Secret)// && team.Members.Any(x => x.UserOrganization.Organization.Id == caller.Organization.Id))
                        return this;


                    if (team.Secret && (team.CreatedBy == caller.Id || team.ManagedBy == caller.Id))
                        return this;

                    if (!team.OnlyManagersEdit)
                    {
                        var members = session.QueryOver<TeamDurationModel>().Where(x => x.TeamId == teamId && x.UserId == caller.Id).List().ToList();
                        if (team.Secret && members.Any())
                            return this;
                    }
                    /*return this;*/
                }
            }

            throw new PermissionsException();
        }
        public PermissionsUtility ManagingTeam(long teamId)
        {
            if (IsRadialAdmin(caller))
                return this;

            //if (teamId == -5 && caller.IsManager())
            //    return this;

            var team = session.Get<OrganizationTeamModel>(teamId);

            if (IsManagingOrganization(team.Organization.Id,true))
                return this;

            if (team.OnlyManagersEdit && team.ManagedBy == caller.Id)
                return this;

            var members = session.QueryOver<TeamDurationModel>().Where(x => x.Team.Id == teamId).List().ToListAlive();

            if (!team.OnlyManagersEdit && members.Any(x => x.User.Id == caller.Id))
                return this;

            throw new PermissionsException();
        }
        #endregion
               
        #region Review
        public PermissionsUtility EditReviewContainer(long reviewContainerId)
        {
            //TODO more permissions here?
            if (IsRadialAdmin(caller))
                return this;

            var review = session.Get<ReviewsModel>(reviewContainerId);
            if (review.CreatedById == caller.Id)
                return this;

            var team = session.Get<OrganizationTeamModel>(review.ForTeamId);
            if (team.ManagedBy == caller.Id)
                return this;

            ManagingOrganization();

            return this;

            //ManagerAtOrganization(caller.Id, caller.Organization.Id);
            //return this;
        }

        public PermissionsUtility EditReview(long reviewId)
        {
            //TODO more permissions here?
            if (IsRadialAdmin(caller))
                return this;
            var review = session.Get<ReviewModel>(reviewId);
            if (review.DueDate < DateTime.UtcNow)
                throw new PermissionsException("Review period has expired.");
            if (review.ForUserId == caller.Id)
                return this;

            throw new PermissionsException();
        }
        
        public PermissionsUtility ViewReviews(long reviewContainerId, bool sensitive)
        {
            if (IsRadialAdmin(caller))
                return this;
            var review = session.Get<ReviewsModel>(reviewContainerId);
            var orgId = review.ForOrganization.Id;
			if (sensitive)
				ManagerAtOrganization(caller.Id, orgId);
            if (orgId == caller.Organization.Id)
                return this;



            /*
            if(IsOwnedBelowOrEqual(caller,x=>x.Id==review.CreatedById))
                return this;*/

            throw new PermissionsException();
        }
        
        public PermissionsUtility ViewReview(long reviewId)
        {
            if (IsRadialAdmin(caller))
                return this;

            var review = session.Get<ReviewModel>(reviewId);
            var reviewUserId = review.ForUserId;

            //Is this correct?
            if (IsManagingOrganization(review.ForUser.Organization.Id))
                return this;

            //Cannot be viewed by the user
            if (reviewUserId == caller.Id)
                return this;

            if (IsOwnedBelowOrEqual(caller, x => x.Id == reviewUserId))
                return this;

            throw new PermissionsException();


        }
        #endregion

        #region Responsbility
        public PermissionsUtility EditResponsibility(long responsibilityId)
        {
            if (IsRadialAdmin(caller))
                return this;

            var r = session.Get<ResponsibilityModel>(responsibilityId);
            var rGroupId = r.ForResponsibilityGroup;
            ResponsibilityGroupModel rGroup = session.Get<ResponsibilityGroupModel>(rGroupId);

            if (rGroup is OrganizationModel)
            {
                return EditOrganization(rGroupId);
            }
            else if (rGroup is OrganizationTeamModel)
            {
                return EditTeam(rGroupId);
            }
            else if (rGroup is UserOrganizationModel)
            {
                return EditUserOrganization(rGroupId);
            }
            else
            {
                throw new PermissionsException("Unknown responsibility group type.");
            }

        }

        #endregion

        #region Position
        public PermissionsUtility ManagingPosition(long positionId)
        {
            if (IsRadialAdmin(caller))
                return this;

            if (positionId == 0)
                return this;

            var position = session.Get<OrganizationPositionModel>(positionId);

            if (IsManagingOrganization(position.Organization.Id,true))
                return this;

            if (caller.Organization.ManagersCanEditPositions && caller.ManagerAtOrganization && position.Organization.Id == caller.Organization.Id)
                return this;

            throw new PermissionsException();
        }
        public PermissionsUtility EditPositions(long organizationId)
        {
            if (IsRadialAdmin(caller))
                return this;

			if (IsManagingOrganization(organizationId,true))
                return this;

            if (caller.Organization.ManagersCanEditPositions && caller.ManagerAtOrganization)
                return this;

            throw new PermissionsException();
        }

        #endregion

        #region Prereview
        public PermissionsUtility ViewPrereview(long prereviewId)
        {
            if (IsRadialAdmin(caller))
                return this;

            var prereview=session.Get<PrereviewModel>(prereviewId);
	        var prereviewOrgId = session.Get<ReviewsModel>(prereview.ReviewContainerId).ForOrganizationId;

			if (IsManagingOrganization(prereviewOrgId))
                return this;

            if (IsOwnedBelowOrEqual(caller, x => x.Id == prereview.ManagerId))
                return this;

            throw new PermissionsException();
        }

        #endregion

		#region Scorecard

	    public PermissionsUtility EditUserScorecard(long userId)
	    {
		    return EditUserOrganization(userId);
	    }

		public PermissionsUtility ViewOrganizationScorecard(long organizationId)
		{
			if (IsRadialAdmin(caller))
				return this;

			if (IsManagingOrganization(organizationId))
				return this;

			var organization = session.Get<OrganizationModel>(organizationId);
			if (organization.Settings.EmployeesCanViewScorecard && caller.Organization.Id == organizationId)
				return this;
			if (organization.Settings.ManagersCanViewScorecard && IsManager(organizationId))
				return this;

			throw new PermissionsException();
		}

		#endregion

		#region L10
		public PermissionsUtility EditL10Meeting(long organizationId,long recurrenceId)
		{
			if (IsRadialAdmin(caller))
				return this;

			if (IsManagingOrganization(organizationId))
				return this;

			var organization = session.Get<OrganizationModel>(organizationId);
			if (recurrenceId == 0){
				if (organization.Settings.EmployeeCanCreateL10 && caller.Organization.Id == organizationId)
					return this;
				if (organization.Settings.ManagersCanCreateL10 && IsManager(organizationId))
					return this;
			}
			else{
				var availUserIds = new[]{caller.Id};
				if (caller.Organization.Settings.ManagersCanEditSubordinateL10){
					availUserIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(session, caller, caller.Id).ToArray();
				}

				var exists = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
					.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
					.WhereRestrictionOn(x => x.User.Id).IsIn(availUserIds)
					.RowCount();
				if (exists > 0)
					return this;
			}


			throw new PermissionsException();
		}
		public PermissionsUtility ViewL10Recurrence(long recurrenceId)
		{
			if (IsRadialAdmin(caller))
				return this;

			var recurrences_OrgId = session.Get<L10Recurrence>(recurrenceId).OrganizationId;

			if (IsManagingOrganization(recurrences_OrgId))
				return this;
			
			var possibleUsers = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
				.Where(x=>x.DeleteTime==null && x.L10Recurrence.Id==recurrenceId)
				.Select(x=>x.User.Id).List<long>()
				.ToList();

			if (possibleUsers.Contains(caller.Id))
				return this;

			if (caller.Organization.Settings.ManagersCanViewSubordinateL10){
				var subIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(session, caller, caller.Id);
				if (possibleUsers.ContainsAny(subIds))
					return this;
			}
			throw new PermissionsException();
		}

	    public PermissionsUtility ViewUsersL10Meetings(long userId)
	    {
			if (IsRadialAdmin(caller))
				return this;

		    if (caller.Id == userId)
			    return this;

			var users_OrgId = session.Get<UserOrganizationModel>(userId).Organization.Id;
			if (IsManagingOrganization(users_OrgId))
				return this;

			if (caller.Organization.Settings.ManagersCanViewSubordinateL10){
				var subIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(session, caller, caller.Id);
				if (subIds.Contains(userId))
					return this;
			}

			throw new PermissionsException();
	    }

	    public PermissionsUtility ViewL10Meeting(long meetingId)
	    {
			if (IsRadialAdmin(caller))
				return this;

		    var meeting = session.Get<L10Meeting>(meetingId);
			var meeting_OrgId = meeting.Organization.Id;
			if (IsManagingOrganization(meeting_OrgId))
				return this;

		    var meetingIds = session.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => 
				x.L10Meeting.Id == meetingId &&
				x.DeleteTime ==null).List().Select(x => x.UserId).ToList();
			if (caller.UserIds.ContainsAny(meetingIds))
				    return this;
		    if (caller.Organization.Settings.ManagersCanViewSubordinateL10){
				var subIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(session, caller, caller.Id);
				if (subIds.ContainsAny(meetingIds))
				    return this;
		    }

		    var recurId = meeting.L10RecurrenceId;
		    var defaultIds = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x =>
				x.L10Recurrence.Id ==  recurId &&
				x.DeleteTime == null).List().Select(x => x.User.Id).ToList();

			if (caller.UserIds.ContainsAny(defaultIds))
				return this;
			if (caller.Organization.Settings.ManagersCanViewSubordinateL10){
				var subIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(session, caller, caller.Id);
				if (subIds.ContainsAny(defaultIds))
					return this;
			}

			throw new PermissionsException();
	    }

		#endregion

		public PermissionsUtility OwnedBelowOrEqual(Predicate<UserOrganizationModel> visiblility)
        {
            if (IsOwnedBelowOrEqual(caller, visiblility))
                return this;
            throw new PermissionsException();
        }
        
        protected bool IsOwnedBelowOrEqual(UserOrganizationModel caller, Predicate<UserOrganizationModel> visibility)
        {
            if (visibility(caller))
                return true;
            foreach (var manager in caller.ManagingUsers.ToListAlive().Select(x => x.Subordinate))
            {
                if (IsOwnedBelowOrEqual(manager, visibility))
                    return true;
            }
            return false;
        }

        protected bool IsOwnedAboveOrEqual(UserOrganizationModel caller, Predicate<UserOrganizationModel> visibility)
        {
            if (visibility(caller))
                return true;
            foreach (var subordinate in caller.ManagedBy.ToListAlive().Select(x => x.Manager))
            {
                if (IsOwnedAboveOrEqual(subordinate, visibility))
                    return true;
            }
            return false;
        }

        protected bool IsOwnedBelowOrEqualOrganizational<T>(T start, Origin origin) where T : IOrigin
        {
            if (origin.AreEqual(start))
                return true;

            foreach (var sub in start.OwnsOrigins())
            {
                if (IsOwnedBelowOrEqualOrganizational(sub, origin))
                    return true;
            }

            return false;
        }
        

        
        /*public PermissionsUtility ViewImage(string imageId)
        {
            if (imageId == null)
                throw new PermissionsException();
            Predicate<UserOrganizationModel> p = x => x.User.NotNull(y => y.ImageUrl.NotNull(z => z.Id.ToString() == imageId));

            if (IsOwnedBelowOrEqual(caller, p) || IsOwnedAboveOrEqual(caller, p))
            {
                return this;
            }
            throw new PermissionsException();
        }*/


	    public PermissionsUtility Or(params Func<PermissionsUtility, PermissionsUtility>[] or)
	    {
		    foreach (var o in or){
			    try{
				    return o(this);
			    }catch (PermissionsException){
			    }
		    }
			throw new PermissionsException();
	    }



        public PermissionsUtility RemoveUser(long userId)
        {
            var found = session.Get<UserOrganizationModel>(userId);

            if (caller.ManagingOrganization || caller.Organization.Id==found.Organization.Id)
                return this;

            if (caller.Organization.ManagersCanRemoveUsers)
                ManagesUserOrganization(userId, true);

            throw new PermissionsException("You cannot remove this user.");
        }

        public static bool IsAdmin(UserOrganizationModel caller)
        {
            return IsRadialAdmin(caller);
        }

	    public delegate PermissionsUtility LongFunc(long id);

	    private PermissionsUtility _ConfirmPermissions<T,M>(T model,bool fixRefs, Expression<Func<T, long?>> idSelector,Expression<Func<T, M>> modelSelector, Func<PermissionsUtility,LongFunc> permissionsSelector) where M : ILongIdentifiable
	    {
		    var id =model.Get(idSelector);
			var m = model.Get(modelSelector);
		    if (id == null){
			    if (m == null)
					return this; //No error.. looks like its optional
				if (m.Id == 0)
					throw new PermissionsException("Model uninitialized [1]");
				if (fixRefs)
					model.Set(idSelector, m.Id);
				return permissionsSelector(this)(m.Id);
		    }else if (m == null)
			{
				if (id == 0)
					throw new PermissionsException();
				if (fixRefs){
					var mLoaded = session.Get<M>(id.Value);
					model.Set(modelSelector, mLoaded);
				}

				return permissionsSelector(this)(id.Value);
			}else{
				if (id == 0){
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [2]");
					if (fixRefs)
						model.Set(idSelector, m.Id);
					return permissionsSelector(this)(m.Id);
				}else{
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [3]");
					if (id != m.Id)
						throw new PermissionsException("Model Id != Id");
					return permissionsSelector(this)(id.Value);
				}
			}
	    }

	    private PermissionsUtility _ConfirmPermissions<T, M>(T model, bool fixRefs, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable
	    {
			var id = model.Get(idSelector);
			var m = model.Get(modelSelector);
			if (m == null)
			{
				if (id == 0)
					throw new PermissionsException();

				if (fixRefs)
				{
					var mLoaded = session.Load<M>(id);
					model.Set(modelSelector, mLoaded);
				}

				return permissionsSelector(this)(id);
			}
			else
			{
				if (id == 0)
				{
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [2]");

					if (fixRefs)
						model.Set(idSelector, m.Id);
					return permissionsSelector(this)(m.Id);
				}
				else
				{
					if (m.Id == 0)
						throw new PermissionsException("Model uninitialized [3]");

					if (id != m.Id)
						throw new PermissionsException("Model Id != Id");
					return permissionsSelector(this)(id);
				}
			}
	    }

	    public PermissionsUtility Confirm<T, M>(T model, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable{
			return _ConfirmPermissions(model, false, idSelector, modelSelector, permissionsSelector);
		}
		public PermissionsUtility Confirm<T, M>(T model, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable{
			return _ConfirmPermissions(model, false, idSelector, modelSelector, permissionsSelector);
		}
		public PermissionsUtility ConfirmAndFix<T, M>(T model, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable{
			return _ConfirmPermissions(model, true, idSelector, modelSelector, permissionsSelector);
		}
		public PermissionsUtility ConfirmAndFix<T, M>(T model, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable
		{
			return _ConfirmPermissions(model, true, idSelector, modelSelector, permissionsSelector);
		}

	}
}