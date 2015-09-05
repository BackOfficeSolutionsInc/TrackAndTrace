﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Envers.Configuration.Fluent;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using Remotion.Linq.Clauses;

namespace RadialReview.Models.VTO
{
	public enum VtoItemType:int
	{
		Field=0,
		List_Uniques,
		List_LookLike,
		List_YearGoals,

	}

	public class VtoModel : ILongIdentifiable,IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long? CopiedFrom { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual bool OrganizationWide { get; set; }
		public virtual String Name { get; set; }
		public virtual List<CompanyValueModel> _Values { get; set; }

		public virtual CoreFocusModel CoreFocus { get; set; }
		public virtual MarketingStrategyModel MarketingStrategy{ get; set; }
		public virtual ThreeYearPictureModel ThreeYearPicture { get; set; }
		public virtual OneYearPlanModel OneYearPlan { get; set; }
		public virtual QuarterlyRocksModel QuarterlyRocks { get; set; }

		public VtoModel()
		{
			CreateTime = DateTime.UtcNow;
			//OrganizationWide = new VtoItem_Bool();
			CoreFocus = new CoreFocusModel();
			MarketingStrategy = new MarketingStrategyModel();
			//Name; //= new VtoItem_String();
			_Values = new List<CompanyValueModel>();
			ThreeYearPicture = new ThreeYearPictureModel();
			OneYearPlan = new OneYearPlanModel();
			QuarterlyRocks = new QuarterlyRocksModel();
		}

		public class VtoModelMap : ClassMap<VtoModel>
		{
			public VtoModelMap()
			{
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CopiedFrom);
				References(x => x.Organization).Not.Nullable().LazyLoad();
				//References(x => x.OrganizationWide).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				Map(x => x.OrganizationWide);

				//References(x => x.Name).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				Map(x => x.Name);

				References(x => x.CoreFocus).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				References(x => x.MarketingStrategy).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				References(x => x.ThreeYearPicture).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				References(x => x.OneYearPlan).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				References(x => x.QuarterlyRocks).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
			}
		}

		#region Core Focus
		public class CoreFocusModel : ILongIdentifiable
		{
			public virtual long Id { get; set; }
			public virtual VtoModel Vto { get; set; }
			public virtual VtoItem_String Purpose { get; set; }
			public virtual VtoItem_String Niche { get; set; }

			public CoreFocusModel()
			{
				Purpose = new VtoItem_String();
				Niche = new VtoItem_String();
			}

			public class CoreFocusMap : ClassMap<CoreFocusModel>
			{
				public CoreFocusMap()
				{
					Id(x => x.Id);
					References(x => x.Vto).Nullable().LazyLoad();
					References(x => x.Purpose).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Niche).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();

				}
			}

		}

		#endregion
		#region Marketing Strategy

		public class MarketingStrategyModel : ILongIdentifiable
		{
			public virtual long Id { get; set; }
			public virtual VtoModel Vto { get; set; }
			public virtual VtoItem_String TenYearTarget { get; set; }
			public virtual VtoItem_String TargetMarket { get; set; }
			public virtual VtoItem_String ProvenProcess { get; set; }
			public virtual VtoItem_String Guarantee { get; set; }
			public virtual List<VtoItem_String> _Uniques { get; set; }

			public MarketingStrategyModel()
			{
				TenYearTarget=new VtoItem_String();
				TargetMarket = new VtoItem_String();
				ProvenProcess = new VtoItem_String();
				Guarantee = new VtoItem_String();
				_Uniques = new List<VtoItem_String>();
			}
			public class MarketingStrategyMap : ClassMap<MarketingStrategyModel>
			{
				public MarketingStrategyMap()
				{
					Id(x => x.Id);
					References(x => x.Vto).Nullable().LazyLoad();
					References(x => x.TenYearTarget).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.TargetMarket).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.ProvenProcess).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Guarantee).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();

				}
			}
		}

		#endregion
		#region 3 Year Picture
		public class ThreeYearPictureModel : ILongIdentifiable
		{
			public virtual long Id { get; set; }
			public virtual VtoModel Vto { get; set; }
			public virtual VtoItem_DateTime FutureDate { get; set; }
			public virtual VtoItem_Decimal Revenue { get; set; }
			public virtual VtoItem_Decimal Profit { get; set; }
			public virtual VtoItem_String Measurables { get; set; }
			public virtual List<VtoItem_String> _LooksLike { get; set; }

			public ThreeYearPictureModel()
			{
				FutureDate = new VtoItem_DateTime();
				Revenue = new VtoItem_Decimal();
				Profit = new VtoItem_Decimal();
				Measurables = new VtoItem_String();
				_LooksLike = new List<VtoItem_String>();
			}

			public class ThreeYearPictureMap : ClassMap<ThreeYearPictureModel>
			{
				public ThreeYearPictureMap()
				{
					Id(x => x.Id);
					References(x => x.Vto).Nullable().LazyLoad();
					References(x => x.FutureDate).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Revenue).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Profit).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Measurables).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				}
			}

		}
		#endregion
		#region 1 Year Plan
		public class OneYearPlanModel : ILongIdentifiable
		{
			public virtual long Id { get; set; }
			public virtual VtoModel Vto { get; set; }
			public virtual VtoItem_DateTime FutureDate { get; set; }
			public virtual VtoItem_Decimal Revenue { get; set; }
			public virtual VtoItem_Decimal Profit { get; set; }
			public virtual VtoItem_String Measurables { get; set; }
			public virtual List<VtoItem_String> _GoalsForYear { get; set; }

			public OneYearPlanModel()
			{
				FutureDate = new VtoItem_DateTime();
				Revenue = new VtoItem_Decimal();
				Profit = new VtoItem_Decimal();
				Measurables = new VtoItem_String();
				_GoalsForYear = new List<VtoItem_String>();
			}

			public class OneYearPlanMap : ClassMap<OneYearPlanModel>
			{
				public OneYearPlanMap()
				{
					Id(x => x.Id);
					References(x => x.Vto).Nullable().LazyLoad();
					References(x => x.FutureDate).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Revenue).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Profit).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Measurables).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				}
			}
		}
		#endregion
		#region Quarterly Rocks
		public class QuarterlyRocksModel : ILongIdentifiable
		{
			public virtual long Id { get; set; }
			public virtual VtoModel Vto { get; set; }
			public virtual VtoItem_DateTime FutureDate { get; set; }
			public virtual VtoItem_Decimal Revenue { get; set; }
			public virtual VtoItem_Decimal Profit { get; set; }
			public virtual VtoItem_String Measurables { get; set; }
			public virtual List<Vto_Rocks> _Rocks { get; set; }

			public QuarterlyRocksModel()
			{
				FutureDate = new VtoItem_DateTime();
				Revenue=new VtoItem_Decimal();
				Profit = new VtoItem_Decimal();
				Measurables = new VtoItem_String();
				_Rocks = new List<Vto_Rocks>();
			}
			public class QuarterlyRocksMap : ClassMap<QuarterlyRocksModel>
			{
				public QuarterlyRocksMap()
				{
					Id(x => x.Id);
					References(x => x.Vto).Nullable().LazyLoad();
					References(x => x.FutureDate).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Revenue).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Profit).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
					References(x => x.Measurables).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
				}
			}

		}
		#endregion

		#region VtoItems
		public abstract class VtoItem: ILongIdentifiable, IHistorical
		{
			public virtual long Id { get; set; }
			public virtual long BaseId { get; set; }
			public virtual VtoModel Vto { get; set; }
			public virtual long? CopiedFrom { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual VtoItemType Type { get; set; }
			public virtual int Ordering { get; set; }

			protected VtoItem()
			{
				CreateTime = DateTime.UtcNow;
			}

			public class VtoItemMap : ClassMap<VtoItem>
			{
				public VtoItemMap()
				{
					Id(x => x.Id);
					Map(x => x.BaseId);
					References(x => x.Vto).Nullable().LazyLoad();
					Map(x => x.CopiedFrom);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.Type).CustomType<VtoItemType>();
					Map(x => x.Ordering);
				}
			}
		}

		public class VtoItem_String : VtoItem
		{
			public virtual String Data { get; set; }
			public class VtoItem_StringMap : SubclassMap<VtoItem_String>
			{
				public VtoItem_StringMap(){
					Map(x => x.Data);
				}
			}
			public override string ToString(){
				return Data??"";
			}
		}

		public class VtoItem_Decimal : VtoItem
		{
			public virtual decimal? Data { get; set; }
			public class VtoItem_DecimalMap : SubclassMap<VtoItem_Decimal>
			{
				public VtoItem_DecimalMap()
				{
					Map(x => x.Data);
				}
			}
			public override string ToString()
			{
				return Data.NotNull(x => String.Format("{0.00##}", x)) ?? "";
			}
		}
		public class VtoItem_DateTime : VtoItem
		{
			public virtual DateTime? Data { get; set; }
			public class VtoItem_DateTimeMap : SubclassMap<VtoItem_DateTime>{
				public VtoItem_DateTimeMap()
				{
					Map(x => x.Data);
				}
			}
			public override string ToString(){
				return Data.NotNull(x => x.Value.ToShortDateString()) ?? "";
			}
		}
		public class VtoItem_Bool : VtoItem
		{
			public VtoItem_Bool()
			{
				
			}

			public virtual bool Data { get; set; }
			public class VtoItem_BoolMap : SubclassMap<VtoItem_Bool>{
				public VtoItem_BoolMap(){
					Map(x => x.Data);
				}
			}
			public override string ToString(){
				return Data?"Yes":"No";
			}
		}

		#endregion
		#region Rocks
		public class Vto_Rocks : ILongIdentifiable, IHistorical
		{
			public virtual long Id { get; set; }

			public virtual VtoModel Vto { get; set; }
			public virtual long? CopiedFrom { get; set; }

			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual int _Ordering { get; set; }
			public virtual RockModel Rock { get; set; }

			public Vto_Rocks()
			{
				CreateTime = DateTime.UtcNow;
			}

			public class Vto_RocksMap : ClassMap<Vto_Rocks>
			{
				public Vto_RocksMap()
				{
					Id(x => x.Id);
					References(x => x.Vto).Nullable().LazyLoad();
					Map(x => x.CopiedFrom);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x._Ordering);
					References(x => x.Rock).Not.Nullable().Not.LazyLoad();
				}
			}
		}
		#endregion

	}
}