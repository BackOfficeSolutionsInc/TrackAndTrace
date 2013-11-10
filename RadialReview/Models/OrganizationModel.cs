using FluentNHibernate.Mapping;
using NHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class OrganizationModel : IOrigin, IDeletable
    {
        public virtual long Id { get; set; }

        [Display(Name = "organizationName", ResourceType = typeof(DisplayNameStrings))]
        public virtual string Name { get; set; }

        [Display(Name = "imageUrl", ResourceType = typeof(DisplayNameStrings))]
        public virtual string ImageUrl
        {
            get { return _ImageUrl ?? ConstantStrings.ImagePlaceholder; }
            set { _ImageUrl = value; }
        }
        private string _ImageUrl { get; set; }

        [Display(Name="managerCanAddQuestions",ResourceType=typeof(DisplayNameStrings))]
        public virtual Boolean ManagersCanEdit { get; set; }
        public virtual IList<UserOrganizationModel> Members { get; set; }
        public virtual IList<PaymentModel> Payments { get; set; }
        public virtual IList<InvoiceModel> Invoices { get; set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public virtual IList<QuestionCategoryModel> QuestionCategories { get; set; }
        public virtual IList<IndustryModel> Industries { get; set; }
        public virtual IList<GroupModel> Groups { get;set;}
        public virtual DateTime? DeleteTime { get; set; }
        public virtual OriginType QuestionOwnerType()
        {
            return OriginType.Organization;
        }

        public virtual String OriginCustomName
        {
            get
            {
                return Name;
            }
        }

        
        public OrganizationModel()
        {
            Groups = new List<GroupModel>();
            Payments = new List<PaymentModel>();
            Invoices = new List<InvoiceModel>();
            CustomQuestions = new List<QuestionModel>();
            Members = new List<UserOrganizationModel>();
            Industries = new List<IndustryModel>();
            QuestionCategories = new List<QuestionCategoryModel>();
        }

    }

    public class OrganizationModelMap : ClassMap<OrganizationModel>
    {
        public OrganizationModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Name);
            Map(x => x.ManagersCanEdit);
            Map(x => x.DeleteTime);
            Map(x => x.ImageUrl);
            HasMany(x => x.Members)
                .KeyColumn("Organization_Id")
                .Inverse();
            HasMany(x => x.Payments)
                .Cascade.SaveUpdate();
            HasMany(x => x.Invoices)
                .Cascade.SaveUpdate();
            HasMany(x => x.Industries)
                .KeyColumn("OrganizationId")
                .Cascade.SaveUpdate();
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