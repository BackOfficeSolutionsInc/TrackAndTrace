using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities.DataTypes;
using PdfSharp.Drawing;
using MigraDoc.DocumentObjectModel;

namespace RadialReview.Models.Enums {
	public enum FiveState {
		Indeterminate = -1,

		/*[Obsolete]
		False = 0,
		[Obsolete]
		True = 1,	*/


		Always = 1,
		Mostly = 2,
		Rarely = 3,
		Never = 0,
		[Obsolete]
		True = Always,
		[Obsolete]
		False = Never,
	}

	public static class FiveStateExtensions {
		public static decimal Score(this FiveState self) {
			switch (self) {
				case FiveState.Always:
					return 1;
				case FiveState.Mostly:
					return 2m / 3m;
				case FiveState.Rarely:
					return 1m / 3m;
				case FiveState.Never:
					return 0;
				case FiveState.Indeterminate:
					return 0;
				default:
					throw new ArgumentOutOfRangeException("FiveState: " + self);
			}
		}

		public static Ratio Ratio(this FiveState self) {
			return new Ratio(self.Score(), self == FiveState.Indeterminate ? 0 : 1);
		}

		public static XColor GetXColor(this FiveState self) {
			switch (self) {
				case FiveState.Indeterminate:
					return XColor.FromArgb(255, 224, 224, 224);
				case FiveState.Always:
					return XColor.FromArgb(255, 68, 157, 68);
				case FiveState.Mostly:
					return XColor.FromArgb(255, 113, 190, 113);
				case FiveState.Rarely:
					return XColor.FromArgb(255, 252, 130, 127);
				case FiveState.Never:
					return XColor.FromArgb(255, 217, 83, 79);
				default:
					throw new ArgumentOutOfRangeException("GetColor out of range: " + self);
			}
		}

		public static Color GetColor(this FiveState self) {
			var x= self.GetXColor();
			return Color.FromArgb((byte)(x.A*255), x.R, x.G, x.B);

			//switch (self) {
			//	case FiveState.Indeterminate:
			//		return Color.FromArgb(255, 224, 224, 224);
			//	case FiveState.Always:
			//		return Color.FromArgb(255, 68, 157, 68);
			//	case FiveState.Mostly:
			//		return Color.FromArgb(255, 113, 190, 113);
			//	case FiveState.Rarely:
			//		return Color.FromArgb(255, 252, 130, 127);
			//	case FiveState.Never:
			//		return Color.FromArgb(255, 217, 83, 79);
			//	default:
			//		throw new ArgumentOutOfRangeException("GetColor out of range: " + self);
			//}
		}
	}
}
