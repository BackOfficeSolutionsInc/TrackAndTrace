using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using FluentNHibernate.Mapping;

namespace RadialReview.Models.Pdf
{
	public enum PdfType {
		Pdf,
		Report,
	}

	public class PdfModel : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual string Filename { get; set; }
		public virtual string FileId { get; set; }
		public virtual PdfType PdfType { get; set; }

		public virtual string GetPath() {
			return PdfType + "/" + FileId + ".pdf";
		}

		public PdfModel() {
			CreateTime = DateTime.UtcNow;
			FileId = Guid.NewGuid().ToString();
		}

		public class Map : ClassMap<PdfModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.CreatedBy);
				Map(x => x.FileId);
				Map(x => x.PdfType);
				Map(x => x.Filename);
			}
		}
	}

	public class ReportPdfModel : PdfModel
	{
		public virtual long ForUserId { get; set; }
		public virtual long ForReviewId { get; set; }
		public virtual long ForReviewContainerId { get; set; }
		public virtual bool Finalized { get; set; }
		public virtual bool Signed { get; set; }
		public virtual bool Sent { get; set; }

		public ReportPdfModel() {
			Finalized = true;
		}

		public class SubMap : SubclassMap<ReportPdfModel> {
			public SubMap() {
				Map(x => x.ForUserId);
				Map(x => x.ForReviewId);
				Map(x => x.ForReviewContainerId);
				Map(x => x.Finalized);
				Map(x => x.Signed);
				Map(x => x.Sent);
			}
		}
	}
}