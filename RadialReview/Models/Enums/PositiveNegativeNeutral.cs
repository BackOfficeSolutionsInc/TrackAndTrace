using MigraDoc.DocumentObjectModel;
using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums {
	public enum PositiveNegativeNeutral {
		Indeterminate = -2,
		Negative = -1,
		Neutral = 0,
		Positive = 1,
	}

	public static class PositiveNegativeNeutralExtensions {
		public static XColor GetXColor(this PositiveNegativeNeutral self) {
			switch (self) {
				case PositiveNegativeNeutral.Indeterminate:
					return XColor.FromArgb(255, 224, 224, 224);
				case PositiveNegativeNeutral.Negative:
					return XColor.FromArgb(255, 217, 83, 79);
				case PositiveNegativeNeutral.Neutral:
					return XColor.FromArgb(255, 236, 151, 31);
				case PositiveNegativeNeutral.Positive:
					return XColor.FromArgb(255, 68, 157, 68);
				default:
					throw new ArgumentOutOfRangeException("GetColor out of range: " + self);
			}
		}



		public static Color GetColor(this PositiveNegativeNeutral self) {
			var x = self.GetXColor();
			return Color.FromArgb((byte)(x.A * 255), x.R, x.G, x.B);
		}

		public static string ToShortKey(this PositiveNegativeNeutral self) {
			switch (self) {
				case PositiveNegativeNeutral.Indeterminate:
					return "";
				case PositiveNegativeNeutral.Negative:
					return "-";
				case PositiveNegativeNeutral.Neutral:
					return "+/-";
				case PositiveNegativeNeutral.Positive:
					return "+";
				default:
					throw new ArgumentOutOfRangeException("GetColor out of range: " + self);
			}
		}
	}
}