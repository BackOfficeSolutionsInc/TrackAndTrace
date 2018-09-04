using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Dynamic;
using RadialReview.Utilities;
using RadialReview.Properties;
using RadialReview.Accessors;

namespace RadialReview.Models.L10 {
	public class PeopleHeadline : ILongIdentifiable, IHistorical, IIssue, ITodo {
		public virtual long Id { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual long OwnerId { get; set; }
		public virtual UserOrganizationModel Owner { get; set; }
		public virtual long? AboutId { get; set; }
		public virtual ResponsibilityGroupModel About { get; set; }
		public virtual string AboutName { get; set; }
		public virtual string AboutIdText { get; set; }
		public virtual string Message { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual DateTime? CloseTime { get; set; }

		public virtual long RecurrenceId { get; set; }
		public virtual long? CreatedDuringMeetingId { get; set; }
		public virtual long? CloseDuringMeetingId { get; set; }

		public virtual string HeadlinePadId { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual long Ordering { get; set; }

		public virtual string _Details { get; set; }
		public virtual string _Notes { get; set; }

		public PeopleHeadline() {
			CreateTime = DateTime.UtcNow;
			HeadlinePadId = "-" + Guid.NewGuid().ToString();
		}

		public class Map : ClassMap<PeopleHeadline> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreatedBy);
				Map(x => x.OwnerId).Column("OwnerId");
				References(x => x.Owner).LazyLoad().ReadOnly().Column("OwnerId");
				Map(x => x.AboutId).Column("AboutId");
				References(x => x.About).LazyLoad().ReadOnly().Nullable().Column("AboutId");
				Map(x => x.AboutName);
				Map(x => x.Message);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CloseTime);
				Map(x => x.RecurrenceId);
				Map(x => x.CreatedDuringMeetingId);
				Map(x => x.CloseDuringMeetingId);
				Map(x => x.HeadlinePadId);
				Map(x => x.Ordering);
				Map(x => x.OrganizationId);
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public virtual async Task<string> GetIssueMessage() {
			return Message;
		}

		public virtual async Task<string> GetIssueDetails() {

			var notes = _Notes ?? "n/a";
			var aboutName = About.NotNull(x => x.GetName()) ?? AboutName ?? "n/a";
			var owner = Owner.NotNull(x => x.GetName()) ?? "n/a";
			return "ABOUT: " + aboutName + "\n\nOwner: " + owner + "\n\nNotes: " + notes;

		}

		public virtual async Task<string> GetTodoMessage() {
			return "";
		}

		public virtual async Task<string> GetTodoDetails() {
			var notes = _Notes ?? "n/a";
			var aboutName = About.NotNull(x => x.GetName()) ?? AboutName ?? "n/a";
			var owner = Owner.NotNull(x => x.GetName()) ?? "n/a";
			return "MESSAGE: " + Message + "\n\nABOUT: " + aboutName + "\n\nOwner: " + owner + "\n\nNotes: " + notes;
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		public virtual ExpandoObject ToRow() {
			dynamic o = new ExpandoObject();

			o.CreateTime = CreateTime;
			o.CloseTime = CloseTime;
			o.Message = Message;
			o.Owner = new ExpandoObject();
			o.Owner.Id = Owner.Id;
			o.Owner.Name = Owner.GetName();
			o.Owner.ImageUrl = Owner.ImageUrl(awsFaster: true, size: ImageSize._32);

			o.About = new ExpandoObject();
			o.About.Id = About.NotNull(x => (long?)x.Id) ?? AboutId;
			o.About.ImageUrl = About.NotNull(x => x.GetImageUrl()) ?? (ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder);
			o.About.Name = About.NotNull(x => x.GetName()) ?? AboutName;

			o.HeadlinePadId = HeadlinePadId;

			o.RecurrenceId = RecurrenceId;
			o.MeetingId = CreatedDuringMeetingId;

			o.Id = Id;

			o.DetailsUrl = Config.NotesUrl() + "p/" + HeadlinePadId + "?showControls=true&showChat=false";


			return o;

		}
	}
}