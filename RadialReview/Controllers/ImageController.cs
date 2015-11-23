using ImageResizer;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Todo;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Encrypt;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ImageController : BaseController
    {                    
        public static readonly byte[] TrackingGif = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x1, 0x0, 0x1, 0x0, 0x80, 0x0, 0x0, 0xff, 0xff, 0xff, 0x0, 0x0, 0x0, 0x2c, 0x0, 0x0, 0x0, 0x0, 0x1, 0x0, 0x1, 0x0, 0x0, 0x2, 0x2, 0x44, 0x1, 0x0, 0x3b };

        [Access(AccessLevel.Any)]
        public ActionResult TodoCompletion(string id,long userId=-1)
        {
            // Construct absolute image path
            try
            {
                long tryId = -1;
                Response.AppendHeader("Cache-Control", "no-cache, max-age=0");

                if (long.TryParse(Crypto.DecryptStringAES(id, TodoAccessor._SharedSecretTodoPrefix(userId)), out tryId) && tryId > 0)
                {
                    using (var s = HibernateSession.GetCurrentSession())
                    {
                        using (var tx = s.BeginTransaction())
                        {
                            var todo = s.Get<TodoModel>(tryId);

                            if (todo == null || todo.AccountableUserId!=userId)
                                return File(TrackingGif, "image/gif");
                            if (todo.CompleteTime == null)
                                return File("~/Content/email/Unchecked.png", "image/png");
                            else
                                return File("~/Content/email/Checked.png", "image/png");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var o = "";
            }
            
            return File(TrackingGif, "image/gif");
        }

        //
        // GET: /Img/
        [Access(AccessLevel.Any)]
        [OutputCache(Duration = 60 * 60 * 24 * 30, VaryByParam = "id;dim")]
        public ActionResult Index(string id,string dim=null)
        {
            try
            {
                var imagePath = "";
                if (id == "userplaceholder"){
                    imagePath = ConstantStrings.AmazonS3Location +ConstantStrings.ImageUserPlaceholder;
                }
                else if (id == "placeholder")
                {
                    imagePath = ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder; //Server.MapPath("~/" + ConstantStrings.ImagePlaceholder);
                }else if(id=="wait"){
                    Thread.Sleep(500);
                    return File(TrackingGif, "image/gif");
                }else{
                    var user = GetUserModel();
                    imagePath = _ImageAccessor.GetImagePath(user, id);
                }

                if (dim!=null)
                {
                    try
                    {
                        var args = dim.Split('x');
                        var width = int.Parse(args[0]);
                        var height = int.Parse(args[1]);
                        var quality = 80;
                        var settings = new ResizeSettings(width,height,FitMode.Pad,"png");
                        settings.BackgroundColor = Color.FromArgb(180,Color.White);
                        settings.Add("quality", quality.ToString());
                        System.IO.MemoryStream outStream = new System.IO.MemoryStream();

                        HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(imagePath);
                        using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
                        {
                            using (Stream stream = httpWebReponse.GetResponseStream())
                            {
                                ImageBuilder.Current.Build(stream, outStream, settings);
                            }
                        }

                        return File(outStream.ToArray(),"image/png");

                    }catch(Exception e)
                    {
                        log.Error(e);
                    }
                }

                //Response.CacheControl = ""+(60*60*24*14);
                //Response.Expires = (60 * 24 * 14);

                return Redirect(imagePath);

            }catch(PermissionsException)
            {
                return Redirect(ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder);
                //return File(ConstantStrings.ImagePlaceholder, "image/png");
            }
        }
	}
}