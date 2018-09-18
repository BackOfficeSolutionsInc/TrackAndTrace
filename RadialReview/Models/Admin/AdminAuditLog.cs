using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.Admin {
	public enum AdminAccessLevel {
		View,
		SetAs
	}

	public class AdminAccessViewModel {
		public string ReturnUrl { get; set; }
		public string SourceLink { get; set; }

		[Required]
		public string Reason { get; set; }
		[Range(1, long.MaxValue)]
		public long AccessId { get; set; }
		[Required]
		public string SetAsEmail { get; set; }

		[Required]
		public AdminAccessLevel? AccessLevel { get; set; }
		[Range(1, 4320)]
		public int RequestedDurationMinutes { get; set; }
		public List<SelectListItem> Durations { get; set; }
		public List<SelectListItem> PotentialReasons { get; set; }

		public string AccessUser { get; set; }
		public string AccessOrganization { get; set; }

		public AdminAccessViewModel(long viewUserId) : this(viewUserId, "-view-") { }
		public AdminAccessViewModel(string setAsEmail) : this(long.MaxValue, setAsEmail) { }

		[Obsolete("Do not use")]
		public AdminAccessViewModel() { }

		private AdminAccessViewModel(long viewUserId, string setAsUserId) {
			AccessId = viewUserId;
			SetAsEmail = setAsUserId;

			PotentialReasons = new List<SelectListItem>() {
				new SelectListItem() {Text = "Other", Value="" },
				new SelectListItem() {Selected = true, Text = "Please select a reason...", Value="" },
                new SelectListItem() {Text = "I need to perform a walk-through", Value="I need to perform a walk-through" },
                new SelectListItem() {Text = "I need to replicate a client's bug", Value="I need to replicate a client's bug" },
				new SelectListItem() {Text = "I need to help a client export data", Value="I need to help a client export data" },
			};

			Durations = new List<SelectListItem>() {
				new SelectListItem() { Text = "5 minutes", Value = "5" },
				new SelectListItem() { Text = "10 minutes", Value = "10" },
				new SelectListItem() { Text = "30 minutes", Value = "30" },
				new SelectListItem() { Text = "1 hour", Value = "60" },
				new SelectListItem() { Text = "2 hours", Value = "120" },
				new SelectListItem() { Text = "4 hours", Value = "240" },
				new SelectListItem() { Text = "8 hours", Value = "480" },
				new SelectListItem() { Text = "1 days", Value = "1440" },
				new SelectListItem() { Text = "2 days", Value = "2880" },
				new SelectListItem() { Text = "3 days", Value = "4320" },
			};
		}


		public void EnsureValid() {
			var context = new ValidationContext(this, null, null);
			var results = new List<ValidationResult>();
			if (!Validator.TryValidateObject(this, context, results, true))
				throw new ValidationException("Invalid");
		}

		public AdminAccessModel ToDatabaseModel(string adminUserId) {
			if (string.IsNullOrWhiteSpace(adminUserId))
				throw new ArgumentOutOfRangeException("Invalid admin id ");
			EnsureValid();
			var createTime = DateTime.UtcNow;
			return new AdminAccessModel() {
				CreateTime = createTime,
				DeleteTime = createTime.AddMinutes(RequestedDurationMinutes),
				AccessId = AccessId,
				AdminUserId = adminUserId,
				AccessLevel = AccessLevel.Value,
				Reason = Reason,
				SourceLink = SourceLink,
				SetAsEmail = SetAsEmail
			};
		}
	}

	public enum AuditStatus {
		Passed,
		Flagged,
		Failed,
	}

	public class AdminAuditLog : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long AdminAccessLogId { get; set; }
		public virtual long AuditedBy { get; set; }
		public virtual DateTime AuditTime { get; set; }
		public virtual AuditStatus Status { get; set; }
	}

	public class AdminAccessModel : ILongIdentifiable {

		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime DeleteTime { get; set; }
		public virtual string Reason { get; set; }
		public virtual string SourceLink { get; set; }

		public virtual AdminAccessLevel AccessLevel { get; set; }
		public virtual string AdminUserId { get; set; }
		public virtual long AccessId { get; set; }
		public virtual string SetAsEmail { get; set; }


		public virtual string _AdminName { get; set; }
		public virtual string _AccessName { get; set; }
		public virtual string _AccessOrganization { get; set; }

		public AdminAccessModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<AdminAccessModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Reason);
				Map(x => x.SourceLink);
				Map(x => x.AdminUserId);
				Map(x => x.AccessLevel).CustomType<AdminAccessLevel>();
				Map(x => x.AccessId);
				Map(x => x.SetAsEmail);
			}
		}
	}
}