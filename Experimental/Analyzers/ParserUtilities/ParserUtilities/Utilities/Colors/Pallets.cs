using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserUtilities.Utilities.Colors {

	public class Pallets {
		public static IPallet Stratified = new StratifiedPallet();
		public static IPallet Scale = new ScalePallet();
		public static IPallet Orange = new OrangePallet(false);
		public static IPallet OrangeDesc = new OrangePallet(true);
	}

	public abstract class InterpolatePallet : IPallet {
		protected abstract string[] GetColors();

		public string GetColor(double percent) {
			var Colors = GetColors();
			percent = Math.Max(0, Math.Min(1, percent));
			var idx = (int)Math.Round((Colors.Count() - 1) * percent);
			return Colors[idx];
		}

		protected int colorIndex = 0;
		public string NextColor() {
			var colors = GetColors();
			return colors[colorIndex++ % colors.Length];
		}
	}

	public class ScalePallet : InterpolatePallet {
		protected override string[] GetColors() {
			return new string[] { "#5C2D2B", "#5E2F2E", "#603031", "#623133", "#643236", "#663339", "#68353C", "#6A363F", "#6B3742", "#6D3945", "#6E3B48", "#6F3C4B", "#713E4E", "#724051", "#734155", "#734358", "#74455B", "#75475E", "#754962", "#754B65", "#764D68", "#76506C", "#76526F", "#755472", "#755775", "#745978", "#745B7B", "#735E7E", "#726081", "#716384", "#6F6587", "#6E688A", "#6C6A8C", "#6A6D8F", "#696F91", "#677294", "#657596", "#627798", "#607A9A", "#5D7C9C", "#5B7F9E", "#58819F", "#5684A1", "#5387A2", "#5089A3", "#4D8CA4", "#4A8EA5", "#4791A6", "#4493A6", "#4296A7", "#3F98A7", "#3C9AA7", "#3A9DA7", "#389FA7", "#36A2A6", "#35A4A6", "#34A6A5", "#34A9A5", "#34ABA4", "#35ADA3", "#36AFA1", "#38B1A0", "#3AB39F", "#3DB69D", "#40B89B", "#44BA9A", "#47BC98", "#4CBE96", "#50C094", "#55C192", "#59C38F", "#5EC58D", "#63C78B", "#68C989", "#6ECA86", "#73CC84", "#79CD81", "#7ECF7F", "#84D07D", "#8AD27A", "#90D378", "#96D576", "#9CD673", "#A2D771", "#A8D86F", "#AED96D", "#B4DA6B", "#BBDB69", "#C1DC68", "#C8DD66", "#CEDE65", "#D5DF63", "#DBDF62", "#E2E062" };
		}
	}

	public class OrangePallet : InterpolatePallet {
		public string[] colors{ get; private set; }
		public OrangePallet(bool desc) {
			var c = new[] { "#F95003", "#F65004", "#F35005", "#F14F07", "#EE4F08", "#EC4F09", "#E94F0A", "#E74E0B", "#E44E0C", "#E24E0D", "#DF4D0E", "#DD4D0F", "#DA4D0F", "#D84C10", "#D54C11", "#D34B11", "#D04B12", "#CE4B12", "#CC4A13", "#C94A13", "#C74914", "#C44914", "#C24915", "#C04815", "#BD4815", "#BB4716", "#B94716", "#B64616", "#B44616", "#B24517", "#AF4517", "#AD4417", "#AB4417", "#A94317", "#A64317", "#A44218", "#A24118", "#A04118", "#9E4018", "#9B4018", "#993F18", "#973F18", "#953E18", "#933D18", "#913D18", "#8E3C18", "#8C3C18", "#8A3B18", "#883A18", "#863A18", "#843918", "#823818", "#803818", "#7E3718", "#7C3617", "#7A3617", "#783517", "#763517", "#743417", "#723317", "#703217", "#6E3217", "#6C3116", "#6A3016", "#683016", "#662F16", "#642E16", "#622E15", "#602D15", "#5F2C15", "#5D2C15", "#5B2B14", "#592A14", "#572914", "#552914", "#542813", "#522713", "#502713", "#4E2612", "#4D2512", "#4B2412", "#492412", "#472311", "#462211", "#442111", "#422110", "#412010", "#3F1F10" };
			if (desc)
				colors = c.Reverse().ToArray();
			else
				colors = c;
		}


		protected override string[] GetColors() {
			return colors;
		}
	}

	public class StratifiedPallet : InterpolatePallet {
		protected override string[] GetColors() {
			return new string[] { "#f23d3d", "#e5b073", "#3df2ce", "#c200f2", "#e57373", "#f2b63d", "#3de6f2", "#e639c3", "#ff2200", "#d9d26c", "#0099e6", "#d9368d", "#d96236", "#cad900", "#73bfe6", "#d90057", "#ffa280", "#aaff00", "#397ee6", "#f27999", "#ff6600", "#a6d96c", "#4073ff", "#d9986c", "#50e639", "#3d00e6", "#e57a00", "#36d98d", "#b56cd9" };			
		}
	}
}
