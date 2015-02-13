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

namespace RadialReview.Models
{
	public class OrganizationModel : ResponsibilityGroupModel, IOrigin, IDeletable
	{
		public class OrganizationSettings
		{
			public virtual DayOfWeek WeekStart { get; set; }
			public virtual int TimeZoneOffsetMinutes { get; set; }
			public virtual bool EmployeesCanViewScorecard { get; set; }
			public virtual bool ManagersCanViewScorecard { get; set; }
			public virtual bool EmployeeCanCreateL10 { get; set; }
			public virtual bool ManagersCanCreateL10 { get; set; }
			public virtual bool ManagersCanViewSubordinateL10 { get; set; }
			public virtual bool ManagersCanEditSubordinateL10 { get; set; }

			public OrganizationSettings()
			{
				TimeZoneOffsetMinutes = -360;
				WeekStart= DayOfWeek.Sunday;

				EmployeesCanViewScorecard = false;
				ManagersCanViewScorecard = true;

				EmployeeCanCreateL10 = false;
				ManagersCanCreateL10 = true;

				ManagersCanViewSubordinateL10 = true;
				ManagersCanEditSubordinateL10 = false;
			}

			public class OrgSettingsVM : ComponentMap<OrganizationSettings>
			{
				public OrgSettingsVM()
				{
					Map(x => x.WeekStart);
					Map(x => x.TimeZoneOffsetMinutes);

					Map(x => x.EmployeesCanViewScorecard);
					Map(x => x.ManagersCanViewScorecard);


					Map(x => x.EmployeeCanCreateL10);
					Map(x => x.ManagersCanCreateL10);

					Map(x => x.ManagersCanViewSubordinateL10);
					Map(x => x.ManagersCanEditSubordinateL10);

				}
			}


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
		public virtual OrganizationSettings Settings
		{
			get
			{
				return _Settings ?? new OrganizationSettings();
			}
		}


		public virtual IList<UserOrganizationModel> Members { get; set; }
		public virtual IList<PaymentModel> Payments { get; set; }
		public virtual IList<InvoiceModel> Invoices { get; set; }
		public virtual IList<QuestionModel> CustomQuestions { get; set; }
		public virtual IList<QuestionCategoryModel> QuestionCategories { get; set; }
		public virtual IList<IndustryModel> Industries { get; set; }
		public virtual IList<GroupModel> Groups { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreationTime { get; set; }
		public virtual bool SendEmailImmediately { get; set; }

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
			Industries = new List<IndustryModel>();
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
				HasMany(x => x.Industries)
					.KeyColumn("OrganizationId")
					.Inverse();
				HasMany(x => x.QuestionCategories)
					.KeyColumn("OrganizationId")
					.Inverse();
				HasMany(x => x.Groups)
					.Inverse();

				HasMany(x => x.CustomQuestions)
					.KeyColumn("OrganizationQuestion_Id")
					.Inverse();
			}
		}
	}
}