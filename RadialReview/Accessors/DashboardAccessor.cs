using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using FluentNHibernate;
using FluentNHibernate.Conventions;
using FluentNHibernate.Utils;
using NHibernate;
using NHibernate.Mapping;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Dashboard;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class DashboardAccessor
	{
		public static List<Dashboard> GetDashboardsForUser(UserOrganizationModel caller, string userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewDashboardForUser(userId);
					return s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == userId).List().ToList();
				}
			}
		}

		public static Dashboard GetPrimaryDashboardForUser(UserOrganizationModel caller, string userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ViewDashboardForUser(userId);
					return s.QueryOver<Dashboard>()
						.Where(x => x.DeleteTime == null && x.ForUser.Id == userId && x.PrimaryDashboard)
						.OrderBy(x => x.CreateTime).Desc
						.Take(1).SingleOrDefault();
				}
			}
		}

		public static Dashboard CreateDashboard(UserOrganizationModel caller, string title, bool primary)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					if (primary)
					{
						var existing = s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id && x.PrimaryDashboard).List();
						foreach (var e in existing)
						{
							e.PrimaryDashboard = false;
							s.Update(e);
						}
					}
					else
					{
						//If this the first one, then override primary to true
						primary = (!s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == caller.User.Id).Select(x => x.Id).List<long>().Any());
					}

					var dash = new Dashboard()
					{
						ForUser = caller.User,
						Title = title,
						PrimaryDashboard = primary,
					};

					s.Save(dash);
					tx.Commit();
					s.Flush();
					return dash;
				}
			}
		}

		public static TileModel CreateTile(UserOrganizationModel caller, long dashboardId, int h, int w, int x, int y, string dataUrl, string title, TileType type)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					var perms = PermissionsUtility.Create(s, caller).EditDashboard(dashboardId);

					if (type == TileType.Invalid)
						throw new PermissionsException("Invalid tile type");

					var uri = new Uri(dataUrl, UriKind.Relative);
					if (uri.IsAbsoluteUri)
						throw new PermissionsException("Data url must be relative");

					var dashboard = s.Get<Dashboard>(dashboardId);

					var tile = (new TileModel()
					{
						Dashboard = dashboard,
						DataUrl = dataUrl,
						ForUser = dashboard.ForUser,
						Height = h,
						Width = w,
						X = x,
						Y = y,
						Type = type,
						Title = title,
					});

					s.Save(tile);

					tx.Commit();
					s.Flush();
					return tile;
				}
			}
		}

		public static TileModel EditTile(ISession s, PermissionsUtility perms, long tileId, int? h = null, int? w = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null)
		{
			var tile = s.Get<TileModel>(tileId);

			tile.Height = h ?? tile.Height;
			tile.Width = w ?? tile.Width;
			tile.X = x ?? tile.X;
			tile.Y = y ?? tile.Y;
			tile.Hidden = hidden ?? tile.Hidden;
			tile.Title = title ?? tile.Title;

			if (dataUrl != null)
			{
				//Ensure relative
				var uri = new Uri(dataUrl, UriKind.Relative);
				if (uri.IsAbsoluteUri)
					throw new PermissionsException("Data url must be relative.");
				tile.DataUrl = dataUrl;
			}

			s.Update(tile);

			return tile;
		}

		public static TileModel EditTile(UserOrganizationModel caller, long tileId, int? h = null, int? w = null, int? x = null, int? y = null, bool? hidden = null, string dataUrl = null, string title = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).EditTile(tileId);

					var o = EditTile(s, perms, tileId, h, w, x, y, hidden, dataUrl, title);

					tx.Commit();
					s.Flush();
					return o;
				}
			}
		}


		public static void EditTiles(UserOrganizationModel caller, long dashboardId, IEnumerable<Controllers.DashboardController.TileVM> model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).EditDashboard(dashboardId);

					var editIds = model.Select(x => x.id).ToList();

					var old = s.QueryOver<TileModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(editIds).List().ToList();

					if (!SetUtility.AddRemove(editIds, old.Select(x => x.Id)).AreSame())
					{
						throw new PermissionsException("You do not have access to edit some tiles.");
					}

					if (old.Any(x => x.Dashboard.Id != dashboardId))
						throw new PermissionsException("You do not have access to edit this dashboard.");

					foreach (var o in old)
					{
						var found = model.First(x => x.id == o.Id);
						o.X = found.x;
						o.Y = found.y;
						o.Height = found.h;
						o.Width = found.w;
						s.Update(o);
					}


					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<TileModel> GetTiles(UserOrganizationModel caller, long dashboardId)
		{
			List<TileModel> tiles;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).EditDashboard(dashboardId);

					tiles = s.QueryOver<TileModel>()
						.Where(x => x.DeleteTime == null && x.Dashboard.Id == dashboardId && x.Hidden==false)
						.List().ToList();

				}
			}
			foreach (var tile in tiles)
			{
				tile.ForUser = null;
				tile.Dashboard = null;
			}
			return tiles;
		}
	}
}