using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Amazon.ElasticTranscoder.Model;
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
		public static List<Dashboard> GetDashboardsForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var user = s.Get<UserOrganizationModel>(userId);
					if (user == null || user.User == null)
						throw new PermissionsException("User does not exist.");

					PermissionsUtility.Create(s, caller).ViewDashboardForUser(user.User.Id);
					return s.QueryOver<Dashboard>().Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id).List().ToList();
				}
			}
		}

		public static Dashboard GetPrimaryDashboardForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var user = s.Get<UserOrganizationModel>(userId);
					if (user==null || user.User==null)
						throw  new PermissionsException("User does not exist.");

					PermissionsUtility.Create(s, caller).ViewDashboardForUser(user.User.Id);
					return s.QueryOver<Dashboard>()
						.Where(x => x.DeleteTime == null && x.ForUser.Id == user.User.Id && x.PrimaryDashboard)
						.OrderBy(x => x.CreateTime).Desc
						.Take(1).SingleOrDefault();
				}
			}
		}

		public static Dashboard CreateDashboard(UserOrganizationModel caller, string title, bool primary,bool defaultDashboard=false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (caller.User==null)
						throw new PermissionsException("User does not exist.");


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
					if (defaultDashboard){
						var perms = PermissionsUtility.Create(s, caller);
						//x: 0, y: 0, w: 1, h: 1
						CreateTile(s, perms, dash.Id, 1, 1, 0, 0, "/TileData/UserProfile2",		"Profile",			TileType.Profile);
						if (caller.IsManager()){
							//x: 0, y: 1, w: 1, h: 3
							CreateTile(s, perms, dash.Id, 3, 1, 0, 1, "/TileData/UserManage2", "Managing",			TileType.Manage);
						}
						//x: 1, y: 2, w: 3, h: 2
						CreateTile(s, perms, dash.Id, 2, 3, 1, 2, "/TileData/UserTodo2",			"To-dos",		TileType.Todo);
						//x: 1, y: 0, w: 6, h: 2
						CreateTile(s, perms, dash.Id, 2, 6, 1, 0, "/TileData/UserScorecard2",	"Scorecard",		TileType.Scorecard);
						//x: 4, y: 2, w: 3, h: 2
						CreateTile(s, perms, dash.Id, 2, 3, 4, 2, "/TileData/UserRock2",			"Rocks",		TileType.Rocks);
					}

					tx.Commit();
					s.Flush();
					return dash;
				}
			}
		}

		public static TileModel CreateTile(ISession s, PermissionsUtility perms, long dashboardId, int h, int w, int x, int y, string dataUrl, string title, TileType type,string keyId=null)
		{
			perms.EditDashboard(dashboardId);
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
                KeyId = keyId,
			});

			s.Save(tile);
			return tile;
		}

		public static TileModel CreateTile(UserOrganizationModel caller, long dashboardId, int h, int w, int x, int y, string dataUrl, string title, TileType type,string keyId=null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){

					var perms = PermissionsUtility.Create(s, caller);
					var tile = CreateTile(s, perms, dashboardId, h, w, x, y, dataUrl, title, type, keyId);
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
						.List().OrderBy(x=>x.Y).ThenBy(x=>x.X).ToList();

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