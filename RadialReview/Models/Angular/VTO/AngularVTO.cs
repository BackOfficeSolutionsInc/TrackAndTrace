using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security.Provider;
using Moq;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.VTO;

namespace RadialReview.Models.Angular.VTO
{
	public class AngularVTO : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularVTO(long id) : base(id)
		{
		}

		public AngularVTO()
		{
		}

		public DateTime? CreateTime { get; set; }
		public long? CopiedFrom { get; set; }
		public String Name { get; set; }

		public AngularCoreFocus CoreFocus { get; set; }
		public AngularStrategy Strategy { get; set; }
		public AngularQuarterlyRocks QuarterlyRocks { get; set; }
		public AngularThreeYearPicture ThreeYearPicture { get; set; }
		public AngularOneYearPlan OneYearPlan { get; set; }
		public List<AngularCompanyValue> Values { get; set; }


		public static AngularVTO Create(VtoModel vto)
		{
			return new AngularVTO(){
				Id = vto.Id,
				CreateTime = vto.CreateTime,
				CopiedFrom = vto.CopiedFrom,
				Name = vto.Name, //AngularVtoString.Create(vto.Name),
				Values = AngularCompanyValue.Create(vto._Values),
				CoreFocus = AngularCoreFocus.Create(vto.CoreFocus),
				Strategy = AngularStrategy.Create(vto.MarketingStrategy),
				OneYearPlan = AngularOneYearPlan.Create(vto.OneYearPlan),
				QuarterlyRocks = AngularQuarterlyRocks.Create(vto.QuarterlyRocks),
				ThreeYearPicture = AngularThreeYearPicture.Create(vto.ThreeYearPicture),
			};
		}
	}
	#region DataTypes
	public class AngularVtoString : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularVtoString(long id) : base(id){
		}
		public AngularVtoString(){
		}
		public String Data { get; set; }
		
		public static AngularVtoString Create(VtoModel.VtoItem_String strs)
		{
			return new AngularVtoString()
			{
				Data = strs.Data,
				Id = strs.Id
			};
		}
		public static List<AngularVtoString> Create(IEnumerable<VtoModel.VtoItem_String> strs)
		{
			return strs.Select(AngularVtoString.Create).ToList();
		}
	}
	public class AngularVtoDateTime : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularVtoDateTime(long id)
			: base(id)
		{
		}
		public AngularVtoDateTime()
		{
		}

		public DateTime? Data { get; set; }

		public static AngularVtoDateTime Create(VtoModel.VtoItem_DateTime futureDate)
		{
			return new AngularVtoDateTime(){
				Id = futureDate.Id,
				Data = futureDate.Data,
			};
		}
	}
	public class AngularVtoDecimal : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularVtoDecimal(long id) : base(id)
		{
		}
		public AngularVtoDecimal()
		{
		}
		public decimal? Data { get; set; }

		public static AngularVtoDecimal Create(VtoModel.VtoItem_Decimal value)
		{
			return new AngularVtoDecimal(){
				Id = value.Id,
				Data = value.Data
			};
		}
	}
	#endregion

	public class AngularCoreFocus : Base.BaseAngular
	{
		
		[Obsolete("User Static constructor",false)]
		public AngularCoreFocus(long id) : base(id){
		}

		public AngularCoreFocus(){
		}

		public AngularVtoString Purpose { get; set; }
		public AngularVtoString Niche { get; set; }

		public static AngularCoreFocus Create(VtoModel.CoreFocusModel coreFocus)
		{
			return new AngularCoreFocus(){
				Id=coreFocus.Id,
				Niche = AngularVtoString.Create(coreFocus.Niche),
				Purpose = AngularVtoString.Create(coreFocus.Purpose),
			};
		}
	}
	public class AngularStrategy : Base.BaseAngular
	{

		[Obsolete("User Static constructor", false)]
		public AngularStrategy(long id) : base(id)
		{
		}
		public AngularStrategy()
		{
		}
		public AngularVtoString TenYearTarget { get; set; }
		public AngularVtoString TargetMarket { get; set; }
		public AngularVtoString ProvenProcess { get; set; }
		public AngularVtoString Guarantee { get; set; }
		public List<AngularVtoString> Uniques { get; set; }

		internal static AngularStrategy Create(VtoModel.MarketingStrategyModel marketingStrategyModel)
		{
			return new AngularStrategy()
			{
				Id = marketingStrategyModel.Id,
				Guarantee = AngularVtoString.Create(marketingStrategyModel.Guarantee),
				ProvenProcess = AngularVtoString.Create(marketingStrategyModel.ProvenProcess),
				TargetMarket = AngularVtoString.Create(marketingStrategyModel.TargetMarket),
				TenYearTarget = AngularVtoString.Create(marketingStrategyModel.TenYearTarget),
				Uniques = AngularVtoString.Create(marketingStrategyModel._Uniques),
			};
		}
	}
	public class AngularThreeYearPicture : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularThreeYearPicture(long id)
			: base(id)
		{
		}
		public AngularThreeYearPicture()
		{
		}
		public AngularVtoDateTime FutureDate { get; set; }
		public AngularVtoDecimal Revenue { get; set; }
		public AngularVtoDecimal Profit { get; set; }
		public AngularVtoString Measurables { get; set; }
		public List<AngularVtoString> LooksLike { get; set; }

		public static AngularThreeYearPicture Create(VtoModel.ThreeYearPictureModel threeYearPicture)
		{
			return new AngularThreeYearPicture(){
				FutureDate = AngularVtoDateTime.Create(threeYearPicture.FutureDate),
				LooksLike = AngularVtoString.Create(threeYearPicture._LooksLike),
				Measurables = AngularVtoString.Create(threeYearPicture.Measurables),
				Profit = AngularVtoDecimal.Create(threeYearPicture.Profit),
				Revenue = AngularVtoDecimal.Create(threeYearPicture.Revenue),
				Id = threeYearPicture.Id
			};
		}
	}

	public class AngularOneYearPlan : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularOneYearPlan(long id): base(id){
		}
		public AngularOneYearPlan(){
		}
		public AngularVtoDateTime FutureDate { get; set; }
		public AngularVtoDecimal Revenue { get; set; }
		public AngularVtoDecimal Profit { get; set; }
		public AngularVtoString Measurables { get; set; }
		public List<AngularVtoString> GoalsForYear { get; set; }

		public static AngularOneYearPlan Create(VtoModel.OneYearPlanModel oneYearPlan)
		{
			return new AngularOneYearPlan(){
				FutureDate = AngularVtoDateTime.Create(oneYearPlan.FutureDate),
				GoalsForYear = AngularVtoString.Create(oneYearPlan._GoalsForYear),
				Measurables = AngularVtoString.Create(oneYearPlan.Measurables),
				Profit = AngularVtoDecimal.Create(oneYearPlan.Profit),
				Revenue = AngularVtoDecimal.Create(oneYearPlan.Revenue),
			};
		}
	}

	public class AngularQuarterlyRocks : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularQuarterlyRocks(long id) : base(id){
		}
		public AngularQuarterlyRocks(){
		}
		public AngularVtoDateTime FutureDate { get; set; }
		public AngularVtoDecimal Revenue { get; set; }
		public AngularVtoDecimal Profit { get; set; }
		public AngularVtoString Measurables { get; set; }
		public List<AngularVtoRock> Rocks { get; set; }

		public static AngularQuarterlyRocks Create(VtoModel.QuarterlyRocksModel quarterlyRocksModel)
		{
			return new AngularQuarterlyRocks(){
				Id = quarterlyRocksModel.Id,
				FutureDate = AngularVtoDateTime.Create(quarterlyRocksModel.FutureDate),
				Measurables = AngularVtoString.Create(quarterlyRocksModel.Measurables),
				Profit = AngularVtoDecimal.Create(quarterlyRocksModel.Profit),
				Revenue = AngularVtoDecimal.Create(quarterlyRocksModel.Revenue),
				Rocks = AngularVtoRock.Create(quarterlyRocksModel._Rocks)
			};
		}
	}

	public class AngularVtoRock : Base.BaseAngular
	{
		[Obsolete("User Static constructor", false)]
		public AngularVtoRock(long id) : base(id){
		}
		public AngularVtoRock(){
		}

		public AngularRock Rock { get; set; }

		public static List<AngularVtoRock> Create(IEnumerable<VtoModel.Vto_Rocks> rocks)
		{
			return rocks.Select(x => new AngularVtoRock(){
				Rock = new AngularRock(x.Rock),
				Id = x.Id,
			}).ToList();
		}
	}
}