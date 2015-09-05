using System.Diagnostics;
using System.Web;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Linq;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models
{
	[DebuggerDisplay("{User}")]
    public class UserOrganizationModel : ResponsibilityGroupModel, IOrigin, IDeletable/*, IAngularizer<UserOrganizationModel>*/
    {
        public static long ADMIN_ID = -7231398885982031L;

        public static UserOrganizationModel ADMIN = new UserOrganizationModel(){
            IsRadialAdmin = true,
            Id = UserOrganizationModel.ADMIN_ID,
        };

		public virtual long? _ClientTimestamp { get; set; }


        public virtual TempUserModel TempUser { get; set; }
        public virtual String EmailAtOrganization { get; set; }
		public virtual Boolean ManagerAtOrganization { get; set; }
		public virtual Boolean ManagingOrganization { get; set; }
        public virtual Boolean IsRadialAdmin { get; set; }
        //public virtual String Title { get; set; }
        public virtual DateTime AttachTime { get; set; }
        public virtual DateTime? DetachTime { get; set; }
        public virtual UserModel User { get; set; }

	    public virtual long[] UserIds
	    {
		    get
		    {
			    if (User == null)
				    return new long[]{Id};
			    
				return User.UserOrganizationIds;//.Select(x => x.Id).ToArray();
		    }
	    }

		public virtual UserLookup Cache { get; set; }

	    public virtual IList<ManagerDuration> ManagingUsers { get; set; }
        public virtual IList<ManagerDuration> ManagedBy { get; set; }
        public virtual IList<GroupModel> Groups { get; set; }
        public virtual IList<GroupModel> ManagingGroups { get; set; }
        //public virtual IList<QuestionModel> CustomQuestions { get; set; }
        //public virtual IList<NexusModel> CreatedNexuses { get; set; }
        //public virtual IList<QuestionModel> CreatedQuestions { get; set; }
        public virtual IList<ReviewModel> Reviews { get; set; }
        public virtual List<ReviewsModel> CreatedReviews { get; set; }
        public virtual IList<PositionDurationModel> Positions { get; set; }
        public virtual IList<TeamDurationModel> Teams { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual int CountPerPage { get; set; }
        public virtual String JobDescription { get; set; }
		
		public virtual long? JobDescriptionFromTemplateId { get; set; }

		/*public virtual int NumRocks { get; set; }
		public virtual int NumRoles { get; set; }
		public virtual int NumMeasurables { get; set; }*/

        public virtual OriginType GetOriginType()
        {
            return OriginType.User;
        }

        public virtual String GetSpecificNameForOrigin()
        {
            return this.GetName();
        }

        #region Helpers
        public virtual Dictionary<String, List<String>> Properties { get; set; }
        public virtual Boolean IsAttached()
        {
            return User != null;
        }
        public virtual List<UserOrganizationModel> AllSubordinates { get; set; }
        /*
        public virtual List<OriginType> EditableQuestionOrigins
        {
            get
            {
                var origins = new List<OriginType>();
                if (IsManager)
                    origins.Add(OriginType.User);
                if (ManagerAtOrganization)
                    origins.Add(OriginType.Group);
                if (IsManagerCanEditOrganization)
                    origins.Add(OriginType.Organization);
                return origins;
            }
        }*/
        #endregion

        public UserOrganizationModel()
            : base()
        {
            CreateTime = DateTime.UtcNow;
            ManagedBy = new List<ManagerDuration>();
            ManagingUsers = new List<ManagerDuration>();
            Groups = new List<GroupModel>();
            ManagingGroups = new List<GroupModel>();
            //CustomQuestions = new List<QuestionModel>();
            //CreatedNexuses = new List<NexusModel>();
            //CreatedQuestions = new List<QuestionModel>();
            AttachTime = DateTime.UtcNow;
            //AllSubordinates = new List<UserOrganizationModel>();
            Properties = new Dictionary<string, List<String>>();
            //CreatedReviews = new List<ReviewsModel>();
            Reviews = new List<ReviewModel>();
            Positions = new List<PositionDurationModel>();
            Teams = new List<TeamDurationModel>();
            TempUser = null;
			Cache=new UserLookup();
        }

	   /* public virtual void Angularize(Angularizer<UserOrganizationModel> angularizer)
		{
			angularizer.Add("Name", GetName());
			angularizer.Add("ImageUrl", this.ImageUrl(true));
			var inits = new List<string>();
			if (GetFirstName() != null && GetFirstName().Length > 0)
				inits.Add(GetFirstName().Substring(0, 1));
			if (GetLastName() != null && GetLastName().Length > 0)
				inits.Add(GetLastName().Substring(0, 1));
			var initials = string.Join(" ", inits).ToUpperInvariant();

			angularizer.Add("Initials", initials);
	    }*/

	    public override string ToString()
        {
            return Organization.NotNull(x => x.Name) + " - " + this.GetNameAndTitle();
        }


        public virtual List<IOrigin> OwnsOrigins()
        {
            var owns = new List<IOrigin>();
            owns.AddRange(ManagingUsers.Cast<IOrigin>());
            owns.AddRange(ManagingGroups.Cast<IOrigin>());
            //owns.AddRange(CreatedQuestions.Cast<IOrigin>());
            return owns;
        }

        public virtual List<IOrigin> OwnedByOrigins()
        {
            var ownedBy = new List<IOrigin>();
            ownedBy.AddRange(ManagedBy.Cast<IOrigin>());
            ownedBy.Add(Organization);
            return ownedBy;
        }

		public override string GetNameExtended()
		{
			return this.GetNameAndTitle();
		}
		public override string GetNameShort()
		{
			return this.GetFirstName();
		}
        public override string GetName()
        {
	        var user = this.NotNull(x => x.User);

			if (user != null)
				return user.Name();
			var tempUser = this.NotNull(x => x.TempUser);

			if (tempUser != null)
				return tempUser.Name();

            return this.Cache.NotNull(x=>x.Name) ?? this.EmailAtOrganization;
        }
		public virtual string GetFirstName() {
			if (this.User != null && !String.IsNullOrWhiteSpace(this.User.FirstName))
				return this.User.FirstName;

			if (TempUser != null && !String.IsNullOrWhiteSpace(this.TempUser.FirstName))
				return this.TempUser.FirstName;

			return GetName();
		}
		public virtual string GetLastName() {
			if (this.User != null && !String.IsNullOrWhiteSpace(this.User.LastName))
				return this.User.LastName;

			if (TempUser != null && !String.IsNullOrWhiteSpace(this.TempUser.LastName))
				return this.TempUser.LastName;

			return GetName();
		}

        public virtual string GetTitles(int numToShow = int.MaxValue, long callerUserId = -1)
        {
            if (this.Positions == null)
                return "";

            var count = this.Positions.Count();

            String titles = null;
            var actualPositions = Positions.ToListAlive().Select(x => x.Position.CustomName).ToList();
            if (callerUserId == Id)
                actualPositions.Insert(0, "You");

            titles = String.Join(", ", actualPositions.Take(numToShow));
            if (actualPositions.Count > numToShow)
                titles += ",...";

            return titles;
        }

        public override string GetGroupType()
        {
            return DisplayNameStrings.user;
        }
        
        public virtual string GetUsername()
        {
            return User.NotNull(x => x.UserName) ?? TempUser.Email;
        }


		public virtual UserOrganizationModel UpdateCache(ISession s)
		{
			if (Cache==null)
				Cache=new UserLookup();
			
			Cache.OrganizationId = Organization.Id;
			Cache._ImageUrlSuffix = this.ImageUrl(true, ImageSize._suffix);
			Cache.AttachTime = AttachTime;
			Cache.CreateTime = CreateTime;
			Cache.DeleteTime = DeleteTime;
			Cache.Email = this.GetEmail();
			Cache.HasJoined = User != null;
			Cache.HasSentInvite = !(TempUser != null && TempUser.LastSent == null);
			Cache.IsAdmin = ManagingOrganization;
			Cache.IsManager = this.IsManager();
			Cache.Managers = String.Join(", ", ManagedBy.ToListAlive().Select(x => x.Manager.GetName()));
			Cache.Positions = String.Join(", ", Positions.ToListAlive().Select(x => x.Position.CustomName));
			Cache.Teams = String.Join(", ", Teams.ToListAlive().Select(x => x.Team.Name));
			Cache.Name = this.GetName();

			var measurable=s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && x.AccountableUserId == Id).ToRowCountQuery().FutureValue<int>();
			var role=s.QueryOver<RoleModel>().Where(x => x.DeleteTime == null && x.ForUserId == Id).ToRowCountQuery().FutureValue<int>();
			var rock=s.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.ForUserId == Id).ToRowCountQuery().FutureValue<int>();

			Cache.NumMeasurables = measurable.Value;
			Cache.NumRoles = role.Value;
			Cache.NumRocks = rock.Value;

			Cache.UserId = Id;
			
			s.SaveOrUpdate(Cache);
			try{
				new Cache().InvalidateForUser(this,CacheKeys.USERORGANIZATION);
				//var id = RadialReview.Cache.Get(CacheKeys.USERORGANIZATION_ID);
				////var id = HttpContext.Current.Session[CacheKeys.USERORGANIZATION_ID];
				//if (id is long && (long)id==Id){
				//	//cache is dirty
				//}

			}catch (Exception e){
				throw new Exception("Could not update Session",e);
			}
			return this;

		}

	}

    public class UserOrganizationModelMap : SubclassMap<UserOrganizationModel>
    {
        public UserOrganizationModelMap()
        {
            //Map(x => x.Title);

            Map(x => x.IsRadialAdmin);
            Map(x => x.CountPerPage).Default("10");
            Map(x => x.ManagingOrganization);
            Map(x => x.ManagerAtOrganization);
            Map(x => x.AttachTime);
            Map(x => x.CreateTime);
            Map(x => x.DetachTime);
			Map(x => x.DeleteTime);
			Map(x => x.EmailAtOrganization);
			/*Map(x => x.NumRocks);
			Map(x => x.NumRoles);
			Map(x => x.NumMeasurables);*/

			Map(x => x.JobDescription).Length(65000);
			Map(x => x.JobDescriptionFromTemplateId);

			References(x => x.TempUser).Not.LazyLoad().Cascade.All();
			References(x => x.Cache).LazyLoad().Cascade.All();

            //Reviews
            HasMany(x => x.Reviews)
                .Cascade.SaveUpdate();
            /*HasMany(x => x.Responsibilities)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();*/
            //HasMany(x => x.CreatedReviews).Cascade.SaveUpdate();

            HasMany(x => x.Positions)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();

            References(x => x.User)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            //ORGANIZATION IS HOUSED IN RESPONSIBILITY GROUP
            /*References(x => x.Organization)
                .Column("Organization_Id")
                .Cascade.SaveUpdate();*/
            HasMany(x => x.Teams)
				.LazyLoad()
                .KeyColumn("User_id")
                .Cascade.SaveUpdate();

            HasMany(x => x.ManagedBy)
				.LazyLoad()
                .KeyColumn("SubordinateId")
                .Cascade.SaveUpdate();

            HasMany(x => x.ManagingUsers)
				.LazyLoad()
                .KeyColumn("ManagerId")
                .Cascade.SaveUpdate();


            /*HasManyToMany(x => x.ManagedBy)
                .Table("ManagedMembers")
                .ParentKeyColumn("Subordinate")
                .ChildKeyColumn("Manager")
                .Cascade.SaveUpdate();
            HasManyToMany(x => x.ManagingUsers)
                .Table("ManagedMembers")
                .ParentKeyColumn("Manager")
                .ChildKeyColumn("Subordinate")
                .Cascade.SaveUpdate();*/


			HasManyToMany(x => x.Groups)
				.LazyLoad()
                .Table("GroupMembers")
                .Inverse();
			HasManyToMany(x => x.ManagingGroups)
				.LazyLoad()
                .Table("GroupManagement")
                .Inverse();
           /* HasMany(x => x.CustomQuestions)
				.LazyLoad()
                .KeyColumn("UserQuestion_Id")
                .Inverse();
            HasMany(x => x.CreatedQuestions)
				.LazyLoad()
                .KeyColumn("CreatedQuestionsId")
                .Inverse();*/
            /*HasManyToMany(x => x.CreatedNexuses)
				.LazyLoad()
                .Cascade.SaveUpdate()
                .Table("UserOrganizationNexuses");*/


        }
    }
}