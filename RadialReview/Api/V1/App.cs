using Microsoft.AspNetCore.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.UI.WebControls;

namespace RadialReview.Api.V1 {

	[RoutePrefix("api/v1")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class AppController : BaseApiController {

		// GET: api/Scores/5
		[Route("app/roles")]
		[HttpGet]
		public List<AngularUserRole> Roles() {
			var res = GetUser().NotNull(x => x.User.UserOrganization.Select(y => new AngularUserRole(y.Id, x.GetName(), y.GetTitles(), y.Organization.GetName())).ToList());
			return res ?? new List<AngularUserRole>();
		}

		public class UploadImageResult {
			public string Name { get; set; }
			public string Url { get; set; }
			public bool Success { get; set; }
		}

		// GET: api/Scores/5
		[Route("app/uploadImage")]
		[HttpPost]
		public async Task<List<UploadImageResult>> Upload() {
			if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any()) {
				// Get the uploaded image from the Files collection
				var o = new List<UploadImageResult>();
				var files = System.Web.HttpContext.Current.Request.Files;
				var ia = new ImageAccessor();
				foreach (string k in files.Keys) {
					bool success = true;
					string url = null;
					string filename = null;
					try {
						var f = files[k];
						filename = f.FileName;
						url = await ia.UploadImage(GetUser().User,null, filename, f.InputStream, UploadType.AppImage, true);
					} catch (Exception) {
						success = false;
					}

					o.Add(new UploadImageResult() {
						Name = filename,
						Url = url,
						Success = success,
					});
				}

				return o;
			}
			throw new Exception("Image is not uploaded");
		}

		[Route("app/uploadProfilePicture")]
		[HttpPost]
		public async Task<UploadImageResult> UploadProfilePicture() {

			var userModel = GetUser().User;
			var fileKey = System.Web.HttpContext.Current.Request.Files.AllKeys.FirstOrDefault();
			if (fileKey == null)
				throw new Exception("No file");

			var file = System.Web.HttpContext.Current.Request.Files[fileKey];

			//you can put your existing save code here
			if (file != null && file.ContentLength > 0) {
				// extract only the fielname
				var _ImageAccessor = new ImageAccessor();
				var url = await _ImageAccessor.UploadImage(userModel, null, file.FileName, file.InputStream, UploadType.ProfileImage, true);
				return new UploadImageResult() {
					Name = file.FileName,
					Success = true,
					Url = url
				};
			}
			throw new Exception("Image is not uploaded");
		}



		//[Route("app/roles/{ROLE_ID}")]
		//[HttpPost]
		//public bool SetRole(long ROLE_ID) {
		//    UserOrganizationModel userOrg = null;
		//    try {
		//        userOrg = GetUser();
		//    } catch (Exception) {

		//    }

		//    new UserAccessor().ChangeRole(GetUserModel(), userOrg, ROLE_ID);
		//    GetUser(ROLE_ID);
		//    return true;
		//}
	}
}