using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using Newtonsoft.Json;
using RadialReview.Utilities.DataTypes;
using RadialReview.Accessors;

namespace RadialReview.Models
{

	public class CallablePhoneNumber : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }

		public virtual DateTime CreateTime { get; set; }

		public virtual DateTime? DeleteTime { get; set; }

		public virtual long Number { get; set; }

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

		public virtual DateTime CreateTime { get; set; }

		public virtual DateTime? DeleteTime { get; set; }

		public virtual long CallerId { get; set; }
		public virtual UserOrganizationModel Caller { get; set; }
		public virtual String Action { get; set; }
		/// <summary>
		/// RecurrenceId
		/// </summary>
		public virtual long ForId { get; set; }
		public virtual long CallerNumber { get; set; }
		public virtual long SystemNumber { get; set; }


		public virtual string _SystemNumberFormatted { get { return SystemNumber.ToPhoneNumber(); } }
		public virtual string _CallerNumberFormatted { get { return CallerNumber.ToPhoneNumber(); } }
		public virtual string _RecurrenceName { get { return _Recurrence.NotNull(x => x.Name); } }
		public virtual string _ActionName { get { return PhoneAccessor.PossibleActions[Action]; } }




		[JsonIgnore]
		public virtual L10Recurrence _Recurrence { get; set; }

		public virtual string Placeholder { get; set; }

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
				Map(x => x.Placeholder);
				Map(x => x.CallerId).Column("Caller_id");
				References(x => x.Caller).Column("Caller_id").Not.LazyLoad().Not.Nullable().ReadOnly();
			}

		}
	}
}