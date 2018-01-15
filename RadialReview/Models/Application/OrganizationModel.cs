using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate.Mapping;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Scorecard;
using RadialReview.Properties;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TimeZoneNames;
using Twilio;

namespace RadialReview.Models {
	public class OrganizationLookup {
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }

		public virtual long LastUserLogin { get; set; }
		public virtual DateTime LastUserLoginTime { get; set; }

		public virtual DateTime CreateTime { get; set; }

		public class Map : ClassMap<OrganizationLookup> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.OrgId).Index("OrgLookup_OrgId_Index");
				Map(x => x.LastUserLogin);
				Map(x => x.LastUserLoginTime);
				Map(x => x.CreateTime);
			}
		}
	}

	public enum CoachType {
		Unknown=0,
		CertifiedOrProfessionalEOSi=1,
		BaseCamp=2,
		BusinessCoach=3,
		Other=4,		
	}

	public enum HasCoach {
		Unknown = 0,
		Yes = 1,
		No = 2,
		Other = 3
	}

	public enum EosUserType {
		Unknown = 0,
		Visionary = 1,
		Integrator = 2,
		HR = 4,
		Ops = 5,
		SystemAdmin = 6,
		Other = 7,
	}

	public class OrgCreationData : ILongIdentifiable {
		public virtual long Id { get; set; }

		[Required(AllowEmptyStrings = false)]
		public virtual string Name { get; set; }

		[Required]
		public virtual bool EnableL10 { get; set; }
		[Required]
		public virtual bool EnableReview { get; set; }
		[Required]
		public virtual bool EnablePeople { get; set; }
		[Required]
		public virtual bool EnableAC { get; set; }
		public virtual AccountType AccountType { get; set; }

		public virtual bool StartDeactivated { get; set; }
		
		public virtual long? AssignedTo { get; set; }
		public virtual string ReferralSource { get; set; }

		//public virtual string CoachName { get; set; }
		//public virtual CoachType CoachType { get; set; }
		public virtual HasCoach HasCoach { get; set; }
		public virtual long? CoachId { get; set; }

		public virtual string ContactFN { get; set; }
		public virtual string ContactLN { get; set; }
		public virtual string ContactPosition { get; set; }
		public virtual EosUserType ContactEosUserType { get; set; }
		public virtual DateTime? TrialEnd { get; set; }

		[EmailAddress]
		public virtual string ContactEmail { get; set; }
		public virtual long OrgId { get; set; }

		public OrgCreationData() {
			AccountType = AccountType.Demo;
			EnableL10 = true;
			EnableAC = true;
			EnableReview = false;
			EnablePeople = false;
			TrialEnd = DateTime.UtcNow.Date.AddDays(30);
		}

		public class Map : ClassMap<OrgCreationData> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Name);
				Map(x => x.EnableL10);
				Map(x => x.EnableReview);
				Map(x => x.EnableAC);
				Map(x => x.AccountType);
				Map(x => x.StartDeactivated);

				Map(x => x.AssignedTo);
				Map(x => x.ReferralSource);
				//Map(x => x.CoachType);
				//Map(x => x.CoachName);
				Map(x => x.HasCoach);

				Map(x => x.ContactFN);
				Map(x => x.ContactLN);
				Map(x => x.ContactEmail);
				Map(x => x.ContactEosUserType);
				Map(x => x.CoachId);
				Map(x => x.TrialEnd);
				Map(x => x.EnablePeople);

				Map(x => x.OrgId);
			}

		}
	}

	public class OrganizationModel : ResponsibilityGroupModel, IOrigin, IDeletable, TimeSettings {
		[Obsolete("Use the user if possible.")]
		public virtual TimeData GetTimeSettings() {
			return new TimeData() {
				Now = DateTime.UtcNow,
				Period = Settings.ScorecardPeriod,
				TimezoneOffset = Settings.GetTimezoneOffset(),
				WeekStart = Settings.WeekStart,
				YearStart = Settings.YearStart,
			};
		}
		public class OrganizationSettings {
			public virtual DayOfWeek WeekStart { get; set; }
			public virtual ScorecardPeriod ScorecardPeriod { get; set; }
			public virtual BrandingType Branding { get; set; }

			public virtual string TimeZoneId { get; set; }
			public virtual bool AutoUpgradePayment { get; set; }
			public virtual bool EmployeesCanViewScorecard { get; set; }
			public virtual bool ManagersCanViewScorecard { get; set; }
			public virtual bool EmployeeCanCreateL10 { get; set; }
			public virtual bool ManagersCanCreateL10 { get; set; }
			public virtual bool ManagersCanViewSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSelf { get; set; }
			public virtual bool EmployeesCanEditSelf { get; set; }
			public virtual bool OnlySeeRocksAndScorecardBelowYou { get; set; }

            public virtual bool AllowAddClient { get; set; }
			
			public virtual bool EnableL10 { get; set; }
			public virtual bool EnableReview { get; set; }
            public virtual bool EnablePeople { get; set; }
            public virtual bool EnableCoreProcess { get; set; }
            public virtual bool DisableAC { get; set; }

			public virtual int? DefaultSendTodoTime { get; set; }

			public virtual bool EnableSurvey { get; set; }

			public virtual String DateFormat { get; set; }

			public virtual int GetTimezoneOffset() {
				var zone = TimeZoneId ?? "Central Standard Time";
				var ts = TimeZoneInfo.FindSystemTimeZoneById(zone);
				return (int)ts.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
			}
			public virtual YearStart YearStart {
				get {
					return new YearStart(this);
				}
			}
			public OrganizationSettings() {
				TimeZoneId = "Central Standard Time";
				WeekStart = DayOfWeek.Sunday;

				ScorecardPeriod = ScorecardPeriod.Weekly;

				EmployeesCanViewScorecard = false;
				ManagersCanViewScorecard = true;

				EmployeeCanCreateL10 = false;
				ManagersCanCreateL10 = true;

				AutoUpgradePayment = true;

				ManagersCanViewSubordinateL10 = true;
				ManagersCanEditSubordinateL10 = false;

				EmployeesCanCreateSurvey = false;
				ManagersCanCreateSurvey = true;

				DefaultSendTodoTime = -1;

				OnlySeeRocksAndScorecardBelowYou = true;

				EnableL10 = false;
				EnableReview = false;
				DisableAC = false;

				LimitFiveState = true;

				DateFormat = "MM-dd-yyyy";

				RockName = "Rocks";
			}
			public virtual string RockName { get; set; }

			public class OrgSettingsVM : ComponentMap<OrganizationSettings> {
				public OrgSettingsVM() {
					Map(x => x.WeekStart);
					//Map(x => x.TimeZoneOffsetMinutes);
					Map(x => x.TimeZoneId);

					Map(x => x.EmployeesCanViewScorecard);
					Map(x => x.ManagersCanViewScorecard);

					Map(x => x.AutoUpgradePayment);

					Map(x => x.EmployeeCanCreateL10);
					Map(x => x.ManagersCanCreateL10);

					Map(x => x.ManagersCanViewSubordinateL10);
					Map(x => x.ManagersCanEditSubordinateL10);

					Map(x => x.ManagersCanEditSelf);
					Map(x => x.EmployeesCanEditSelf);

                    Map(x => x.AllowAddClient);

					Map(x => x.EmployeesCanCreateSurvey);
					Map(x => x.ManagersCanCreateSurvey);

					Map(x => x.DefaultSendTodoTime);

					Map(x => x.OnlySeeRocksAndScorecardBelowYou);

                    Map(x => x.EnableCoreProcess);
                    Map(x => x.EnableL10);
                    Map(x => x.EnableReview);
					Map(x => x.EnableSurvey);
					Map(x => x.EnablePeople);

					Map(x => x.DisableUpgradeUsers);

					Map(x => x.LimitFiveState);

					Map(x => x.RockName);
					Map(x => x.DateFormat);
					Map(x => x.NumberFormat);

					Map(x => x.Branding).CustomType<BrandingType>();
					Map(x => x.ScorecardPeriod).CustomType<ScorecardPeriod>();
					Map(x => x.StartOfYearMonth).CustomType<Month>();
					Map(x => x.StartOfYearOffset).CustomType<DateOffset>();
				}
			}

			public virtual bool EmployeesCanCreateSurvey { get; set; }

			public virtual bool ManagersCanCreateSurvey { get; set; }

			public virtual Month StartOfYearMonth { get; set; }
			public virtual DateOffset StartOfYearOffset { get; set; }
			public virtual NumberFormat NumberFormat { get; set; }
			public virtual bool LimitFiveState { get; set; }
			public virtual bool DisableUpgradeUsers { get; set; }

			public virtual string GetAngularNumberFormat() {
				return NumberFormat.Angular();
			}

			public virtual String GetDateFormat() {
				return DateFormat ?? "MM-dd-yyyy";
			}
		}

		
		public virtual long? PrimaryContactUserId { get; set; }

		/// <summary>
		/// In minutes
		/// </summary>
		/// <returns></returns>
		public virtual int GetTimezoneOffset() {
			return Settings.GetTimezoneOffset();
			//var zone = Settings.TimeZoneId ?? "Central Standard Time";
			//var ts = TimeZoneInfo.FindSystemTimeZoneById(zone);
			//return (int)ts.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
		}

		public virtual DateTime ConvertFromUTC(DateTime utcTime) {
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
			return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz); //your UTC date here
		}

		public virtual string GetTimeZoneId(DateTime? time = null) {
			time = time ?? DateTime.UtcNow;
			var id = Settings.TimeZoneId ?? "Central Standard Time";
			var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
			var abb = TZNames.GetAbbreviationsForTimeZone(id, "en-us");
			if (tz.IsDaylightSavingTime(time.Value)) {
				return abb.Daylight;
			}
			return abb.Standard;

		}

		public virtual DateTime ConvertToUTC(DateTime localTime) {
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
			return TimeZoneInfo.ConvertTimeToUtc(localTime, tz); //your UTC date here
		}

		public virtual TimeSpan ConvertToUTC(TimeSpan localTimeSpan) {
			//var localDate = ConvertFromUTC(DateTime.UtcNow.Date);
			//var localDT = localDate.Add(localTimeSpan);
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var now = DateTime.UtcNow;
			return localTimeSpan - TimeZoneInfo.FindSystemTimeZoneById(zone).GetUtcOffset(now);
			//return ConvertToUTC(localDT).Subtract(localDate);
		}
		public virtual TimeSpan ConvertFromUTC(TimeSpan localTimeSpan) {
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var now = DateTime.UtcNow;
			return localTimeSpan + TimeZoneInfo.FindSystemTimeZoneById(zone).GetUtcOffset(now);
		}


		[Display(Name = "organizationName", ResourceType = typeof(DisplayNameStrings))]
		public virtual LocalizedStringModel Name { get; set; }

		[Display(Name = "imageUrl", ResourceType = typeof(DisplayNameStrings))]
		public virtual ImageModel Image { get; set; }

		[Display(Name = "managerCanAddQuestions", ResourceType = typeof(DisplayNameStrings))]
		public virtual Boolean ManagersCanEdit { get; set; }
		public virtual Boolean ManagersCanRemoveUsers { get; set; }
		public virtual bool StrictHierarchy { get; set; }

		protected virtual OrganizationSettings _Settings { get; set; }
		public virtual OrganizationSettings Settings {
			get {
				if (_Settings == null)
					_Settings = new OrganizationSettings();
				return _Settings;
			}
		}

		public virtual AccountType AccountType { get; set; }
		[JsonIgnore]
		public virtual IList<UserOrganizationModel> Members { get; set; }
		[JsonIgnore]
		public virtual IList<PaymentModel> Payments { get; set; }
		[JsonIgnore]
		public virtual IList<InvoiceModel> Invoices { get; set; }
		[JsonIgnore]
		public virtual IList<QuestionModel> CustomQuestions { get; set; }
		[JsonIgnore]
		public virtual IList<QuestionCategoryModel> QuestionCategories { get; set; }
		//public virtual IList<IndustryModel> Industries { get; set; }
		[JsonIgnore]
		public virtual IList<GroupModel> Groups { get; set; }
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
		public virtual DateTime? DeleteTime { get; set; }
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
		public virtual DateTime CreationTime { get; set; }
		public virtual bool SendEmailImmediately { get; set; }
		public virtual String ImageUrl { get; set; }
		public virtual long AccountabilityChartId { get; set; }

		[JsonIgnore]
		public virtual IList<ReviewsModel> Reviews { get; set; }

		public override OriginType GetOrigin() {
			return OriginType.Organization;
		}
		public virtual OriginType GetOriginType() {
			return OriginType.Organization;
		}

		public virtual PaymentPlanModel PaymentPlan { get; set; }

		public virtual String GetSpecificNameForOrigin() {
			return Name.Translate();
		}

		public virtual bool ManagersCanEditPositions { get; set; }

		public OrganizationModel() {
			Groups = new List<GroupModel>();
			Payments = new List<PaymentModel>();
			Invoices = new List<InvoiceModel>();
			CustomQuestions = new List<QuestionModel>();
			Members = new List<UserOrganizationModel>();
			//Industries = new List<IndustryModel>();
			QuestionCategories = new List<QuestionCategoryModel>();
			Reviews = new List<ReviewsModel>();
			ManagersCanEditPositions = true;
			ManagersCanEdit = false;
			_Settings = new OrganizationSettings();
			AccountType = AccountType.Demo;

			//Lookup = new OrganizationLookup();
		}

		public virtual List<IOrigin> OwnsOrigins() {
			var owns = new List<IOrigin>();
			owns.AddRange(CustomQuestions.Cast<IOrigin>().ToList());
			owns.AddRange(QuestionCategories.Cast<IOrigin>().ToList());
			owns.AddRange(Groups.Cast<IOrigin>().ToList());
			owns.AddRange(Members.Cast<IOrigin>().ToList());
			owns.AddRange(Members.Cast<IOrigin>().ToList());

			return owns;
		}

		public virtual List<IOrigin> OwnedByOrigins() {
			var ownedBy = new List<IOrigin>();
			return ownedBy;
		}

		public override string GetName() {
			return Name.Translate();
		}
		public override string GetImageUrl() {
			return ImageUrl ?? base.GetImageUrl();
		}

		public override string GetGroupType() {
			return DisplayNameStrings.organization;
		}


		//public OrganizationLookup Lookup { get; set; }


		public class OrganizationModelMap : SubclassMap<OrganizationModel> {
			public OrganizationModelMap() {
				Map(x => x.AccountType);
				Map(x => x.ManagersCanEdit);
				Map(x => x.DeleteTime);
				Map(x => x.ImageUrl);
				Map(x => x.CreationTime);
				Map(x => x.StrictHierarchy);
				Map(x => x.ManagersCanEditPositions);
				Map(x => x.ManagersCanRemoveUsers);
				Map(x => x.AccountabilityChartId);

				Map(x => x.PrimaryContactUserId);

				//References(x => x.Lookup).LazyLoad().Cascade.SaveUpdate();

				//Map(x => x.ImageUrl);
				Map(x => x.SendEmailImmediately);
				Component(x => x._Settings).ColumnPrefix("Settings_");

				References(x => x.Image)
					.Not.LazyLoad()
					.Cascade.SaveUpdate();
				References(x => x.Name)
					.Not.LazyLoad()
					.Cascade.SaveUpdate();
				References(x => x.PaymentPlan)
					.LazyLoad()
					.Cascade.SaveUpdate();

				HasMany(x => x.Reviews)
					.LazyLoad()
					.Cascade.SaveUpdate();
				HasMany(x => x.Members)
					.KeyColumn("Organization_Id")
					.LazyLoad()
					.Cascade.SaveUpdate();
				HasMany(x => x.Payments)
					.LazyLoad()
					.Cascade.SaveUpdate();
				HasMany(x => x.Invoices)
					.LazyLoad()
					.Cascade.SaveUpdate();
				/*HasMany(x => x.Industries)
                    .KeyColumn("OrganizationId")
                    .Inverse();
                HasMany(x => x.QuestionCategories)
                    .KeyColumn("OrganizationId")
                    .Inverse();
                HasMany(x => x.Groups)
                    .Inverse();
                HasMany(x => x.CustomQuestions)
                    .KeyColumn("OrganizationQuestion_Id")
                    .Inverse();*/
			}
		}

	}
}