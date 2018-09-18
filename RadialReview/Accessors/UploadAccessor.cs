using Amazon.S3;
using Amazon.S3.Transfer;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public enum FileType {
		Invalid,
		CSV,
		Lines,
		XLS,
		XLSX,
	}
	public class UploadInfo {
		public string Path { get; set; }
		public bool UseAWS { get; set; }
		public List<List<string>> Csv { get; set; }
		public List<string> Lines { get; set; }
		public DiscreteDistribution<FileType> FileType { get; set; }

		public FileType GetLikelyFileType() {
			return FileType.ResolveOne();
		}
	}
	public class UploadAccessor {
		public async static Task<UploadModel> UploadFile(UserOrganizationModel caller, UploadType type, HttpPostedFileBase file, ForModel forModel = null) {
			return await UploadFile(caller, type, file.ContentType, file.FileName, file.InputStream, forModel);
		}

		public async static Task<UploadModel> UploadFile(UserOrganizationModel caller, UploadType type, String contentType, String originalName, Stream stream, ForModel forModel = null) {
			UploadModel upload = null;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					stream.Seek(0, SeekOrigin.Begin);
					PermissionsUtility.Create(s, caller).CanUpload();

					upload = new UploadModel() {
						ForModel = forModel,
						MimeType = contentType,
						OrganizationId = caller.Organization.Id,
						OriginalName = originalName,
						CreatedBy = caller.Id,
						UploadType = type,
					};

					s.Save(upload);

					tx.Commit();
					s.Flush();
				}
			}

			//using (var mscopy2 = new MemoryStream()) {
			using (var mscopy = new MemoryStream()) {
				using (var ms = new MemoryStream()) {
					stream.Seek(0, SeekOrigin.Begin);
					await stream.CopyToAsync(ms);
					ms.Seek(0, SeekOrigin.Begin);
					await stream.CopyToAsync(mscopy);
					mscopy.Seek(0, SeekOrigin.Begin);
					//await stream.CopyToAsync(mscopy2);
					//mscopy2.Seek(0, SeekOrigin.Begin);

					var path = upload.GetPath(false);

					var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
						BucketName = "Radial",
						InputStream = ms,
						StorageClass = S3StorageClass.ReducedRedundancy,
						Key = path,
						ContentType = contentType,
						CannedACL = S3CannedACL.PublicRead,
					};
					fileTransferUtilityRequest.Headers.CacheControl = "public, max-age=604800";

					var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
					await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
					//ms.Seek(0, SeekOrigin.Begin);
					upload._Data = mscopy.ReadBytes();
					mscopy.Seek(0, SeekOrigin.Begin);
					//upload._Stream = mscopy2;
					//upload._Stream.Seek(0, SeekOrigin.Begin);
				}
			}
			// }
			return upload;
		}

        public static string MockUpload(string file) {
            var guid = "noaws_" + Guid.NewGuid();
            Backup[guid] = file;
            return guid;
        }

		private static Dictionary<string, string> Backup = new Dictionary<string, string>();
		public async static Task<UploadInfo> UploadAndParse(UserOrganizationModel caller, UploadType type, HttpPostedFileBase file, ForModel forModel) {
			if (file != null && file.ContentLength > 0) {
				using (var ms = new MemoryStream()) {
					await file.InputStream.CopyToAsync(ms);

					var o = new UploadInfo();
					if (file.ContentType.NotNull(x => x.ToLower().Contains("application/vnd.ms-excel")) && (file.FileName.NotNull(x => x.ToLower()) ?? "").EndsWith(".xls"))
						throw new FileTypeException(FileType.XLS);
					if (file.ContentType.NotNull(x => x.ToLower().Contains("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")) && (file.FileName.NotNull(x => x.ToLower()) ?? "").EndsWith(".xlsx"))
						throw new FileTypeException(FileType.XLSX);

					Parse(ms, ref o);

					if (file.ContentType.NotNull(x => x.ToLower().Contains("csv")))
						o.FileType.Add(FileType.CSV, 2);

					if (file.FileName.NotNull(x => x.ToLower().Contains(".csv")))
						o.FileType.Add(FileType.CSV, 1);

					if (file.FileName.NotNull(x => x.ToLower().Contains(".txt")))
						o.FileType.Add(FileType.Lines, 1);



					//if (file.ContentType.NotNull(x => x.ToLower().Contains("text")))
					//    o.FileType.Add(FileType.Lines, 2);

					var useAws = true;
					string path = null;
					try {
						var upload = await UploadAccessor.UploadFile(caller, type, file, forModel);
						path = upload.GetPath();
					} catch (Exception) {
						useAws = false;
						ms.Seek(0, SeekOrigin.Begin);
						var read = ms.ReadToEnd();
						path = "noaws_" + Guid.NewGuid().ToString();
						Backup[path] = read;
					}
					o.Path = path;
					o.UseAWS = useAws;
					return o;
				}
			} else {
				if (file == null)
					throw new FileNotFoundException("File was not found.");
				throw new FileNotFoundException("File was empty.");
			}
		}

		private static void Parse(MemoryStream ms, ref UploadInfo ui) {
			ms.Seek(0, SeekOrigin.Begin);
			var text = ms.ReadToEnd();
			ui.Lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
			ui.Csv = CsvUtility.Load(text.ToStream());

			var dist = new DiscreteDistribution<FileType>(0, 2, true);

			foreach (var l in ui.Lines) {
				if (l.Split(',').Count() > 1)
					dist.Add(FileType.CSV, 1);
				else
					dist.Add(FileType.Lines, 1);
			}

			ui.FileType = dist;
		}

		public async static Task<UploadInfo> DownloadAndParse(UserOrganizationModel caller, string path) {
			UploadInfo ui = new UploadInfo();
			ui.Path = path;
			using (var ms = new MemoryStream()) {
				if (path.StartsWith("noaws_")) {
					await Backup[path].ToStream().CopyToAsync(ms);
					ui.UseAWS = false;
				} else {
					using (var webClient = new WebClient()) {
						var data = await webClient.DownloadStringTaskAsync("https://s3.amazonaws.com/" + path);
						await data.ToStream().CopyToAsync(ms);
						ui.UseAWS = false;
					}
				}
				Parse(ms, ref ui);
			}
			return ui;
		}
	}
}