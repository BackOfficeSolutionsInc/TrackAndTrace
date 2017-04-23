using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RadialReview.Models.L10 {
	public partial class L10Recurrence {



		public enum L10PageType {
			// Invalid =0,
			[Display(Name = "Segue"),EnumMember(Value = "Segue")]
			Segue = 1,
			[Display(Name = "Scorecard"), EnumMember(Value = "Scorecard")]
			Scorecard = 2,
			[Display(Name = "Rock Review"), EnumMember(Value = "Rock Review")]
			Rocks = 3,
			[Display(Name = "People Headlines"), EnumMember(Value = "People Headlines")]
			Headlines = 4,
			[Display(Name = "To-do List"), EnumMember(Value = "To-do List")]
			Todo = 5,
			[Display(Name = "IDS"), EnumMember(Value = "IDS")]
			IDS = 6,
			[Display(Name = "Conclude"), EnumMember(Value = "Conclude")]
			Conclude = 7,
			[Display(Name = "Title Page"), EnumMember(Value = "Title Page")]			
			Empty = 0,
			[Display(Name = "Notes Box"), EnumMember(Value = "Notes Box")]
			NotesBox = 8,
		}


		public class L10Recurrence_Page : ILongIdentifiable, IDeletable {
			public virtual long Id { get; set; }
			[JsonIgnore]
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			[JsonConverter(typeof(StringEnumConverter))]
			public virtual L10PageType PageType { get; set; }
			public virtual string PageTypeStr { get { return PageType.ToString(); } }
			public virtual bool AutoGen { get; set; }
			[JsonIgnore]
			public virtual string PadId { get; set; }
			[Required]
			public virtual string Title { get; set; }
			public virtual string Subheading { get; set; }
			[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
			public virtual decimal Minutes { get; set; }
			public virtual long L10RecurrenceId { get; set; }
			[JsonIgnore]
			public virtual L10Recurrence L10Recurrence { get; set; }
			public virtual int _Ordering { get; set; }
			public L10Recurrence_Page() {
				CreateTime = DateTime.UtcNow;
				PadId = Guid.NewGuid().ToString();
				Title = "";
				Subheading = "";
				Minutes = 5;
			}
			public class Map : ClassMap<L10Recurrence_Page> {
				public Map() {
					Id(x => x.Id);
					Map(x => x.PageType);
					Map(x => x.PadId);
					Map(x => x.Title);
					Map(x => x.AutoGen);
					Map(x => x.Minutes);
					Map(x => x.Subheading);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x._Ordering);
					References(x => x.L10Recurrence, "L10RecurrenceId").ReadOnly().LazyLoad();
					Map(x => x.L10RecurrenceId, "L10RecurrenceId");
				}
			}
			
			/*Hack: Treats all dividers as the same. WasModfided indicates that the ordering has already been set.*/
			public virtual bool _WasModified { get; set; }
			public virtual bool _Used { get; set; }
		}
	}
}