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
    public interface IVtoSectionHeader {
        DateTime? FutureDate { get; set; }
        Decimal? Revenue { get; set; }
        Decimal? Profit { get; set; }
        String Measurables { get; set; }
    }

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
		public IEnumerable<AngularCompanyValue> Values { get; set; }
		public IEnumerable<AngularVtoString> Issues { get; set; }

        public String TenYearTarget { get; set; }
        public String TenYearTargetTitle { get; set; }
        public String CoreValueTitle { get; set; }
        public String IssuesListTitle { get; set; }
		public static AngularVTO Create(VtoModel vto)
		{
			return new AngularVTO(){
				Id = vto.Id,
				CreateTime = vto.CreateTime,
				CopiedFrom = vto.CopiedFrom,
				TenYearTarget = vto.TenYearTarget,
				Name = vto.Name, //AngularVtoString.Create(vto.Name),
				Values = AngularCompanyValue.Create(vto._Values),
				CoreFocus = AngularCoreFocus.Create(vto.CoreFocus),
				Strategy = AngularStrategy.Create(vto.MarketingStrategy),
				OneYearPlan = AngularOneYearPlan.Create(vto.OneYearPlan),
				QuarterlyRocks = AngularQuarterlyRocks.Create(vto.QuarterlyRocks),
				ThreeYearPicture = AngularThreeYearPicture.Create(vto.ThreeYearPicture),
                Issues = AngularVtoString.Create(vto._Issues),
                TenYearTargetTitle = vto.TenYearTargetTitle ?? "10-YEAR TARGET™",
                CoreValueTitle = vto.CoreValueTitle ?? "CORE VALUES",
                IssuesListTitle = vto.IssuesListTitle ?? "ISSUES LIST"
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
		
		public bool Deleted { get; set; }

		public static AngularVtoString Create(VtoModel.VtoItem_String strs)
		{
			return new AngularVtoString()
			{
				Data = strs.Data,
				Id = strs.Id,
				Deleted = strs.DeleteTime!=null
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

		public String Purpose { get; set; }
		public String Niche { get; set; }
        public string PurposeTitle { get; set; }
        public string CoreFocusTitle { get; set; }

		public static AngularCoreFocus Create(VtoModel.CoreFocusModel coreFocus)
		{
			return new AngularCoreFocus(){
				Id=coreFocus.Id,
				Niche = coreFocus.Niche,
				Purpose = (coreFocus.Purpose),
                PurposeTitle = coreFocus.PurposeTitle??"Purpose/Cause/Passion",
                CoreFocusTitle = coreFocus.CoreFocusTitle ?? "CORE FOCUS™"

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
	//	public String TenYearTarget { get; set; }
		public String TargetMarket { get; set; }
		public String ProvenProcess { get; set; }
		public String Guarantee { get; set; }
        public String MarketingStrategyTitle { get; set; }
		public IEnumerable<AngularVtoString> Uniques { get; set; }

		internal static AngularStrategy Create(VtoModel.MarketingStrategyModel marketingStrategyModel)
		{
			return new AngularStrategy()
			{
				Id = marketingStrategyModel.Id,
				Guarantee = (marketingStrategyModel.Guarantee),
				ProvenProcess = (marketingStrategyModel.ProvenProcess),
				TargetMarket = (marketingStrategyModel.TargetMarket),
				//TenYearTarget = (marketingStrategyModel.TenYearTarget),
				Uniques = AngularVtoString.Create(marketingStrategyModel._Uniques),
                MarketingStrategyTitle = marketingStrategyModel.MarketingStrategyTitle ?? "MARKETING STRATEGY",
			};
		}
	}
	public class AngularThreeYearPicture : Base.BaseAngular, IVtoSectionHeader
	{
		[Obsolete("User Static constructor", false)]
		public AngularThreeYearPicture(long id)
			: base(id)
		{
		}
		public AngularThreeYearPicture()
		{
		}
		public DateTime? FutureDate { get; set; }
		public Decimal? Revenue { get; set; }
		public Decimal? Profit { get; set; }
		public String Measurables { get; set; }
        public String ThreeYearPictureTitle { get; set; }
		public IEnumerable<AngularVtoString> LooksLike { get; set; }

		public static AngularThreeYearPicture Create(VtoModel.ThreeYearPictureModel threeYearPicture)
		{
			return new AngularThreeYearPicture(){
				FutureDate = (threeYearPicture.FutureDate),
				LooksLike = AngularVtoString.Create(threeYearPicture._LooksLike),
				Measurables = (threeYearPicture.Measurables),
				Profit = (threeYearPicture.Profit),
				Revenue = (threeYearPicture.Revenue),
				Id = threeYearPicture.Id,
                ThreeYearPictureTitle = threeYearPicture.ThreeYearPictureTitle ?? "3-YEAR PICTURE™"
			};
		}
	}

    public class AngularOneYearPlan : Base.BaseAngular, IVtoSectionHeader
	{
		[Obsolete("User Static constructor", false)]
		public AngularOneYearPlan(long id): base(id){
		}
		public AngularOneYearPlan(){
		}
		public DateTime? FutureDate { get; set; }
		public Decimal? Revenue { get; set; }
        public Decimal? Profit { get; set; }
        public String Measurables { get; set; }
        public String OneYearPlanTitle { get; set; }
		public IEnumerable<AngularVtoString> GoalsForYear { get; set; }

		public static AngularOneYearPlan Create(VtoModel.OneYearPlanModel oneYearPlan)
		{
			return new AngularOneYearPlan(){
				Id = oneYearPlan.Id,
				FutureDate = (oneYearPlan.FutureDate),
				GoalsForYear = AngularVtoString.Create(oneYearPlan._GoalsForYear),
				Measurables = (oneYearPlan.Measurables),
				Profit = (oneYearPlan.Profit),
				Revenue = (oneYearPlan.Revenue),
                OneYearPlanTitle = oneYearPlan.OneYearPlanTitle ?? "1-YEAR PLAN"
			};
		}
	}

    public class AngularQuarterlyRocks : Base.BaseAngular, IVtoSectionHeader
	{
		[Obsolete("User Static constructor", false)]
		public AngularQuarterlyRocks(long id) : base(id){
		}
		public AngularQuarterlyRocks(){
		}
		public DateTime? FutureDate { get; set; }
		public Decimal? Revenue { get; set; }
        public Decimal? Profit { get; set; }
        public String Measurables { get; set; }
        public String RocksTitle { get; set; }
		public IEnumerable<AngularVtoRock> Rocks { get; set; }

		public static AngularQuarterlyRocks Create(VtoModel.QuarterlyRocksModel quarterlyRocksModel)
		{
			return new AngularQuarterlyRocks(){
				Id = quarterlyRocksModel.Id,
				FutureDate = (quarterlyRocksModel.FutureDate),
				Measurables = (quarterlyRocksModel.Measurables),
				Profit = (quarterlyRocksModel.Profit),
				Revenue = (quarterlyRocksModel.Revenue),
				Rocks = AngularVtoRock.Create(quarterlyRocksModel._Rocks),
                RocksTitle = quarterlyRocksModel.RocksTitle ?? "ROCKS"
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
		public bool Deleted { get; set; }

		public static AngularVtoRock Create(VtoModel.Vto_Rocks rock)
		{
			return new AngularVtoRock(){
				Rock = new AngularRock(rock.Rock),
				Id = rock.Id,
				Deleted = rock.DeleteTime!=null
			};
		}

		public static List<AngularVtoRock> Create(IEnumerable<VtoModel.Vto_Rocks> rocks){
			return rocks.Select(Create).ToList();
		}
	}
}