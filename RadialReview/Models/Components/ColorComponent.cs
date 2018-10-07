using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PdfSharp.Drawing;

namespace RadialReview.Models.Components {
	public class ColorComponent {
		/// <summary>
		/// Black
		/// </summary>
		public ColorComponent() {
			Alpha = 255;
		}
		public ColorComponent(byte red, byte green, byte blue, byte alpha=255) {
			Red = red;
			Green = green;
			Blue = blue;
			Alpha = alpha;
		}

		public virtual byte Red { get; set; }
		public virtual byte Green { get; set; }
		public virtual byte Blue { get; set; }
		public virtual byte Alpha { get; set; }

		public class Map : ComponentMap<ColorComponent> {
			public Map() {
				Map(x => x.Red);
				Map(x => x.Green);
				Map(x => x.Blue);
				Map(x => x.Alpha);
			}
		}

		public XColor ToXColor() {
			return new XColor() {
				R = Red,
				G = Green,
				B = Blue,
				A = Alpha / 255.0
			};			
		}
		public MigraDoc.DocumentObjectModel.Color ToMigradocColor() {
			return new MigraDoc.DocumentObjectModel.Color(Alpha, Red, Green, Blue);
	}

		public string ToHex(bool includeAlpha=true) {
			return "#" + Red.ToString("X2") + Green.ToString("X2") + Blue.ToString("X2") + (includeAlpha?Alpha.ToString("X2"):"");
		}

		public static ColorComponent FromHex(string hex,ColorComponent deflt=null) {
			deflt = deflt ?? new ColorComponent();
			try {
				if (hex == null)
					return deflt;
				System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(hex);
				return new ColorComponent(col.R, col.G, col.B, col.A);
			} catch (Exception e) {
				return deflt;
			}
		}

		public ColorComponent WithAlpha(byte alpha) {
			return new ColorComponent(Red, Green, Blue, alpha);
		}

		public static ColorComponent TractionOrange() {
			return new ColorComponent(239, 118, 34);
		}
		public static ColorComponent TractionBlack() {
			return new ColorComponent(62, 57, 53);
		}
	}
}