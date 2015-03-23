using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Pdf
{

	public class PdfModel : ILongIdentifiable, IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual DateTime CreatedTime { get; set; }
		public virtual string Location { get; set; }
	}

	public class ReportPdfModel : PdfModel
	{
		public virtual long ForUserId { get; set; }
	}
}