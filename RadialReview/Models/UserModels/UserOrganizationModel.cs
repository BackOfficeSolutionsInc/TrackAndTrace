﻿using System.Diagnostics;
using System.Runtime.Serialization;
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
using RadialReview.Utilities.DataTypes;
using log4net;
using RadialReview.Models.Components;
using System.Web.Script.Serialization;

namespace RadialReview.Models {



	[DebuggerDisplay("User: {EmailAtOrganization}")]
	[DataContract]
	public class UserOrganizationModel : ResponsibilityGroupModel, IOrigin, IHistorical, TimeSettings, IForModel/*, IAngularizer<UserOrganizationModel>*/
	{
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static long ADMIN_ID = -7231398885982031L;

		public static UserOrganizationModel ADMIN = new UserOrganizationModel() {
			IsRadialAdmin = true,
			Id = UserOrganizationModel.ADMIN_ID,
		};

		public static UserOrganizationModel CreateAdmin() {

			return new UserOrganizationModel() {
				IsRadialAdmin = true,
				Id = UserOrganizationModel.ADMIN_ID,
			};
		}


		public virtual DateTime? _MethodStart { get; set; }

		public virtual string GetClientRequestId() {
			if (string.IsNullOrEmpty(_ClientRequestId)) {
				return "" + User.NotNull(x => x.Id.ToString().Replace("-", "")) ?? ("" + CreateTime.ToJsMs() + Id);
			}
			return _ClientRequestId;
		}

		public virtual void SetClientRequestId(string id) {
			_ClientRequestId = id;
		}

		public virtual void SetClientTimeStamp(long timestamp) {
			_ClientTimestamp = timestamp;
		}
		public virtual void IncrementClientTimestamp() {
			_ClientTimestamp = (_ClientTimestamp ?? DateTime.UtcNow.ToJsMs()) + 1;
		}

		public virtual string _ClientRequestId { get; set; }
		public virtual long? _ClientTimestamp { get; set; }
		public virtual int? _ClientOffset { get; set; }
		protected virtual TimeData _timeData { get; set; }

		public class AdminShortCircuit{
			public bool IsMocking { get; internal set; }
			public bool IsRadialAdmin { get; set; }
			public string ActualUserId { get; set; }
			public bool AllowAdminWithoutAudit { get; set; }
		}
		public virtual AdminShortCircuit _AdminShortCircuit { get; set; }
		public virtual bool _IsRadialAdmin { get; set; }
		[Obsolete("For testing only")]
		public virtual bool _IsTestAdmin { get; set; }

		public virtual ITimeData GetTimeSettings() {
			if (_timeData == null) {
				var orgSettings = GetOrganizationSettings();
				_timeData = new TimeData() {
					Now = _MethodStart ?? DateTime.UtcNow,
					Period = orgSettings.ScorecardPeriod,
					TimezoneOffset = _ClientOffset ?? orgSettings.GetTimezoneOffset(),
					WeekStart = orgSettings.WeekStart,
					YearStart = orgSettings.YearStart,
					DateFormat = orgSettings.GetDateFormat()
				};
			}
			return _timeData;
		}

		[DataMember]
		public virtual string Name { get { return GetName(); } }
		[DataMember]
		public virtual string UserName {
			get {
				try {
					return GetUsername();
				} catch (Exception e) {
					return null;
				}
			}
		}

		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual TempUserModel TempUser { get; set; }
		public virtual String EmailAtOrganization { get; set; }
		public virtual Boolean ManagerAtOrganization { get; set; }
		public virtual Boolean ManagingOrganization { get; set; }
		public virtual Boolean IsRadialAdmin { get; set; }
		public virtual bool IsImplementer { get; set; }
		//public virtual String Title { get; set; }
		public virtual DateTime AttachTime { get; set; }
		public virtual DateTime? DetachTime { get; set; }

		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual UserModel User { get; set; }

		public virtual long[] UserIds {
			get {
				if (User == null)
					return new long[] { Id };

				return User.UserOrganizationIds;//.Select(x => x.Id).ToArray();
			}
		}

		public virtual UserLookup Cache { get; set; }

		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<ManagerDuration> ManagingUsers { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<ManagerDuration> ManagedBy { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<GroupModel> Groups { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<GroupModel> ManagingGroups { get; set; }
		//public virtual IList<QuestionModel> CustomQuestions { get; set; }
		//public virtual IList<NexusModel> CreatedNexuses { get; set; }
		//public virtual IList<QuestionModel> CreatedQuestions { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<ReviewModel> Reviews { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual List<ReviewsModel> CreatedReviews { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<PositionDurationModel> Positions { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual IList<TeamDurationModel> Teams { get; set; }
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
		public virtual DateTime? DeleteTime { get; set; }
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
		public virtual DateTime CreateTime { get; set; }
		public virtual int CountPerPage { get; set; }
		public virtual String JobDescription { get; set; }

		public virtual long? JobDescriptionFromTemplateId { get; set; }
		public virtual Boolean EvalOnly { get; set; }

		/*public virtual int NumRocks { get; set; }
		public virtual int NumRoles { get; set; }
		public virtual int NumMeasurables { get; set; }*/

		public override string GetImageUrl() {
			return this.ImageUrl(true);
		}

		public override OriginType GetOrigin() {
			return OriginType.User;
		}

		public virtual OriginType GetOriginType() {
			return OriginType.User;
		}

		public virtual String GetSpecificNameForOrigin() {
			return this.GetName();
		}

		public virtual OrganizationModel.OrganizationSettings GetOrganizationSettings() {
			return Organization.NotNull(x => x.Settings) ?? new OrganizationModel.OrganizationSettings();
		}

		#region Helpers
		public virtual Dictionary<String, List<String>> Properties { get; set; }
		public virtual Boolean IsAttached() {
			return User != null;
		}
		public virtual List<UserOrganizationModel> AllSubordinates { get; set; }

		public virtual bool IsClient { get; set; }
		public virtual bool IsPlaceholder { get; set; }

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
			: base() {
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
			IsClient = false;
			IsPlaceholder = false;
			Cache = new UserLookup();
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

		public override string ToString() {
			return Organization.NotNull(x => x.Name) + " - " + this.GetNameAndTitle();
		}


		public virtual List<IOrigin> OwnsOrigins() {
			var owns = new List<IOrigin>();
			owns.AddRange(ManagingUsers.Cast<IOrigin>());
			owns.AddRange(ManagingGroups.Cast<IOrigin>());
			//owns.AddRange(CreatedQuestions.Cast<IOrigin>());
			return owns;
		}

		public virtual List<IOrigin> OwnedByOrigins() {
			var ownedBy = new List<IOrigin>();
			ownedBy.AddRange(ManagedBy.Cast<IOrigin>());
			ownedBy.Add(Organization);
			return ownedBy;
		}

		public override string GetNameExtended() {
			return this.GetNameAndTitle();
		}
		public override string GetNameShort() {
			return this.GetFirstName();
		}
		public override string GetName() {
			var user = this.NotNull(x => x.User);

			if (user != null)
				return user.Name();
			var tempUser = this.NotNull(x => x.TempUser);

			if (tempUser != null)
				return tempUser.Name();

			return this.Cache.NotNull(x => x.Name) ?? this.EmailAtOrganization;
		}
		public virtual string GetFirstName() {
			if (this.User != null && !String.IsNullOrWhiteSpace(this.User.FirstName))
				return this.User.FirstName.Trim();

			if (TempUser != null && !String.IsNullOrWhiteSpace(this.TempUser.FirstName))
				return this.TempUser.FirstName.Trim();

			return GetName();
		}
		public virtual string GetLastName() {
			if (this.User != null && !String.IsNullOrWhiteSpace(this.User.LastName))
				return this.User.LastName.Trim();

			if (TempUser != null && !String.IsNullOrWhiteSpace(this.TempUser.LastName))
				return this.TempUser.LastName.Trim();

			return GetName();
		}


		public virtual string GetTitles(int numToShow = int.MaxValue, long callerUserId = -1) {
			if (this.Positions == null)
				return "";

			var count = this.Positions.Distinct().ToListAlive().Count();

			String titles = null;
			var actualPositions = Positions.Distinct().ToListAlive().Select(x => x.Position.CustomName).ToList();
			if (callerUserId == Id)
				actualPositions.Insert(0, "You");

			titles = String.Join(", ", actualPositions.Take(numToShow));
			if (actualPositions.Count > numToShow)
				titles += ",...";

			return titles;
		}

		public override string GetGroupType() {
			return DisplayNameStrings.user;
		}

		public virtual string GetUsername() {
			return User.NotNull(x => x.UserName) ?? TempUser.Email;
		}


		public virtual UserOrganizationModel UpdateCache(ISession s) {
			if (Cache == null)
				Cache = new UserLookup();

			if (Cache.OrganizationId != Organization.Id)
				Cache.OrganizationId = Organization.Id;
			if (Cache._ImageUrlSuffix != this.ImageUrl(true, ImageSize._suffix))
				Cache._ImageUrlSuffix = this.ImageUrl(true, ImageSize._suffix);
			if (Cache.AttachTime != AttachTime)
				Cache.AttachTime = AttachTime;
			if (Cache.CreateTime != CreateTime)
				Cache.CreateTime = CreateTime;
			if (Cache.DeleteTime != DeleteTime)
				Cache.DeleteTime = DeleteTime;
			if (Cache.IsRadialAdmin != this.IsRadialAdmin)
				Cache.IsRadialAdmin = this.IsRadialAdmin;
			if (Cache.Email != this.GetEmail())
				Cache.Email = this.GetEmail();
			if (Cache.IsClient != this.IsClient)
				Cache.IsClient = this.IsClient;
			if (Cache.HasJoined != (User != null))
				Cache.HasJoined = User != null;
			if (Cache.HasSentInvite != (!(TempUser != null && TempUser.LastSent == null)))
				Cache.HasSentInvite = !(TempUser != null && TempUser.LastSent == null);
			if (Cache.IsAdmin != this.ManagingOrganization)
				Cache.IsAdmin = ManagingOrganization;
			if (Cache.IsManager != this.IsManager(true))
				Cache.IsManager = this.IsManager(true);
			if (Cache.EvalOnly != this.EvalOnly)
				Cache.EvalOnly = this.EvalOnly;
			UserOrganizationModel managerA = null;
			UserLookup cacheA = null;
			try {
				var managersQ = s.QueryOver<ManagerDuration>()
					.JoinAlias(x => x.Manager, () => managerA)
					.JoinAlias(x => managerA.Cache, () => cacheA)
					.Where(x => x.DeleteTime == null && x.SubordinateId == Id && managerA.DeleteTime == null)
					.Select(x => cacheA.Name).List<string>().Distinct().ToList();

				var managers = String.Join(", ", managersQ);// ManagedBy.ToListAlive().Distinct(x => x.ManagerId).Select(x => x.Manager.GetName()));
				if (Cache.Managers != managers)
					Cache.Managers = managers;
			} catch (Exception e) {
				log.Error(e);
			}
			var positions = String.Join(", ", Positions.ToListAlive().Distinct(x => x.Position.Id).Select(x => x.Position.CustomName));
			if (Cache.Positions != positions)
				Cache.Positions = positions;

			if (Cache.IsImplementer != IsImplementer)
				Cache.IsImplementer = IsImplementer;
			try {
				var teams = s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null && x.UserId == Id).Select(x => x.Team).List<OrganizationTeamModel>().ToList();

				var teamsStr = String.Join(", ", teams.Select(x => x.Name));
				if (Cache.Teams != teamsStr)
					Cache.Teams = teamsStr;
			} catch (Exception e) {
				log.Error(e);
			}
			if (Cache.Name != this.GetName())
				Cache.Name = this.GetName();

			var measurable = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && x.AccountableUserId == Id).ToRowCountQuery().FutureValue<int>();

			//s.QueryOver<RoleModel>().Where(x => x.DeleteTime == null && x.ForUserId == Id).ToRowCountQuery().FutureValue<int>();
			var rock = s.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.ForUserId == Id).ToRowCountQuery().FutureValue<int>();

			var role = RoleAccessor.CountRoles(s, Id);

			if (Cache.NumMeasurables != measurable.Value)
				Cache.NumMeasurables = measurable.Value;
			if (Cache.NumRoles != role)
				Cache.NumRoles = role;
			if (Cache.NumRocks != rock.Value)
				Cache.NumRocks = rock.Value;

			if (Cache.UserId != Id)
				Cache.UserId = Id;
#pragma warning disable CS0618 // Type or member is obsolete
			if (Cache.Id == 0) {
#pragma warning restore CS0618 // Type or member is obsolete
				s.Save(Cache);
			} else {
				s.Merge(Cache);
			}
			try {
				new Cache().InvalidateForUser(this, CacheKeys.USERORGANIZATION);
				//var id = RadialReview.Cache.Get(CacheKeys.USERORGANIZATION_ID);
				////var id = HttpContext.Current.Session[CacheKeys.USERORGANIZATION_ID];
				//if (id is long && (long)id==Id){
				//	//cache is dirty
				//}

			} catch (Exception e) {
				throw new Exception("Could not update Session", e);
			}
			return this;

		}


		public virtual string ClientOrganizationName { get; set; }

		public virtual string UserModelId { get { return User.NotNull(x => x.Id); } set { } }

		public virtual long ModelId { get { return Id; } }
		public virtual string ModelType { get { return ForModel.GetModelType<UserOrganizationModel>(); } }


		public virtual bool Is<T>() {
			return typeof(UserOrganizationModel).IsAssignableFrom(typeof(T));
		}
		public virtual string ToPrettyString() {
			return GetName();
		}

		public virtual DataContract GetUserDataContract() {
			return new DataContract(this);
		}

		[DataContract]
		public class DataContract {
			[DataMember]
			public virtual long Id { get; set; }
			[DataMember]
			public virtual String Name { get; set; }
			[DataMember]
			public virtual String Username { get; set; }

			public DataContract(UserOrganizationModel self) {
				Id = self.Id;
				Name = self.GetName();
				Username = self.GetUsername();
			}
		}

		public virtual int GetTimezoneOffset() {
			return _ClientOffset ?? GetOrganizationSettings().GetTimezoneOffset();
		}

		internal class PermissionsShortCircuit : AdminShortCircuit {
			public string ActualUserId { get; set; }
			public bool IsMocking { get; set; }
			public bool IsRadialAdmin { get; set; }
		}
	}

	public class UserOrganizationModelMap : SubclassMap<UserOrganizationModel> {
		public UserOrganizationModelMap() {
			//Map(x => x.Title);

			Map(x => x.IsRadialAdmin);
			Map(x => x.IsImplementer);
			Map(x => x.CountPerPage).Default("10");
			Map(x => x.ManagingOrganization);
			Map(x => x.ManagerAtOrganization);
			Map(x => x.AttachTime);
			Map(x => x.CreateTime);
			Map(x => x.DetachTime);
			Map(x => x.DeleteTime);
			Map(x => x.EmailAtOrganization);
			Map(x => x.IsClient);
			Map(x => x.IsPlaceholder);

			Map(x => x.UserModelId).Column("UserModel_id");

			Map(x => x.EvalOnly);

			Map(x => x.ClientOrganizationName);
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
				.KeyColumn("UserId")
				.Not.LazyLoad()
				.Cascade.SaveUpdate();

			References(x => x.User)
				.Not.LazyLoad()
				.Cascade.SaveUpdate();
			//ORGANIZATION IS HOUSED IN RESPONSIBILITY GROUP
			/*References(x => x.Organization)
                .Column("Organization_Id")
                .Cascade.SaveUpdate();*/
			/* HasMany(x => x.Teams)
				 .LazyLoad()
				 .KeyColumn("User_id")
				 .Cascade.SaveUpdate();*/

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
