using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Mapping;
using NHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TimeZoneNames;

namespace RadialReview.Models
{
	public class OrganizationModel : ResponsibilityGroupModel, IOrigin, IDeletable
	{
		public class OrganizationSettings
		{
			public virtual DayOfWeek WeekStart { get; set; }
			/*public virtual int TimeZoneOffsetMinutes {
				get
				{
					if (TimeZoneId != null){
						var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
						zone.IsDaylightSavingTime(hwTime) ? hwZone.DaylightName : hwZone.StandardName, 


						return .BaseUtcOffset.Minutes;
					}
				}
			}*/

			public virtual BrandingType Branding { get; set; }
			
			public virtual string TimeZoneId { get; set; }
			public virtual bool EmployeesCanViewScorecard { get; set; }
			public virtual bool ManagersCanViewScorecard { get; set; }
			public virtual bool EmployeeCanCreateL10 { get; set; }
			public virtual bool ManagersCanCreateL10 { get; set; }
			public virtual bool ManagersCanViewSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSelf { get; set; }
			public virtual bool EmployeesCanEditSelf { get; set; }
			public virtual bool OnlySeeRocksAndScorecardBelowYou { get; set; }

			public virtual bool EnableL10 { get; set; }
			public virtual bool EnableReview { get; set; }

			public virtual bool EnableSurvey { get; set; }
			public OrganizationSettings()
			{
				TimeZoneId = "Central Standard Time";
				WeekStart= DayOfWeek.Sunday;

				EmployeesCanViewScorecard = false;
				ManagersCanViewScorecard = true;

				EmployeeCanCreateL10 = false;
				ManagersCanCreateL10 = true;

				ManagersCanViewSubordinateL10 = true;
				ManagersCanEditSubordinateL10 = false;

				EmployeesCanCreateSurvey = false;
				ManagersCanCreateSurvey = true;

				OnlySeeRocksAndScorecardBelowYou = true;

				EnableL10 = false;
				EnableReview = false;

				RockName = "Rocks";
			}
			public virtual string RockName { get; set; }

			public class OrgSettingsVM : ComponentMap<OrganizationSettings>
			{
				public OrgSettingsVM()
				{
					Map(x => x.WeekStart);
					//Map(x => x.TimeZoneOffsetMinutes);
					Map(x => x.TimeZoneId);

					Map(x => x.EmployeesCanViewScorecard);
					Map(x => x.ManagersCanViewScorecard);


					Map(x => x.EmployeeCanCreateL10);
					Map(x => x.ManagersCanCreateL10);

					Map(x => x.ManagersCanViewSubordinateL10);
					Map(x => x.ManagersCanEditSubordinateL10);

					Map(x => x.ManagersCanEditSelf);
					Map(x => x.EmployeesCanEditSelf);

					Map(x => x.EmployeesCanCreateSurvey);
					Map(x => x.ManagersCanCreateSurvey);


					Map(x => x.OnlySeeRocksAndScorecardBelowYou);

					Map(x => x.EnableL10);
					Map(x => x.EnableReview);
					Map(x => x.EnableSurvey);

					Map(x => x.RockName);

					Map(x => x.Branding).CustomType<BrandingType>();
				}
			}

			public virtual bool EmployeesCanCreateSurvey { get; set; }

			public virtual bool ManagersCanCreateSurvey { get; set; }
		}

		/// <summary>
		/// In minutes
		/// </summary>
		/// <returns></returns>
		public virtual int GetTimezoneOffset()
		{
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var ts = TimeZoneInfo.FindSystemTimeZoneById(zone);
			return (int)ts.GetUtcOffset(DateTime.UtcNow).TotalMinutes;
		}

		public virtual DateTime ConvertFromUTC(DateTime utcTime){
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
			return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz); //your UTC date here
		}

		public virtual string GetTimeZoneId(DateTime? time=null)
		{
			time = time ?? DateTime.UtcNow;
			var id = Settings.TimeZoneId ?? "Central Standard Time";
			var tz=TimeZoneInfo.FindSystemTimeZoneById(id);
			var abb=TimeZoneNames.TimeZoneNames.GetAbbreviationsForTimeZone(id, "en-us");
			if (tz.IsDaylightSavingTime(time.Value)){
				return abb.Daylight;
			}
			return abb.Standard;

		}

		public virtual DateTime ConvertToUTC(DateTime localTime){
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
			return TimeZoneInfo.ConvertTimeToUtc(localTime, tz); //your UTC date here
		}

		public virtual TimeSpan ConvertToUTC(TimeSpan localTimeSpan)
		{
			//var localDate = ConvertFromUTC(DateTime.UtcNow.Date);
			//var localDT = localDate.Add(localTimeSpan);
			var zone = Settings.TimeZoneId ?? "Central Standard Time";
			var now = DateTime.UtcNow;
			return localTimeSpan - TimeZoneInfo.FindSystemTimeZoneById(zone).GetUtcOffset(now);
			//return ConvertToUTC(localDT).Subtract(localDate);
		}
		public virtual TimeSpan ConvertFromUTC(TimeSpan localTimeSpan)
		{
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
		public virtual OrganizationSettings Settings{
			get{
				if(_Settings==null )
					_Settings =new OrganizationSettings();
				return _Settings;
			}
		}


		public virtual IList<UserOrganizationModel> Members { get; set; }
		public virtual IList<PaymentModel> Payments { get; set; }
		public virtual IList<InvoiceModel> Invoices { get; set; }
		public virtual IList<QuestionModel> CustomQuestions { get; set; }
		public virtual IList<QuestionCategoryModel> QuestionCategories { get; set; }
		//public virtual IList<IndustryModel> Industries { get; set; }
		public virtual IList<GroupModel> Groups { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreationTime { get; set; }
		public virtual bool SendEmailImmediately { get; set; }
		public virtual String ImageUrl { get; set; }

		public virtual IList<ReviewsModel> Reviews { get; set; }

		public virtual OriginType GetOriginType()
		{
			return OriginType.Organization;
		}

		public virtual PaymentPlanModel PaymentPlan { get; set; }

		public virtual String GetSpecificNameForOrigin()
		{
			return Name.Translate();
		}

		public virtual bool ManagersCanEditPositions { get; set; }
		

		public OrganizationModel()
		{
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

		}

		public virtual List<IOrigin> OwnsOrigins()
		{
			var owns = new List<IOrigin>();
			owns.AddRange(CustomQuestions.Cast<IOrigin>().ToList());
			owns.AddRange(QuestionCategories.Cast<IOrigin>().ToList());
			owns.AddRange(Groups.Cast<IOrigin>().ToList());
			owns.AddRange(Members.Cast<IOrigin>().ToList());
			owns.AddRange(Members.Cast<IOrigin>().ToList());

			return owns;
		}

		public virtual List<IOrigin> OwnedByOrigins()
		{
			var ownedBy = new List<IOrigin>();
			return ownedBy;
		}

		public override string GetName()
		{
			return Name.Translate();
		}
		public override string GetImageUrl()
		{
			return ImageUrl??base.GetImageUrl();
		}

		public override string GetGroupType()
		{
			return DisplayNameStrings.organization;
		}


		public class OrganizationModelMap : SubclassMap<OrganizationModel>
		{
			public OrganizationModelMap()
			{
				Map(x => x.ManagersCanEdit);
				Map(x => x.DeleteTime);
				Map(x => x.ImageUrl);
				Map(x => x.CreationTime);
				Map(x => x.StrictHierarchy);
				Map(x => x.ManagersCanEditPositions);
				Map(x => x.ManagersCanRemoveUsers);
				//Map(x => x.ImageUrl);
				Map(x => x.SendEmailImmediately);
				Component(x => x._Settings).ColumnPrefix("Settings_");

				References(x => x.Image).Not.LazyLoad().Cascade.SaveUpdate();
				References(x => x.Name).Not.LazyLoad().Cascade.SaveUpdate();
				References(x => x.PaymentPlan).Cascade.SaveUpdate();

				HasMany(x => x.Reviews)
					.Cascade.SaveUpdate();
				HasMany(x => x.Members)
					.KeyColumn("Organization_Id")
					.Cascade.SaveUpdate();
				HasMany(x => x.Payments)
					.Cascade.SaveUpdate();
				HasMany(x => x.Invoices)
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