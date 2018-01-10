using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Models;

namespace RadialReview.Utilities.DataTypes
{
	public class Ratio : ICompletable
	{
		public decimal Numerator { get; set; }
		public decimal Denominator { get; set; }

        public Ratio Add(decimal numerator, decimal denominator) {
            Numerator += numerator;
            Denominator += denominator;
            return this;
        }
        public Ratio Add(Ratio ratio) {
            return Merge(ratio);
        }

        public Ratio Merge(Ratio ratio)
		{
			Add(ratio.Numerator,ratio.Denominator);
            return this;
        }


		public Ratio()
		{
			Numerator = 0;
			Denominator = 0;
		}
		public Ratio(decimal numerator, decimal denominator)
		{
			Numerator = numerator;
			Denominator = denominator;
		}
		public Ratio(decimal numerator, decimal denominator,decimal weight)
		{
			Numerator = numerator * weight;
			Denominator = denominator * weight;
		}

        public override bool Equals(object obj)
        {
            if (obj is Ratio)            {
                var o = (Ratio)obj;
                return o.Numerator == Numerator && o.Denominator == Denominator;
            }
            return false;
        }

        public override int GetHashCode(){
            int hash = 17;
            hash = hash * 31 + Numerator.GetHashCode();
            hash = hash * 31 + Denominator.GetHashCode();
            return hash;
        }

		public bool IsValid()
		{
			return Denominator != 0;
		}

		public decimal GetValue(decimal? invalid=null){
			if (IsValid() || invalid==null)
				return Numerator/Denominator;
			return invalid.Value;
		}

		public override string ToString()
		{
			return String.Format("{0:0.00}/{1:0.00}", Numerator, Denominator);
		}
		public string ToPercentage(string onInvalid)
		{
			if (IsValid())
				return String.Format("{0}%", Math.Round(GetValue()*100));
			return onInvalid;
		}

		public ICompletionModel GetCompletion(bool split = false) {
			return new CompletionModel(Numerator,Denominator); 
		}

		public class RatioComponent : ComponentMap<Ratio> 
		{
			public RatioComponent()
			{
				Map(x => x.Numerator).Column("Num");
				Map(x => x.Denominator).Column("Den");
			}
		} 
	}
}