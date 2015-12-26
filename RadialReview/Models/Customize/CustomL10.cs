using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Customize
{
	public class CustomText : ILongIdentifiable,IHistorical
	{
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }
		public virtual String NewText { get; set; }
		public virtual String PropertyName { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }


		public virtual List<SelectListItem> _PossibleItems { get; set; } 

		public CustomText()
		{
			CreateTime = DateTime.UtcNow;
		}
		public class CustomTextMap : ClassMap<CustomText>
		{
			public CustomTextMap()
			{
				Id(x => x.Id);
				Map(x => x.OrgId);
				Map(x => x.NewText);
				Map(x => x.PropertyName);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
			}
		}

	}
}