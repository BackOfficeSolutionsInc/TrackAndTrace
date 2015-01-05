using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
	public class Ratio
	{
		public decimal Numerator { get; set; }
		public decimal Denominator { get; set; }

		public void Add(decimal numerator, decimal denominator)
		{
			Numerator   += numerator;
			Denominator += denominator;
		}

		public void Merge(Ratio ratio)
		{
			Add(ratio.Numerator,ratio.Denominator);			
		}


		public Ratio()
		{
			Numerator = 0;
			Denominator = 0;
		}
		public Ratio(decimal numerator, decimal denominator)
		{
			Numerator = numerator;
			Denominator=denominator;
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

	}
}