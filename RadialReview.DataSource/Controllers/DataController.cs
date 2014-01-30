using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Json;

namespace RadialReview.DataSource.Controllers
{
    public class DataController : Controller
    {
        //
        // GET: /Data/
        public JsonResult Scatter()
        {
            var data = GenerateData();

            return Json(data,JsonRequestBehavior.AllowGet);
        }


        private ScatterData GenerateData()
        {
            var points =new List<ScatterDataPoint>();
            Random r=new Random();

            var startDate = new DateTime(1990,11,7);
            var now =startDate;

            var curDims=new List<ScatterDataDimension>();
            var dimMax=4;

            var min=-100;
            var max=100;
            var delta=20;

            for(int dimNum=0;dimNum<dimMax;dimNum++)
            {
                curDims.Add(new ScatterDataDimension(){
                    Name="Dim-"+dimNum,
                    Value=r.NextDouble()*(max-min)+min,
                    Class="class-dim-"+dimNum,
                    Max=max,
                    Min=min
                });
            }

            for(int dateNum=0;dateNum<10;dateNum++)
            {


                points.Add(new ScatterDataPoint(){
                    Date=now,
                    Class="point point-"+dateNum,
                    Dimensions =curDims.ToList()
                });

                now=now.AddDays(r.NextDouble()*365*10);
                var newDims=new List<ScatterDataDimension>();
                for(int dimNum=0;dimNum<dimMax;dimNum++){
                    newDims.Add(new ScatterDataDimension(){
                        Class=curDims[dimNum].Class,
                        Name=curDims[dimNum].Name,
                        Min=curDims[dimNum].Min,
                        Max=curDims[dimNum].Max,
                        Value = Math.Max(min,Math.Min(max,curDims[dimNum].Value+r.NextDouble()*delta))
                    });
                }
                curDims=newDims;
            }


            var data = new ScatterData()
            {
                Class = "scatter-data",
                Points = points
            };
            return data;
        }
	}
}