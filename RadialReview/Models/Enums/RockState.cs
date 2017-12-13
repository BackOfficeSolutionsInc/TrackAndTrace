using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum RockState
	{
        [Display(Name = "Not Set")]
        Indeterminate = -1,
        [Display(Name = "Off Track")]
        AtRisk = 0,
        [Display(Name = "On Track")]
        OnTrack = 1,
        [Display(Name = "Done")]
		Complete = 2,
	}

	public static class RockStateExtensions {

		public static XColor GetColor(this RockState self) {
			switch (self) {
				case RockState.Indeterminate:
					return XColor.FromArgb(255, 224, 224, 224);
				case RockState.AtRisk:
					return XColor.FromArgb(255, 217, 83, 79);
				case RockState.OnTrack:
					return XColor.FromArgb(255, 31, 104, 236);
				case RockState.Complete:
					return XColor.FromArgb(255, 68, 157, 68);
				default:
					throw new ArgumentOutOfRangeException("GetColor out of range: " + self);
			}
		}

		public static bool? IsComplete(this RockState self) {
			if (self == RockState.Indeterminate)
				return null;
			return self == RockState.Complete;
		}

		public static string GetCompletion(this RockState self) {
			switch (self) {
				case RockState.Indeterminate:	return "Unspecified";
				case RockState.AtRisk:			return "Not Done";
				case RockState.OnTrack:			return "Not Done";
				case RockState.Complete:		return "Done";
				default:throw new ArgumentOutOfRangeException("GetCompletion out of range: " + self);
			}
		}

		public static string GetCompletionVal(this RockState self) {
			switch (self) {
			case RockState.Indeterminate:
			return "Unspecified";
			case RockState.AtRisk:
			return "Off Track";
			case RockState.OnTrack:
			return "On Track";
			case RockState.Complete:
			return "Done";
			default:
			throw new ArgumentOutOfRangeException("GetCompletion out of range: " + self);
			}
		}
	}
}