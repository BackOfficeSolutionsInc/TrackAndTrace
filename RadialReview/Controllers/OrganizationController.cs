using System.IO;
using System.Text;
using CsvHelper;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Extensions;

namespace RadialReview.Controllers {
	public class OrganizationController : BaseController {
		//
		// GET: /Organization/
		[Access(AccessLevel.Any)]
		public ActionResult Index() {
			return View();
		}

		[Access(AccessLevel.Any)]
		public ActionResult Create(int? count) {
			var user = GetUserModel();
			if (count == null)
				return RedirectToAction("Index");

			return View();
		}

		[HttpPost]
		[Access(AccessLevel.Any)]
		public ActionResult Create(String name,bool enableL10,bool enableReview) {
			Boolean managersCanEdit = false;
			var user = GetUserModel();
			var basicPlan=_PaymentAccessor.BasicPaymentPlan();
			var localizedName=new LocalizedStringModel() { Standard = name };
			long newRoleId;
			var organization = _OrganizationAccessor.CreateOrganization(user, localizedName, managersCanEdit, basicPlan, DateTime.UtcNow, out newRoleId,enableL10,enableReview);
			return RedirectToAction("SetRole", "Account", new { id = newRoleId });
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Products()
		{
			_PermissionsAccessor.Permitted(GetUser(),x=>x.ManagingOrganization(GetUser().Organization.Id));
			return View(GetUser().Organization);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Products(OrganizationModel model)
		{
			if (ModelState.IsValid){
				_OrganizationAccessor.UpdateProducts(GetUser(), model.Settings.EnableReview, model.Settings.EnableL10,model.Settings.Branding);
				return RedirectToAction("Index", "Manage");
			}
			return View(GetUser().Organization);
		}

		[Access(AccessLevel.Any)]
		public ActionResult Join(String id=null) {
			if (String.IsNullOrWhiteSpace(id))
				throw new PermissionsException("Id cannot be empty.");


			var nexus = _NexusAccessor.Get(id);
			if (nexus.DateExecuted != null)
				throw new RedirectException(ExceptionStrings.AlreadyMember);
			var user = GetUserModel();

			new Cache().Invalidate(AlertHub.REGISTERED_KEY + user.UserName);

			var orgId = int.Parse(nexus.GetArgs()[0]);
			var placeholderUserId = long.Parse(nexus.GetArgs()[2]);
			if (user == null)
				return RedirectToAction("Login", "Account", new{returnUrl = "Organization/Join/" + id});
			try{
				var userOrg = GetUser(placeholderUserId);
				if (!user.IsRadialAdmin){
					throw new RedirectException(ExceptionStrings.AlreadyMember);
				}
				else{
					throw new OrganizationIdException();
				}
			}
			catch (OrganizationIdException){
				//We want to hit this exception.
				new Cache().Invalidate(CacheKeys.ORGANIZATION_ID);
				//Session["OrganizationId"] = null;
				var org = _OrganizationAccessor.JoinOrganization(user, nexus.ByUserId, placeholderUserId);
				_NexusAccessor.Execute(nexus);
				return RedirectToAction("Index", "Home", new{message = String.Format(MessageStrings.SuccessfullyJoinedOrganization, org.Organization.Name)});
			}
		}
		/*
		[Access(AccessLevel.UserOrganization)]
		public ActionResult ManageList()
		{
		}*/

		/*
		public ActionResult Manage(int? organizationId)
		{
			if (organizationId == null)
			{
				var userOrgs = GetUserOrganizations();
				var managing = userOrgs.Where(x => x.IsManager());
				var count = managing.Count();
				if (count == 0)
					throw new PermissionsException();
				else if (count == 1)
					return RedirectToAction("Manage", new { organizationId = managing.First().Organization.Id });
				else
					return View("ManageList", managing.Select(x => x.Organization).ToList());
			}
			else
			{
				var userOrg = GetOneUserOrganization(organizationId.Value)
					.Hydrate()
					.ManagingGroups(questions:true)
					.ManagingUsers(subordinates:true)
					.Organization(questions:true)
					.Reviews(answers:true)
					.Nexus()
					.Execute();

				if (userOrg == null)
					throw new PermissionsException();

				if (!userOrg.IsManager())
					throw new PermissionsException();
                
				return View(new ManageViewModel(userOrg));
			}
		}*/

		[Access(AccessLevel.Any)]
		public ActionResult Begin(int? count = null) {
			ViewBag.Count = count;
			int[] roundUp = new int[] { 10, 15, 25, 50, 100, 500 };
			double[] prices = new double[] { 0, 199, 499, 999, 1999, 3999, Double.MaxValue };

			if (count != null) {
				ViewBag.Price = prices[0];
				for (int i = 0; i < roundUp.Length; i++) {
					if (count > roundUp[i]) {
						ViewBag.Price = prices[i + 1];
					}
				}
			}
			return View();
		}

		[Access(AccessLevel.Any)]
		public ActionResult Redirect(int organizationId, string returnUrl) {
			if (returnUrl.Contains("?"))
				return RedirectToLocal(returnUrl + "&organizationId=" + organizationId);
			else
				return RedirectToLocal(returnUrl + "?organizationId=" + organizationId);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Tree(string type = "cartesian") {
			if (type.ToLower() == "radial") {
				return View("RadialTree", GetUser().Organization.Id);
			}
			else if (type.ToLower() == "forcedirected") {
				return View("ForceDirected", GetUser().Organization.Id);
			}
			else {
				return View("Tree", GetUser().Organization.Id);
			}
		}

		[Access(AccessLevel.Manager)]
		public ActionResult ResendJoin(long id) {
			var found = _UserAccessor.GetUserOrganization(GetUser(), id, true, false);

			if (found.TempUser == null)
				throw new PermissionsException("User is already a part of the organization");

			return PartialView(found.TempUser);
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public async Task<JsonResult> ResendJoin(long id, TempUserModel model, long TempId) {
			var found = _UserAccessor.GetUserOrganization(GetUser(), id, true, false);
			if (found.TempUser == null)
				throw new PermissionsException("User is already a part of the organization");

			_UserAccessor.UpdateTempUser(GetUser(), id, model.FirstName, model.LastName, model.Email, DateTime.UtcNow);
			model.Id = TempId;
			var result = await Emailer.SendEmail(_NexusAccessor.CreateJoinEmailToGuid(GetUser(), model));
			var prefix = "Resent";
			if (model.LastSent == null)
				prefix = "Sent";
			return Json(result.ToResults(prefix+" invite to " + model.Name() + "."));
		}


		#region Uploads
		public class UploadVM {
			public long OrganizationId { get; set; }
			public HttpPostedFileBase File { get; set; }
			public Guid Guid { get; set; }

		}

		[Access(AccessLevel.Manager)]
		[HttpGet]
		public ActionResult Upload() {
			var orgId = GetUser().Organization.Id;

			return View(new UploadVM() {
				OrganizationId = orgId,
			});
		}

		public static Dictionary<Guid,String> CSVs = new Dictionary<Guid, String>();

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public ActionResult Upload(UploadVM model) {
			var file = model.File;

			if (file != null && file.ContentLength > 0) {
				var guid = Guid.NewGuid();
				CSVs[guid] = file.InputStream.ReadToEnd();
				return RedirectToAction("Fields", new { id = guid.ToString() });
			}
			ViewBag.Message = "An error has occurred.";
			return RedirectToAction("Upload");
		}

		public class FieldsVM {
			public Guid Guid { get; set; }
			public string[] Fields { get; set; }
			public String FirstNameColumn { get; set; }
			public String LastNameColumn { get; set; }
			public String EmailColumn { get; set; }
			public String PositionColumn { get; set; }
			public String ManagerFirstNameColumn { get; set; }
			public String ManagerLastNameColumn { get; set; }

		}

		[Access(AccessLevel.Manager)]
		public ActionResult Fields(string id = null) {
			try {
				var guid = Guid.Parse(id);

				var data = CSVs[guid];
				using (var sr = new StreamReader(data.ToStream())) {
					var csv = new CsvReader(sr);
					while (csv.Read()) {}

					var headers = csv.FieldHeaders.ToList();
					
					var model = new FieldsVM() {
						Guid = guid,
						Fields = headers.ToArray(),
					};

					return View(model);
				}

			}
			catch (Exception e) {
				ViewBag.Message = "An error has occurred.";
				return RedirectToAction("Upload");
			}
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public async Task<ActionResult> Fields(FieldsVM model)
	    {
		    var nexus = new NexusController();

		    var existingPositions = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);
		    var existingUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id,false,false);
			var pos = _PositionAccessor.AllPositions().First();
			
			var data = CSVs[model.Guid];

			var managerLookup = new Dictionary<long, string[]>();

			var errors = new CounterSet<String>();

		    using (var sr = new StreamReader(data.ToStream())){
			    var csv = new CsvReader(sr);
			    while (csv.Read()){
					var email = csv.GetField(model.EmailColumn).Trim();
					var firstName = csv.GetField(model.FirstNameColumn).Trim();
					var lastName = csv.GetField(model.LastNameColumn).Trim();
					var position = csv.GetField(model.PositionColumn).Trim();
					var managerFirst = csv.GetField(model.ManagerFirstNameColumn).Trim();
					var managerLast = csv.GetField(model.ManagerLastNameColumn).Trim();

				    if ((new[]{email, firstName, lastName, position, managerFirst, managerLast}.All(string.IsNullOrWhiteSpace))){
						//Empty row
						continue;
				    }

				    var positionFound =existingPositions.FirstOrDefault(x => x.CustomName == position);

					if (positionFound == null && !String.IsNullOrWhiteSpace(position))
					{
						var newPosition = _OrganizationAccessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, /*pos.Id,*/ position);
						existingPositions.Add(newPosition);
						positionFound = newPosition;
					}

				    var vm = new CreateUserOrganizationViewModel(){
						Email = email,
						FirstName = firstName,
						LastName = lastName,
						OrgId = GetUser().Organization.Id,
						ManagerId = -4,
						Position = new UserPositionViewModel(){
							CustomPosition = null,
							PositionId = positionFound!=null?positionFound.Id:-2
						},
				    };
				    try{
					    var user = (await _UserAccessor.CreateUser(GetUser(), vm)).Item2;

					    existingUsers.Add(user);
					    managerLookup.Add(user.Id, new[]{managerFirst, managerLast});
				    }
				    catch (PermissionsException e){
					    errors.Add(e.Message);
				    }
					catch (Exception e) {
						errors.Add("An error has occurred.");
				    }
			    }
		    }
			var now = DateTime.UtcNow;
			foreach (var m in managerLookup){
				var foundManager =existingUsers.FirstOrDefault(x => x.GetFirstName() == m.Value[0] && x.GetLastName() == m.Value[1]);
				if (foundManager == null){
					errors.Add("Could not find manager " + m.Value[0] + " " + m.Value[1] + ".");
					continue;
				}
				if (!foundManager.IsManager()){
					_UserAccessor.EditUser(GetUser(),foundManager.Id,true);
					foundManager.ManagerAtOrganization = true;
				}

				_UserAccessor.AddManager(GetUser(), m.Key, foundManager.Id, now);
			}
			ViewBag.Message = String.Join("\n", errors.Select(x => x.Key));
			return RedirectToAction("Members","Manage");

	    }
		#endregion





	}
}