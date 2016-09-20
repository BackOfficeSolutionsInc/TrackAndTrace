using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;
using Mandrill;

namespace RadialReview.Models.UserModels
{
	public class UserLookup : ILongIdentifiable, IHistorical
	{
		//private bool _isClient;

		[Obsolete("Use UserId instead")]
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual DateTime AttachTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual bool IsRadialAdmin { get; set; }
		public virtual string Name { get; set; }
		public virtual int NumRocks { get; set; }
		public virtual int NumMeasurables { get; set; }
		public virtual int NumRoles { get; set; }
		public virtual string Email { get; set; }
		public virtual string Positions { get; set; }
		public virtual string Teams { get; set; }
		public virtual string Managers { get; set; }
		public virtual bool IsManager { get; set; }
		public virtual bool IsAdmin { get; set; }
		public virtual bool HasJoined { get; set; }
		public virtual bool HasSentInvite { get; set; }
		public virtual bool IsImplementer { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual DateTime? LastLogin { get; set; }

		public virtual bool _PersonallyManaging { get; set; }
		public virtual string _ImageUrlSuffix { get; set; }

		public virtual bool IsClient { get; set; }

		public virtual WebHookEventType? EmailStatus { get; set; }

		public virtual string ImageUrl(ImageSize size = ImageSize._32)
		{
			return TransformImageSuffix(_ImageUrlSuffix, size);
		}

		public static string TransformImageSuffix(string imageSuffix, ImageSize size = ImageSize._32) {

			var s = size.ToString().Substring(1);
			if (imageSuffix != null && !imageSuffix.EndsWith("/i/userplaceholder"))
				return ConstantStrings.AmazonS3Location + s + imageSuffix;
			return "/i/userplaceholder";
		}

		public virtual string GetInitials()
		{
			var inits = (Name ?? "").Split(' ').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Substring(0, 1).ToUpperInvariant()).ToList();
			while(inits.Count>2)
				inits.RemoveAt(1);

			return string.Join(" ", inits).ToUpperInvariant();
		}

		public class UserLookupMap : ClassMap<UserLookup>
		{
			public UserLookupMap()
			{
#pragma warning disable CS0618 // Type or member is obsolete
				Id(x => x.Id);
#pragma warning restore CS0618 // Type or member is obsolete
				Map(x => x.UserId).Index("UserLookup_UserId_IDX");
				Map(x => x.AttachTime);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.NumRocks);
				Map(x => x.IsClient);
				Map(x => x.IsImplementer);
				Map(x => x.IsRadialAdmin);
				Map(x => x.NumMeasurables);
				Map(x => x.NumRoles);
				Map(x => x.Email);
				Map(x => x.Positions);
				Map(x => x.Teams);
				Map(x => x.Managers);
				Map(x => x.IsManager);
				Map(x => x.IsAdmin);
				Map(x => x.HasJoined);
				Map(x => x.LastLogin);
				Map(x => x.HasSentInvite);
				Map(x => x.EmailStatus).Nullable().CustomType<WebHookEventType>();
				Map(x => x.OrganizationId).Index("UserLookup_OrganizationId_IDX");
				Map(x => x._ImageUrlSuffix);
			}
		}


	}
}