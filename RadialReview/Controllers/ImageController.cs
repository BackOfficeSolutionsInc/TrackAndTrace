﻿using ImageResizer;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Properties;
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

        //
        // GET: /Img/
        [Access(AccessLevel.Any)]
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


                return Redirect(imagePath);

            }catch(PermissionsException)
            {
                return Redirect(ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder);
                //return File(ConstantStrings.ImagePlaceholder, "image/png");
            }
        }
	}
}