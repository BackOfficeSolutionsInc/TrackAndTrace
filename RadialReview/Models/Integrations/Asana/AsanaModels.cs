using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;

namespace RadialReview.Models.Integrations {

	[Flags]
	public enum AsanaActionType {
		NoAction = 0,
		SyncMyTodos=1
		//use flags
	}
	public class AsanaToken : ILongIdentifiable, IHistorical{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string AccessToken { get; set; }
		public virtual string RefreshToken { get; set; }
		public virtual DateTime Expires { get; set; }
		public virtual long CreatorId { get; set; }
		public virtual string RedirectUri { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AsanaUserId { get; set; }


		public AsanaToken() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<AsanaToken> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.AccessToken);
				Map(x => x.RefreshToken);
				Map(x => x.Expires);
				Map(x => x.CreatorId);
				Map(x => x.RedirectUri);
				Map(x => x.OrganizationId);
				Map(x => x.AsanaUserId);
			}
		}

	}


	public class AsanaAction : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long AsanaTokenId { get; set; }
		public virtual long WorkspaceId { get; set; }		
		public virtual ForModel Resource { get; set; }
		public virtual AsanaActionType ActionType { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		private string _Description { get; set; }

		public class Map : ClassMap<AsanaAction> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.AsanaTokenId);
				Map(x => x.WorkspaceId);
				Component(x => x.Resource).ColumnPrefix("Resource_");
				Map(x => x.ActionType).CustomType<AsanaActionType>();
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
			}
		}

		public virtual string GetDescription() {
			if (_Description == null)
				throw new Exception("Description was not populated");
			return _Description;
		}

		public virtual string PopulateDescription(ISession s) {
			var builder = "";

			switch (ActionType) {
				case AsanaActionType.SyncMyTodos:
					builder += "Sync All My Todos";
					break;
				case AsanaActionType.NoAction:
					builder += "No action";
					break;
				default:
					throw new NotImplementedException();
			}
			_Description = builder;
			return builder;
		}

	}

	public class AsanaWorkspace {
		public long Id { get; set; }
		public string Name { get; set; }
	}
}