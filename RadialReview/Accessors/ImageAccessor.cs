using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ImageResizer;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class ImageAccessor : BaseAccessor {
		public String GetImagePath(UserModel caller, String imageId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//PermissionsUtility.Create(s, caller).ViewImage(imageId);

					if (imageId == null)
						return ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder;
					return ConstantStrings.AmazonS3Location + s.Get<ImageModel>(Guid.Parse(imageId)).Url;
				}
			}
		}
		/*
		public static String GetUrl(ISession session, Guid guid)
		{
			return session.Get<ImageModel>(guid).Url;
		}
		*/
		public static Instructions HUGE_INSTRUCTIONS = new Instructions("width=2048;height=2048;format=png;mode=max");

		public static Instructions BIG_INSTRUCTIONS = new Instructions("width=256;height=256;format=png;mode=max");
		public static Instructions TINY_INSTRUCTIONS = new Instructions("width=32;height=32;format=png;mode=max");
		public static Instructions MED_INSTRUCTIONS = new Instructions("width=64;height=64;format=png;mode=max");
		public static Instructions LARGE_INSTRUCTIONS = new Instructions("width=128;height=128;format=png;mode=max");

		public static void Upload(Stream stream, string path, Instructions instructions) {
			using (var ms = new MemoryStream()) {
				stream.Seek(0, SeekOrigin.Begin);
				var i = new ImageJob(stream, ms, instructions);
				i.Build();
				ms.Seek(0, SeekOrigin.Begin);

				var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
					BucketName = "Radial",
					InputStream = ms,
					StorageClass = S3StorageClass.ReducedRedundancy,
					Key = path,
					CannedACL = S3CannedACL.PublicRead,
				};
				fileTransferUtilityRequest.Headers.CacheControl = "public, max-age=604800";

				var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
				fileTransferUtility.Upload(fileTransferUtilityRequest);
			}
		}

		public bool RemoveImage(UserModel caller, string userId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					if (caller.Id != userId)
						throw new PermissionsException("Cannot remove image");

					var user = s.Get<UserModel>(userId);
					user.ImageGuid = null;
					s.Save(user);
					tx.Commit();
					s.Flush();
					return true;
				}
			}
		}


		public static async Task<ImageModel> UploadProfileImageForUser(UserOrganizationModel caller, long forUserId, string filename, Stream inputStream, bool huge = false) {
			
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditUserDetails(forUserId);
				}
			}

			var img = await RawUploadImage(caller.User, filename, inputStream, UploadType.ProfileImage, huge);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var userOrg = s.Get<UserOrganizationModel>(forUserId);

					if (userOrg.TempUser != null) {
						userOrg.TempUser.ImageGuid = img.Id.ToString();
					} else {
						var user = userOrg.User;
						user.ImageGuid = img.Id.ToString();

						if (user.UserOrganization != null && user.UserOrganization.Any()) {
							foreach (var u in user.UserOrganization) {
								u.UpdateCache(s);
							}
						}

						var cache = new Cache();
						cache.InvalidateForUser(user.Id, CacheKeys.USER);
						cache.InvalidateForUser(user.Id, CacheKeys.USERORGANIZATION);
					}

					tx.Commit();
					s.Flush();

				}
			}
			return img;
		}

		private static async Task<ImageModel> RawUploadImage(UserModel uploader, string filename, Stream inputStream, UploadType type, bool huge = false) {
			var img = new ImageModel() {
				OriginalName = Path.GetFileName(filename),
				UploadedBy = uploader,
				UploadType = type
			};
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Save(img);
					tx.Commit();
					s.Flush();
				}
			}
			var guid = img.Id.ToString();
			var path = "img/" + guid + ".png";
			var pathTiny = "32/" + guid + ".png";
			var pathMed = "64/" + guid + ".png";
			var pathLarge = "128/" + guid + ".png";
			var pathHuge = "2048/" + guid + ".png";

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var sBig = new MemoryStream();
					var sTiny = new MemoryStream();
					var sMed = new MemoryStream();
					var sLarge = new MemoryStream();
					var sHuge = new MemoryStream();
					inputStream.Seek(0, SeekOrigin.Begin);
					await inputStream.CopyToAsync(sBig);
					inputStream.Seek(0, SeekOrigin.Begin);
					await inputStream.CopyToAsync(sTiny);
					inputStream.Seek(0, SeekOrigin.Begin);
					await inputStream.CopyToAsync(sMed);
					inputStream.Seek(0, SeekOrigin.Begin);
					await inputStream.CopyToAsync(sLarge);
					inputStream.Seek(0, SeekOrigin.Begin);

					if (huge) {
						await inputStream.CopyToAsync(sHuge);
						inputStream.Seek(0, SeekOrigin.Begin);
					}
					Upload(sBig, path, BIG_INSTRUCTIONS);
					Upload(sTiny, pathTiny, TINY_INSTRUCTIONS);
					Upload(sMed, pathMed, MED_INSTRUCTIONS);
					Upload(sLarge, pathLarge, LARGE_INSTRUCTIONS);

					if (huge) {
						Upload(sHuge, pathHuge, HUGE_INSTRUCTIONS);
					}

					img.Url = path;

					if (huge) {
						img.Url = pathHuge;
					}

					s.Update(img);
				}
			}
			return img;
		}



		public async Task<String> UploadImage(UserModel user,UserOrganizationModel caller, string filename, Stream inputStream, UploadType type, bool huge = false) {
			var img = await RawUploadImage(user, filename, inputStream, type, huge);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					switch (type) {
						case UploadType.ProfileImage: {
								user = s.Get<UserModel>(user.Id);
								var old = user.ImageGuid;
								user.ImageGuid = img.Id.ToString();
								s.Update(user);
								if (user.UserOrganization != null && user.UserOrganization.Any()) {
									foreach (var u in user.UserOrganization) {
										u.UpdateCache(s);
									}
								}
								var cache = new Cache();
								cache.InvalidateForUser(user.Id, CacheKeys.USER);
								cache.InvalidateForUser(user.Id, CacheKeys.USERORGANIZATION);
							};
							break;
						case UploadType.AppImage:
							break;
						case UploadType.Logo:
							PermissionsUtility.Create(s, caller).ManagingOrganization(caller.Organization.Id);
							var org = s.Get<OrganizationModel>(caller.Organization.Id);
							org._Settings.ImageGuid = img.Id.ToString();
							s.Update(org);
							break;
						default:
							throw new PermissionsException();
					}
					tx.Commit();
					s.Flush();
				}
			}
			return ConstantStrings.AmazonS3Location + img.Url;
		}

		public async Task<String> UploadImage(UserModel user,UserOrganizationModel caller, HttpServerUtilityBase server, HttpPostedFileBase file, UploadType uploadType) {
			var filename = file.FileName;
			var inputStream = file.InputStream;
			return await UploadImage(user,caller, filename, inputStream, uploadType);
			/*
			TransferUtility utility = new TransferUtility("AKIAJYCO3OR34HOFIQTQ", "HKotVY6T302RWUcHbDu+zyQlwBILKcp+99on8bs9",);
			utility.Upload(file.InputStream, "Radial", path);	

			using (var client = Amazon.AWSClientFactory.CreateAmazonS3Client(AWS.Key, AWS.Secret))
			{
				MemoryStream ms = new MemoryStream();
				PutObjectRequest request = new PutObjectRequest();
				request.BucketName="Radial";
				request.CannedACL = S3CannedACL.PublicRead;
				request.Key = path;
				request.InputStream = file.InputStream;
				PutObjectResponse response = client.PutObject(request);
			}
			*/
		}

		private String GetPath(HttpServerUtilityBase server, String imageId) {
			var fileName = imageId + ".png";
			// store the file inside ~/App_Data/uploads folder
			var dir = server.MapPath("~/App_Data/uploads");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			var path = Path.Combine(dir, fileName);
			return path;
		}

		private String GetOldPath(HttpServerUtilityBase server, String imageId) {
			var fileName = imageId + ".png";
			// store the file inside ~/App_Data/uploads folder
			var dir = server.MapPath("~/App_Data/uploads/old");
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			var path = Path.Combine(dir, fileName);
			return path;
		}

	}
}
