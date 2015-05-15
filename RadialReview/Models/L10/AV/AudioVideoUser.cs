using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.L10.AV
{
	public class AudioVideoUser : ILongIdentifiable,IHistorical
	{
		public virtual long Id { get; set; }
		public virtual UserOrganizationModel User { get; set; }
		public virtual string ConnectionId { get; set; }
		public virtual long RecurrenceId { get; set; }
		public virtual bool Audio { get; set; }
		public virtual bool Video { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public AudioVideoUser()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class AudioVideoUserMap : ClassMap<AudioVideoUser>
		{
			public AudioVideoUserMap()
			{
				Id(x => x.Id);
				Map(x => x.ConnectionId);
				Map(x => x.RecurrenceId);
				Map(x => x.Audio);
				Map(x => x.Video);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				References(x => x.User).Column("UserOrganizationId").LazyLoad();
			}
		}

	}

	public class AudioVisualUserVM
	{
		public String Name { get; set; }
		public bool Audio { get; set; }
		public bool Video { get; set; }
		public string ConnectionId { get; set; }
		public long UserId { get; set; }

		public AudioVisualUserVM(AudioVideoUser avu)
		{
			Name = avu.User.GetName();
			Audio = avu.Audio;
			Video = avu.Video;
			ConnectionId = avu.ConnectionId;
			UserId = avu.User.Id;
		}
	}
}