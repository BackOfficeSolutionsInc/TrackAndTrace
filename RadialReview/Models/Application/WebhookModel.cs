using FluentNHibernate.Mapping;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models {


	public class WebhookDetails : IHistorical {

		public WebhookDetails() {
			CreateTime = DateTime.UtcNow;
		}

		public virtual string Id { get; set; }
		public virtual string Email { get; set; }

		public virtual string UserId { get; set; }

		[JsonIgnore]
		public virtual UserModel User { get; set; }

		public virtual string ProtectedData { get; set; }

		public virtual IList<WebhookEventsSubscription> WebhookEventsSubscription { get; set; }

		public virtual DateTime CreateTime { get; set; }				
		public virtual DateTime? DeleteTime { get; set; }

		public class Map : ClassMap<WebhookDetails> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.Email).Length(256);
				References(x => x.User).Column("UserId").LazyLoad().ReadOnly();
				Map(x => x.UserId).Length(64);
				Map(x => x.ProtectedData);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				HasMany(x => x.WebhookEventsSubscription).LazyLoad();
			}
		}
	}

	//public class WebhookEvents {
	//	public virtual long Id { get; set; }
	//	public virtual string Name { get; set; }
	//	public virtual string Description { get; set; }

	//	public virtual WebhookEventType EventType { get; set; }
	//	public virtual long KeyColumn { get; set; } // either L10, UserId, OrganizationID
	//	public class Map : ClassMap<WebhookEvents> {
	//		public Map() {
	//			Id(x => x.Id);
	//			Map(x => x.Name).Length(256);
	//			//Map(x => x.EventType).CustomType<WebhookEventType>();
	//			//Map(x => x.KeyColumn);
	//			Map(x => x.Description).Length(256);
	//		}
	//	}
	//}

	public enum WebhookEventType {
		[Description("Add TODO to L10_")]
		AddTODOtoL10,
		[Description("Add TODO to Organization_")]
		AddTODOtoOrganization,
		[Description("Add TODO for User_")]
		AddTODOforUser,

		[Description("Checking/Unchecking/Closing TODO to L10_")]
		Checking_Unchecking_Closing_TODOtoL10,

		[Description("Checking/Unchecking/Closing TODO for User_")]
		Checking_Unchecking_Closing_TODOforUser,

		[Description("Changing TODO to L10_")]
		ChangingToDotoL10,

		[Description("Changing TODO for User_")]
		ChangingToDoforUser,

		[Description("Checking/Unchecking/Closing TODO to Organization_")]
		Checking_Unchecking_Closing_TODOtoOrganization,

		[Description("Changing TODO to Organization_")]
		ChangingTODOtoOrganization,

		[Description("Add Issue to L10_")]
		AddIssuetoL10,
		[Description("Add Issue to Organization_")]
		AddIssuetoOrganization,
		[Description("Add Issue for User_")]
		AddIssueforUser,

		[Description("Checking/Unchecking/Closing Issue to L10_")]
		Checking_Unchecking_Closing_IssuetoL10,

		[Description("Checking/Unchecking/Closing Issue for User_")]
		Checking_Unchecking_Closing_IssueforUser,

		[Description("Changing Issue to L10_")]
		ChangingIssuetoL10,

		[Description("Changing Issue for User_")]
		ChangingIssueforUser,

		[Description("Checking/Unchecking/Closing Issue to Organization_")]
		Checking_Unchecking_Closing_IssuetoOrganization,

		[Description("Changing Issue to Organization_")]
		ChangingIssuetoOrganization

	}
	public class WebhookEventsSubscription : IHistorical {
		public virtual long Id { get; set; }
		public virtual string WebhookId { get; set; }		
		public virtual string EventName { get; set; }
		public virtual WebhookDetails Webhook { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public class Map : ClassMap<WebhookEventsSubscription> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.WebhookId);
				Map(x => x.EventName);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				References(x => x.Webhook).Column("WebhookId").LazyLoad().ReadOnly();
			}
		}
	}
}