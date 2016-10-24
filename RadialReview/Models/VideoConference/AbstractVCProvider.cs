using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.VideoConference {
	public interface IVideoConferenceProvider : ILongIdentifiable {
		VideoConferenceType GetVideoConferenceType();
		string GetUrl();
		string GetName();
		DateTime LastUsed { get; set; }
	}

	public abstract class AbstractVCProvider : ILongIdentifiable, IHistorical, IVideoConferenceProvider{

		public virtual long Id { get; set; }
		public virtual DateTime LastUsed { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		
		public virtual string FriendlyName { get; set; }

		public virtual string Url { get { return GetUrl(); } }

		[JsonConverter(typeof(StringEnumConverter))]
		public virtual VideoConferenceType VideoConferenceType { get { return GetVideoConferenceType(); } }

		public virtual long OwnerId { get; set; }

		public abstract VideoConferenceType GetVideoConferenceType();
		public virtual string GetName() {
			return FriendlyName?? (""+GetVideoConferenceType());
		}
		public abstract string GetUrl();
		public AbstractVCProvider() {
			CreateTime = DateTime.UtcNow;
			LastUsed = DateTime.UtcNow;
		}

		public class AMap : ClassMap<AbstractVCProvider> {
			public AMap() {
				Id(x => x.Id);
				Map(x => x.OwnerId);
				Map(x => x.LastUsed);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.FriendlyName);
			}
		}
	}
}