using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsvHelper;
using RadialReview.Utilities;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using System.Net;
using System.Globalization;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Issues;
using RadialReview.Models.Todo;
using RadialReview.Models.ViewModels;
using RadialReview.Models.Accountability;

namespace RadialReview.Controllers
{
	public partial class UploadController : BaseController
	{

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<PartialViewResult> ProcessUserSelection(IEnumerable<int> fnames, IEnumerable<int> lnames, IEnumerable<int> emails, IEnumerable<int> positions, IEnumerable<int> mfnames, IEnumerable<int> mlnames, long recurrenceId, string path, FileType fileType)
		{
			try
			{
				var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);

				var fnameRect = new Rect(fnames);

				fnameRect.EnsureRowOrColumn();

				var m = new UploadUsersSelectedDataVM() { };
				var orgId = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, false).OrganizationId;
				var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), orgId);
				m.ExistingUser = new List<string>();


				var now = DateTime.UtcNow;
				if (fileType == FileType.CSV)
				{
					var csvData = ui.Csv;

					var lnamesRect = new Rect(lnames);
					var emailRect = new Rect(emails);
					lnamesRect.EnsureSameRangeAs(fnameRect);
					emailRect.EnsureSameRangeAs(fnameRect);


					m.FNames = fnameRect.GetArray1D(csvData);
					m.LNames = lnamesRect.GetArray1D(csvData);
					m.Emails = emailRect.GetArray1D(csvData);

					if (positions != null)
					{
						var positionsRect = new Rect(positions);
						positionsRect.EnsureSameRangeAs(fnameRect);
						m.Positions = positionsRect.GetArray1D(csvData);
						m.IncludePositions = true;
					}

					if (mfnames != null && mlnames != null)
					{
						var mfNamesRect = new Rect(mfnames);
						var mlNamesRect = new Rect(mlnames);
						mfNamesRect.EnsureSameRangeAs(fnameRect);
						mlNamesRect.EnsureSameRangeAs(fnameRect);
						m.ManagerFNames = mfNamesRect.GetArray1D(csvData);
						m.ManagerLNames = mlNamesRect.GetArray1D(csvData);
						m.IncludeManagers = true;
					}

				}
				else
				{
					throw new PermissionsException("Expecting a csv.");
				}

				var newNames = new List<string>();
				for (var i = 0; i < m.FNames.Count; i++)
				{
					var name = m.FNames[i] + " " + m.LNames[i];
					newNames.Add(name);
				}

				var userLookup = DistanceUtility.TryMatch(newNames, allUsers);


				for (var i = 0; i < newNames.Count; i++)
				{
					var found = userLookup[newNames[i]].GetProbabilities().Where(x => x.Value > 0.4).Select(x => x.Key.FirstName + " " + x.Key.LastName);

					if (!found.Any())
						m.ExistingUser.Add(null);
					else
						m.ExistingUser.Add(string.Join(", or ", found));
				}




				m.Path = path;

				return PartialView("UploadUsersSelected", m);
			}
			catch (Exception e)
			{
				//e.Data.Add("AWS_ID", path);
				throw new Exception(e.Message + "[" + path + "]", e);
			}
		}



		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> SubmitUsers(FormCollection model)
		{
			var path = model["Path"].ToString();
			try
			{
				//var useAws = model["UseAWS"].ToBoolean();
				var recurrence = model["recurrenceId"].ToLong();

				_PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));

				var now = DateTime.UtcNow;
				var keys = model.Keys.OfType<string>();
				var fns = keys.Where(x => x.StartsWith("m_fn_"))
					.Where(x => !String.IsNullOrWhiteSpace(model[x]))
					.ToDictionary(x => x.SubstringAfter("m_fn_").ToInt(), x => (string)model[x]);

				var lns = keys.Where(x => x.StartsWith("m_ln_"))
					.ToDictionary(x => x.SubstringAfter("m_ln_").ToInt(), x => (string)model[x]);

				var emails = keys.Where(x => x.StartsWith("m_emails_"))
					.ToDictionary(x => x.SubstringAfter("m_emails_").ToInt(), x => (string)model[x]);

				var pos = keys.Where(x => x.StartsWith("m_pos_"))
							   .ToDictionary(x => x.SubstringAfter("m_pos_").ToInt(), x => (string)model[x]);
				var mfns = keys.Where(x => x.StartsWith("m_mfn_"))
							   .ToDictionary(x => x.SubstringAfter("m_mfn_").ToInt(), x => (string)model[x]);
				var mlns = keys.Where(x => x.StartsWith("m_mln_"))
							   .ToDictionary(x => x.SubstringAfter("m_mln_").ToInt(), x => (string)model[x]);
				var incs = keys.Where(x => x.StartsWith("m_include_"))
							   .ToDictionary(x => x.SubstringAfter("m_include_").ToInt(), x => model[x].ToBoolean());

				var caller = GetUser();
				var measurableLookup = new Dictionary<int, MeasurableModel>();

				var existingPositions = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);
				var existingUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
				var managerLookup = new Dictionary<long, string[]>();

				var errors = new CounterSet<String>();


				foreach (var m in incs)
				{
					if (m.Value == false)
						continue;
					var k = m.Key;
					var email = emails.GetOrDefault(k, "").Trim();
					var firstName = fns.GetOrDefault(k, "").Trim();
					var lastName = lns.GetOrDefault(k, "").Trim();
					var position = pos.GetOrDefault(k, "").Trim();
					var managerFirst = mfns.GetOrDefault(k, "").Trim();
					var managerLast = mlns.GetOrDefault(k, "").Trim();

					if ((new[] { email, firstName, lastName, position, managerFirst, managerLast }.All(string.IsNullOrWhiteSpace)))
					{
						//Empty row
						continue;
					}
					var positionFound = existingPositions.FirstOrDefault(x => x.CustomName == position);

					if (positionFound == null && !String.IsNullOrWhiteSpace(position))
					{
						var newPosition = _OrganizationAccessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, /*pos.Id,*/ position);
						existingPositions.Add(newPosition);
						positionFound = newPosition;
					}

					var vm = new CreateUserOrganizationViewModel()
					{
						Email = email,
						FirstName = firstName,
						LastName = lastName,
						OrgId = GetUser().Organization.Id,
						ManagerNodeId = null,
						Position = new UserPositionViewModel()
						{
							CustomPosition = null,
							PositionId = positionFound != null ? positionFound.Id : -2
						},
					};
					try
					{
						var user = (await _UserAccessor.CreateUser(GetUser(), vm)).Item2;
						existingUsers.Add(user);
						managerLookup.Add(user.Id, new[] { managerFirst, managerLast });
						try
						{
							L10Accessor.AddAttendee(GetUser(), recurrence, user.Id);
						}
						catch (PermissionsException e)
						{
							throw new PermissionsException("Could not add " + vm.FirstName + " " + vm.LastName + " to meeting.");
						}
					}
					catch (PermissionsException e)
					{
						errors.Add(e.Message);
					}
					catch (Exception e)
					{
						errors.Add("An error has occurred.");
					}
				}

				AccountabilityAccessor._FinishUploadAccountabilityChart(GetUser(),existingUsers, managerLookup, errors);
				//	foreach (var m in links.Where(x => x.Item1 == s))
				//	{
				//		var foundManager = existingUsers.FirstOrDefault(x => x.GetFirstName() == m.Value[0] && x.GetLastName() == m.Value[1]);
				//		if (foundManager == null) {
				//			errors.Add("Could not find manager " + m.Value[0] + " " + m.Value[1] + ".");
				//			continue;
				//		}
				//		if (!foundManager.IsManager()) {
				//			_UserAccessor.EditUser(GetUser(), foundManager.Id, true);
				//			foundManager.ManagerAtOrganization = true;
				//		}

				//		AccountabilityAccessor.AppendNode(GetUser(),

				//		_UserAccessor.AddManager(GetUser(), m.Key, foundManager.Id, now);
				//	}
				//}
				return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "#Attendees", "Uploaded Users"));
			}
			catch (Exception e)
			{
				throw new Exception(e.Message + "[" + path + "]", e);
			}
		}

		

		public class UploadUsersSelectedDataVM
		{
			public List<string> FNames { get; set; }
			public List<string> LNames { get; set; }
			public List<string> Positions { get; set; }
			public List<string> Emails { get; set; }
			public List<string> ManagerFNames { get; set; }
			public List<string> ManagerLNames { get; set; }
			public List<string> ExistingUser { get; set; }
			public bool IncludePositions { get; set; }
			public bool IncludeManagers { get; set; }


			public string Path { get; set; }
		}
	}
}