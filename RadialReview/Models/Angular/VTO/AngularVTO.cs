using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security.Provider;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.L10;

namespace RadialReview.Models.Angular.VTO {
	public interface IVtoSectionHeader {
		DateTime? FutureDate { get; set; }
		String Revenue { get; set; }
		String Profit { get; set; }
		String Measurables { get; set; }
	}

	public class AngularVTO : Base.BaseAngular {
		public AngularVTO(long id) : base(id) {
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public AngularVTO() {
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public DateTime? CreateTime { get; set; }
		public long? CopiedFrom { get; set; }
		public String Name { get; set; }
		public bool IncludeVision { get; set; }

		public AngularCoreFocus CoreFocus { get; set; }
		public AngularStrategy Strategy { get; set; }
		public List<AngularStrategy> Strategies { get; set; }

		public AngularQuarterlyRocks QuarterlyRocks { get; set; }
		public AngularThreeYearPicture ThreeYearPicture { get; set; }
		public AngularOneYearPlan OneYearPlan { get; set; }
		public IEnumerable<AngularCompanyValue> Values { get; set; }
		public IEnumerable<AngularVtoString> Issues { get; set; }

		public String TenYearTarget { get; set; }
		public String TenYearTargetTitle { get; set; }
		public String CoreValueTitle { get; set; }
		public String IssuesListTitle { get; set; }
		public static AngularVTO Create(VtoModel vto) {
            return new AngularVTO() {
                Id = vto.Id,
                L10Recurrence = vto.L10Recurrence,
                CreateTime = vto.CreateTime,
                CopiedFrom = vto.CopiedFrom,
                TenYearTarget = vto.TenYearTarget,
                Name = vto.Name, //AngularVtoString.Create(vto.Name),
                Values = AngularCompanyValue.Create(vto._Values),
                CoreFocus = AngularCoreFocus.Create(vto.CoreFocus),
                Strategy = AngularStrategy.Create(vto.MarketingStrategy),
				//vto._MarketingStrategyModel.Select(x => AngularStrategy.Create(x)),
				Strategies= vto._MarketingStrategyModel.Select(x => AngularStrategy.Create(x)).ToList(),

				OneYearPlan = AngularOneYearPlan.Create(vto.OneYearPlan),
				QuarterlyRocks = AngularQuarterlyRocks.Create(vto.QuarterlyRocks),
				ThreeYearPicture = AngularThreeYearPicture.Create(vto.ThreeYearPicture),
				Issues = AngularVtoString.Create(vto._Issues),
				TenYearTargetTitle = vto.TenYearTargetTitle ?? "10-YEAR TARGET™",
				CoreValueTitle = vto.CoreValueTitle ?? "CORE VALUES",
				IssuesListTitle = vto.IssuesListTitle ?? "ISSUES LIST",
				IncludeVision = true
			};
		}

		public long? L10Recurrence { get; set; }

        public void ReplaceVision(VtoModel vto) {
            // Id = vto.Id;
            //L10Recurrence = vto.L10Recurrence,
            //CreateTime = vto.CreateTime,
            //CopiedFrom = vto.CopiedFrom,
            TenYearTarget = vto.TenYearTarget;
            //Name = vto.Name, //AngularVtoString.Create(vto.Name),
            Values = AngularCompanyValue.Create(vto._Values);
            CoreFocus = AngularCoreFocus.Create(vto.CoreFocus);
            Strategy = AngularStrategy.Create(vto.MarketingStrategy); //vto._MarketingStrategyModel.Select(x => AngularStrategy.Create(x));
			Strategies = vto._MarketingStrategyModel.Select(x => AngularStrategy.Create(x)).ToList();
            //OneYearPlan = AngularOneYearPlan.Create(vto.OneYearPlan);
            //QuarterlyRocks = AngularQuarterlyRocks.Create(vto.QuarterlyRocks),
            ThreeYearPicture = AngularThreeYearPicture.Create(vto.ThreeYearPicture);
            //Issues = AngularVtoString.Create(vto._Issues),
            TenYearTargetTitle = vto.TenYearTargetTitle ?? "10-YEAR TARGET™";
            CoreValueTitle = vto.CoreValueTitle ?? "CORE VALUES";
            //IssuesListTitle = vto.IssuesListTitle ?? "ISSUES LIST",
            IncludeVision = true;
        }
    }
	#region DataTypes
	//public class AngularVtoIssue : AngularVtoString {
	//	public string Owner { get; set; }
	//	public string OwnerInitials { get; set; }
		
	//	public static AngularVtoIssue Create(VtoIssue strs) {
	//		return new AngularVtoIssue() {
	//			Data = strs.Data,
	//			Id = strs.Id,
	//			Deleted = strs.DeleteTime != null,
	//			Owner = strs.Owner,
	//			OwnerInitials = strs.OwnerInitials
	//		};
	//	}
	//	public static List<AngularVtoIssue> Create(IEnumerable<VtoIssue> strs) {
	//		return strs.Select(AngularVtoIssue.Create).ToList();
	//	}
	//}

	public class AngularVtoString : Base.BaseAngular {
		public AngularVtoString(long id) : base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularVtoString() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public String Data { get; set; }

		public bool Deleted { get; set; }

		public static AngularVtoString Create(VtoItem_String strs) {
			return new AngularVtoString() {
				Data = strs.Data,
				Id = strs.Id,
				Deleted = strs.DeleteTime != null,
				_ExtraProperties = strs._Extras
			};
		}
		public static List<AngularVtoString> Create(IEnumerable<VtoItem_String> strs) {
			return strs.Select(AngularVtoString.Create).ToList();
		}
	}
	public class AngularVtoDateTime : Base.BaseAngular {
		public AngularVtoDateTime(long id)
			: base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularVtoDateTime() {
		}

		public DateTime? Data { get; set; }

		public static AngularVtoDateTime Create(VtoItem_DateTime futureDate) {
			return new AngularVtoDateTime() {
				Id = futureDate.Id,
				Data = futureDate.Data,
			};
		}
	}
	public class AngularVtoDecimal : Base.BaseAngular {
		public AngularVtoDecimal(long id) : base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularVtoDecimal() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public decimal? Data { get; set; }

		public static AngularVtoDecimal Create(VtoItem_Decimal value) {
			return new AngularVtoDecimal() {
				Id = value.Id,
				Data = value.Data
			};
		}
	}
	#endregion
	public class AngularCoreFocus : Base.BaseAngular {

		public AngularCoreFocus(long id) : base(id) {
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public AngularCoreFocus() {
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public String Purpose { get; set; }
		public String Niche { get; set; }
		public string PurposeTitle { get; set; }
		public string CoreFocusTitle { get; set; }

		public static AngularCoreFocus Create(CoreFocusModel coreFocus) {
			return new AngularCoreFocus() {
				Id = coreFocus.Id,
				Niche = coreFocus.Niche,
				Purpose = (coreFocus.Purpose),
				PurposeTitle = coreFocus.PurposeTitle ?? "Purpose/Cause/Passion",
				CoreFocusTitle = coreFocus.CoreFocusTitle ?? "CORE FOCUS™"

			};
		}
	}
	public class AngularStrategy : Base.BaseAngular {

		public AngularStrategy(long id) : base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularStrategy() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		//	public String TenYearTarget { get; set; }
		public String TargetMarket { get; set; }
		public String ProvenProcess { get; set; }
		public String Guarantee { get; set; }
		public String MarketingStrategyTitle { get; set; }
		public IEnumerable<AngularVtoString> Uniques { get; set; }

		internal static AngularStrategy Create(MarketingStrategyModel marketingStrategyModel) {
			return new AngularStrategy() {
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
	public class AngularThreeYearPicture : Base.BaseAngular, IVtoSectionHeader {
		public AngularThreeYearPicture(long id)
			: base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularThreeYearPicture() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public DateTime? FutureDate { get; set; }
		public String Revenue { get; set; }
		public String Profit { get; set; }
		public String Measurables { get; set; }
		public String ThreeYearPictureTitle { get; set; }
		public IEnumerable<AngularVtoString> LooksLike { get; set; }

		public static AngularThreeYearPicture Create(ThreeYearPictureModel threeYearPicture) {
			return new AngularThreeYearPicture() {
				FutureDate = (threeYearPicture.FutureDate),
				LooksLike = AngularVtoString.Create(threeYearPicture._LooksLike),
				Measurables = (threeYearPicture.Measurables),
				Profit = (threeYearPicture.ProfitStr),
				Revenue = (threeYearPicture.RevenueStr),
				Id = threeYearPicture.Id,
				ThreeYearPictureTitle = threeYearPicture.ThreeYearPictureTitle ?? "3-YEAR PICTURE™"
			};
		}
	}

	public class AngularOneYearPlan : Base.BaseAngular, IVtoSectionHeader {
		public AngularOneYearPlan(long id) : base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularOneYearPlan() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public DateTime? FutureDate { get; set; }
		public string Revenue { get; set; }
		public string Profit { get; set; }
		public String Measurables { get; set; }
		public String OneYearPlanTitle { get; set; }
		public IEnumerable<AngularVtoString> GoalsForYear { get; set; }

		public static AngularOneYearPlan Create(OneYearPlanModel oneYearPlan) {
			return new AngularOneYearPlan() {
				Id = oneYearPlan.Id,
				FutureDate = (oneYearPlan.FutureDate),
				GoalsForYear = AngularVtoString.Create(oneYearPlan._GoalsForYear),
				Measurables = (oneYearPlan.Measurables),
				Profit = (oneYearPlan.ProfitStr),
				Revenue = (oneYearPlan.RevenueStr),
				OneYearPlanTitle = oneYearPlan.OneYearPlanTitle ?? "1-YEAR PLAN"
			};
		}
	}

	public class AngularQuarterlyRocks : Base.BaseAngular, IVtoSectionHeader {
		//[Obsolete("Use vto id")]
		public AngularQuarterlyRocks(long id) : base(id) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularQuarterlyRocks() {
		}
#pragma warning restore CS0618 // Type or member is obsolete
		public DateTime? FutureDate { get; set; }
		public String Revenue { get; set; }
		public String Profit { get; set; }
		public String Measurables { get; set; }
		public String RocksTitle { get; set; }
		public IEnumerable<AngularVtoRock> Rocks { get; set; }

		public static AngularQuarterlyRocks Create(QuarterlyRocksModel quarterlyRocksModel) {
			return new AngularQuarterlyRocks() {
				Id = quarterlyRocksModel.Id,
				FutureDate = (quarterlyRocksModel.FutureDate),
				Measurables = (quarterlyRocksModel.Measurables),
				Profit = (quarterlyRocksModel.ProfitStr),
				Revenue = (quarterlyRocksModel.RevenueStr),
				Rocks = /*AngularVtoRock.Create(*/quarterlyRocksModel._Rocks/*)*/,
				RocksTitle = quarterlyRocksModel.RocksTitle ?? "ROCKS"
			};
		}
	}

	public class AngularVtoRock : Base.BaseAngular {
		public AngularVtoRock(long recurRockId) : base(recurRockId) {
		}
#pragma warning disable CS0618 // Type or member is obsolete
		public AngularVtoRock() {
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public AngularRock Rock { get; set; }

		public bool Deleted { get; set; }

		public static AngularVtoRock Create(L10Recurrence.L10Recurrence_Rocks recurRock) {
			return new AngularVtoRock() {
				Rock = new AngularRock(recurRock),
				Deleted = recurRock.DeleteTime != null,
				Id = recurRock.Id,
			};
		}
		public static List<AngularVtoRock> Create(IEnumerable<L10Recurrence.L10Recurrence_Rocks> recurRocks) {
			return recurRocks.Select(Create).ToList();
		}

		//public static AngularVtoRock Create(Vto_Rocks rock) {
		//	return new AngularVtoRock() {
		//		Rock = new AngularRock(rock.Rock),
		//		Id = rock.Id,
		//		Deleted = rock.DeleteTime != null
		//	};
		//}

		//public static List<AngularVtoRock> Create(IEnumerable<Vto_Rocks> rocks) {
		//	return rocks.Select(Create).ToList();
		//}
	}
}