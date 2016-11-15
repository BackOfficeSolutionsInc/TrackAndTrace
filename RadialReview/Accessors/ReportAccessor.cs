using PdfSharp.Pdf;
using RadialReview.Models;
using RadialReview.Models.Pdf;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using RadialReview.Controllers;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace RadialReview.Accessors {
	public class ReportAccessor {

		public static List<ReportPdfModel> ListAllReports(UserOrganizationModel caller, long reviewContainerId,bool requireFinalized = true) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).AdminReviewContainer(reviewContainerId);
					var q = s.QueryOver<ReportPdfModel>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewContainerId);
					if (requireFinalized)
						q = q.Where(x => x.Finalized == true);
					return q.List().ToList();
				}
			}
		}
		

		public static async Task<ReportPdfModel> ArchiveReport(UserOrganizationModel caller, ReviewController.ReviewDetailsViewModel model,bool finalized) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewReview(model.Review.Id);

					var pdf = PdfAccessor.GenerateReviewPrintout(caller, model);

					if (finalized) {
						var existingFinalized = s.QueryOver<ReportPdfModel>().Where(x => x.Finalized == true && x.DeleteTime == null && x.ForReviewId == model.Review.Id).List().ToList();
						foreach (var e in existingFinalized) {
							e.Finalized = false;
							s.Update(e);
						}
					}

					var pdfUpload = new ReportPdfModel() {
						CreatedBy = caller.Id,
						ForReviewContainerId = model.ReviewContainer.Id,
						OrganizationId = model.ReviewContainer.ForOrganizationId,
						Filename = pdf.Info.Title,
						Finalized = finalized,
						ForUserId = model.Review.ForUserId,
						ForReviewId = model.Review.Id,
						PdfType = PdfType.Report,
						Sent = false,
						Signed = false,
					};

					s.Save(pdfUpload);

					using (var ms = new MemoryStream()) {
						var stream = new MemoryStream();
						pdf.Save(stream, false);

						stream.Seek(0, SeekOrigin.Begin);
						await stream.CopyToAsync(ms);
						ms.Seek(0, SeekOrigin.Begin);


						var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
							BucketName = "Radial",
							InputStream = ms,
							StorageClass = S3StorageClass.ReducedRedundancy,
							Key = pdfUpload.GetPath(),
							CannedACL = S3CannedACL.PublicRead,
							ContentType = "application/pdf"
						};
						fileTransferUtilityRequest.Headers.CacheControl = "public, max-age=604800";

						var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
						await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);

					}

					tx.Commit();
					s.Flush();
					return pdfUpload;
				}
			}
		}
	}
}