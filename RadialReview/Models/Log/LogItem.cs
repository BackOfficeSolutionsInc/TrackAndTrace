using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using MathNet.Numerics.Distributions;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using TrelloNet;

namespace RadialReview.Models.Log
{
	public enum InteractionType
	{
		InitialContact=1,
		FollowUp=2,
		InitialDemo=3,
		InitialL10=4,
		AdminDemo=5,
		Checkup=6,
		Support=7,
		BugFix=8,
		FeatureAddition=9,
		Cancellation=10,
		Other=100,
	}
	public enum InteractionMethodType
	{
		Email=1,
		Call=2,
		VideoConference=3,
		Software=4,
		Other=100
	}

	public class InteractionUtility
	{
		public static bool IsSupport(InteractionType type)
		{
			var TRUE = true;
			switch (type)
			{
				case InteractionType.InitialContact: return false;
				case InteractionType.FollowUp: return false;
				case InteractionType.InitialDemo: return TRUE;
				case InteractionType.InitialL10: return TRUE;
				case InteractionType.AdminDemo: return TRUE;
				case InteractionType.Checkup: return false;
				case InteractionType.Support: return TRUE;
				case InteractionType.BugFix: return false;
				case InteractionType.FeatureAddition: return false;
				case InteractionType.Cancellation: return false;
				case InteractionType.Other: return false;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static bool IsDev(InteractionType type)
		{
			var TRUE = true;
			switch (type)
			{
				case InteractionType.InitialContact: return false;
				case InteractionType.FollowUp: return false;
				case InteractionType.InitialDemo: return false;
				case InteractionType.InitialL10: return false;
				case InteractionType.AdminDemo: return false;
				case InteractionType.Checkup: return false;
				case InteractionType.Support: return false;
				case InteractionType.BugFix: return TRUE;
				case InteractionType.FeatureAddition: return TRUE;
				case InteractionType.Cancellation: return false;
				case InteractionType.Other: return false;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		internal static bool IsDemo(AccountType type)
		{
			var TRUE = true;
			switch(type){
				case AccountType.Paying:		 return false;			
				case AccountType.Implementer:	 return TRUE;
				case AccountType.Demo:			 return TRUE;
				case AccountType.Other:			 return TRUE;
				case AccountType.Dormant:		 return TRUE;	
				case AccountType.Cancelled:		 return TRUE;	
				default: throw new ArgumentOutOfRangeException("type");
			}
		}
	}


	public class  InteractionLogItem : ILongIdentifiable,IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime LogDate { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual InteractionType InteractionType { get; set; }
		public virtual InteractionMethodType InteractionMethod { get; set; }
		public virtual String Notes { get; set; }

		public virtual decimal Duration { get; set; }

		public virtual long? UserId { get; set; }
		public virtual string Name { get; set; }
		public virtual string Email { get; set; }
		public virtual string Company { get; set; }

		public virtual DateTime? DeleteTime { get; set; }
		public virtual AccountType? AccountType { get; set; }


		public InteractionLogItem()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class CMap : ClassMap<InteractionLogItem>
		{
			public CMap()
			{
				Id(x => x.Id);
				Map(x => x.Duration);
				Map(x => x.DeleteTime);
				Map(x => x.LogDate);
				Map(x => x.CreateTime);
				Map(x => x.CreatedBy);
				Map(x => x.InteractionType).CustomType<InteractionType>();
				Map(x => x.InteractionMethod).CustomType<InteractionMethodType>();
				Map(x => x.Notes);
				Map(x => x.UserId);
				Map(x => x.Name);
				Map(x => x.Email);
				Map(x => x.Company);
				Map(x => x.AccountType).CustomType<AccountType>();

			}
		}
	}
}