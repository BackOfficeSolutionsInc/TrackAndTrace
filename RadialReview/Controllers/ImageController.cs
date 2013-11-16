using ImageResizer;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ImageController : BaseController
    {
        private static ImageAccessor _ImageAccessor = new ImageAccessor();
        //
        // GET: /Img/
        public ActionResult Index(string id,long? organizationId,string dim=null)
        {
            try
            {
                var user = GetOneUserOrganization(organizationId);
                var imagePath = "";
                if (id == "userplaceholder"){
                    imagePath = Server.MapPath("~/"+ConstantStrings.ImageUserPlaceholder);
                }else{
                    imagePath = _ImageAccessor.GetImagePath(user, Server, id);
                }

                if (dim!=null)
                {

                    try
                    {
                        var args = dim.Split('x');
                        var width = int.Parse(args[0]);
                        var height = int.Parse(args[1]);
                        var quality = 80;
                        var settings = new ResizeSettings
                        {
                            MaxWidth = width,
                            MaxHeight = height,
                            Format = "png"
                        };
                        settings.Add("quality", quality.ToString());
                        System.IO.MemoryStream outStream = new System.IO.MemoryStream();
                        ImageBuilder.Current.Build(System.IO.File.ReadAllBytes(imagePath), outStream, settings);
                        return File(outStream.ToArray(),"image/png");

                    }catch(Exception e)
                    {
                        log.Error(e);
                    }
                }


                return File(imagePath, "image/png");


            }catch(PermissionsException e)
            {
                return File(ConstantStrings.ImagePlaceholder, "image/png");
            }
        }
	}
}