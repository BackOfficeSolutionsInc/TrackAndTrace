using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{

	public class CallablePhoneNumber : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }

		public DateTime CreateTime { get; set; }

		public DateTime? DeleteTime { get; set; }

		public long Number { get; set; }

		public CallablePhoneNumber()
		{
			CreateTime = DateTime.UtcNow;
		}
		public class MMap : ClassMap<CallablePhoneNumber>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Number);
			}
		}
	}

	public class PhoneTextModel : ILongIdentifiable
	{
		public virtual long Id { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual String Message { get; set; }
		public virtual long FromNumber { get; set; }
		public virtual UserOrganizationModel FromUser { get; set; }

		public PhoneTextModel()
		{
			Date = DateTime.UtcNow;
		}

		public class MMap: ClassMap<PhoneTextModel>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.Date);
				Map(x => x.Message);
				Map(x => x.FromNumber);
				References(x => x.FromUser).Nullable().ReadOnly();
			}
		}
	}

	public class PhoneActionMap : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }

		public DateTime CreateTime { get; set; }

		public DateTime? DeleteTime{ get; set; }

		public UserOrganizationModel Caller { get; set; }
		public String Action { get; set; }
		public long ForId { get; set; }
		public long CallerNumber { get; set; }
		public long SystemNumber { get; set; }

		public PhoneActionMap()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class MMap : ClassMap<PhoneActionMap>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.ForId);
				Map(x => x.Action);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CallerNumber);
				Map(x => x.SystemNumber);
				References(x => x.Caller).Not.LazyLoad().Not.Nullable().ReadOnly();
			}

		}
	}
}