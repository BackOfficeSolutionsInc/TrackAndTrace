using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class ImageAccessor :BaseAccessor
    {
        public String GetImagePath(UserOrganizationModel caller, HttpServerUtilityBase server,String imageId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewImage(imageId);
                    return GetPath(server, imageId);
                }
            }
        }



        public String UploadImageImage(UserModel user, HttpServerUtilityBase server,HttpPostedFileBase file,UploadType uploadType)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var img = new ImageModel()
                    {
                        OriginalName = Path.GetFileName(file.FileName),
                        UploadedBy = user,
                        UploadType = uploadType
                    };
                    s.Save(img);
                    var guid = img.Id.ToString();
                    var path = GetPath(server, guid);
                    file.SaveAs(path);
                    switch (uploadType)
                    {
                        case UploadType.ProfileImage: {
                            user=s.Get<UserModel>(user.Id);
                            var old = user.Image;
                            if (old != null)
                            {
                                try
                                {
                                    File.Move(GetPath(server, old.Id.ToString()), GetOldPath(server, old.Id.ToString()));
                                }catch(Exception e)
                                {
                                    log.Error("Move Error", e);
                                }
                            }
                            user.Image = img;
                            s.SaveOrUpdate(user);
                        }; break;
                        default: throw new PermissionsException();
                    }
                    tx.Commit();
                    s.Flush();
                    return path;
                }
            }
        }

        private String GetPath(HttpServerUtilityBase server, String imageId)
        {
            var fileName = imageId + ".png";
            // store the file inside ~/App_Data/uploads folder
            var dir = server.MapPath("~/App_Data/uploads");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);
            return path;
        }

        private String GetOldPath(HttpServerUtilityBase server,String imageId)
        {
            var fileName = imageId + ".png";
            // store the file inside ~/App_Data/uploads folder
            var dir=server.MapPath("~/App_Data/uploads/old");
            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);
            return path;
        }

    }
}