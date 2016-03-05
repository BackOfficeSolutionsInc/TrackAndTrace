using Amazon.S3;
using Amazon.S3.Transfer;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors
{

    public class UploadAccessor
    {
        public async static Task<UploadModel> UploadFile(UserOrganizationModel caller, UploadType type, HttpPostedFileBase file, ForModel forModel = null)
        {
            return await UploadFile(caller, type, file.ContentType, file.FileName, file.InputStream, forModel);
        }
       

        public async static Task<UploadModel> UploadFile(UserOrganizationModel caller, UploadType type, String contentType, String originalName, Stream stream, ForModel forModel = null)
        {
            UploadModel upload = null;

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    PermissionsUtility.Create(s, caller).CanUpload();

                    upload = new UploadModel()
                    {
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

            using (var ms = new MemoryStream())
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var path = upload.GetPath(false);

                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = "Radial",
                    InputStream = ms,
                    StorageClass = S3StorageClass.ReducedRedundancy,
                    Key = path,
                    ContentType = contentType,
                    CannedACL = S3CannedACL.PublicRead,
                };
                fileTransferUtilityRequest.Headers.CacheControl = "public, max-age=604800";

                var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
                fileTransferUtility.Upload(fileTransferUtilityRequest);
            }
            return upload;
        }
    }
}