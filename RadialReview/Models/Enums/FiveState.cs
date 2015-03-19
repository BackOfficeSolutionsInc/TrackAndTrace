﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Models.Enums
{
	public enum FiveState
	{
		Indeterminate = -1,
	
		/*[Obsolete]
		False = 0,
		[Obsolete]
		True = 1,	*/


		Always =1,
		Mostly =2,
		Rarely =3,
		Never  =0,
		[Obsolete]
		True = Always,
		[Obsolete]
		False = Never,
	}

	public static class FiveStateExtensions
	{
		public static decimal Score(this FiveState self)
		{
			switch(self){
				case FiveState.Always:return 1;
				case FiveState.Mostly:return 2m/3m;
				case FiveState.Rarely:return 1m/3m;
				case FiveState.Never:return 0;
				case FiveState.Indeterminate:return 0;
				default: throw new ArgumentOutOfRangeException("FiveState: "+self);
			}
		}

		public static Ratio Ratio(this FiveState self)
		{
			return new Ratio(self.Score(), self==FiveState.Indeterminate?0:1);
		}
	}
}
