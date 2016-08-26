using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using RadialReview.Models.Charts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
    public class ScatterPt {
        public double X { get; set; }
        public double Y { get; set; }
        public string Icon { get; set; }
    }
    public class OxyplotAccessor {

        protected static void AddSeries(PlotModel plot,string title, MarkerType marker, OxyColor fill, OxyColor stroke, IEnumerable<Scatter.ScatterPoint> data)
        {
            var scatterSeries = new ScatterSeries {
                MarkerType = marker,
                MarkerStroke = stroke,
                MarkerFill = fill,
                RenderInLegend = true,
                Title = title
            };

            if (marker == MarkerType.Cross) {
                scatterSeries.MarkerType = MarkerType.Custom;
                scatterSeries.MarkerOutline = new ScreenPoint[]{
                    //new ScreenPoint(-two,-two),
                    //new ScreenPoint(-two,-six),
                    //new ScreenPoint(two,-six),
                    //new ScreenPoint(two,-two),
                    //new ScreenPoint(six,-two),
                    //new ScreenPoint(six,two),
                    //new ScreenPoint(two,two),
                    //new ScreenPoint(two,six),
                    //new ScreenPoint(-two,six),
                    //new ScreenPoint(-two,two),
                    //new ScreenPoint(-six,two),
                    //new ScreenPoint(-six,-two),
                    new ScreenPoint(-0.565685424949238,0),
                    new ScreenPoint(-1.13137084989848,-0.565685424949238),
                    new ScreenPoint(-0.565685424949238,-1.13137084989848),
                    new ScreenPoint(0,-0.565685424949238),
                    new ScreenPoint(0.565685424949238,-1.13137084989848),
                    new ScreenPoint(1.13137084989848,-0.565685424949238),
                    new ScreenPoint(0.565685424949238,0),
                    new ScreenPoint(1.13137084989848,0.565685424949238),
                    new ScreenPoint(0.565685424949238,1.13137084989848),
                    new ScreenPoint(0,0.565685424949238),
                    new ScreenPoint(-0.565685424949238,1.13137084989848),
                    new ScreenPoint(-1.13137084989848,0.565685424949238),





                };
            }


            foreach (var d in data) {
                var size = 8;
              //  var colorValue = 1000;
                scatterSeries.Points.Add(new ScatterPoint((double)d.cx, (double)d.cy, size));
            }
            plot.Series.Add(scatterSeries);
        }

        public static PlotModel ScatterPlot(Scatter plot,int margin)
        {
            var borderColor =  OxyColor.FromRgb(128,128,128);

            var model = new PlotModel {
                Title = "",
                //Padding = new OxyThickness(5),
                PlotMargins = new OxyThickness(10, 5, 10, 190),
                PlotAreaBorderColor =borderColor,
                PlotAreaBorderThickness = new OxyThickness(1),

                //PlotAreaBackground = OxyColor.FromRgb(0,128,0),
                //PlotType = PlotType.Cartesian
                
                
            };
            model.PlotAreaBorderThickness = new OxyThickness(0);
            var self = plot.Points.Where(x => x.@class.Contains("about-Self"));
            AddSeries(model,"Self", MarkerType.Cross, OxyColor.FromRgb(255, 255, 0), OxyColor.FromRgb(184, 134, 11), self);
            var manager = plot.Points.Where(x => x.@class.Contains("about-Manager"));
            AddSeries(model,"Supervisor", MarkerType.Square, OxyColor.FromRgb(173, 216, 230), OxyColor.FromRgb(0, 0, 255), manager);
            var peer = plot.Points.Where(x => x.@class.Contains("about-Peer"));
            AddSeries(model,"Peer", MarkerType.Triangle, OxyColor.FromRgb(122, 209, 122), OxyColor.FromRgb(48, 89, 48), peer);
            var subordinate = plot.Points.Where(x => x.@class.Contains("about-Subordinate"));
            AddSeries(model,"Direct Report", MarkerType.Diamond, OxyColor.FromRgb(255, 0, 0), OxyColor.FromRgb(139, 0, 0), subordinate);
            var norelationship = plot.Points.Where(x => x.@class.Contains("about-NoRelationship"));
            AddSeries(model,"No Relationship", MarkerType.Circle, OxyColor.FromRgb(128, 128, 128), OxyColor.FromRgb(0, 0, 0), norelationship);

            model.LegendSymbolPlacement = LegendSymbolPlacement.Left;
            model.LegendLineSpacing = 5;
            model.LegendFontSize = 18;

            model.LegendPosition = LegendPosition.RightTop;
            model.LegendPlacement = LegendPlacement.Outside;

           // model.PlotMargins = new OxyThickness(5, 5, 5, 5);


            foreach (var a in new[] { new { pos = AxisPosition.Bottom, name = plot.xAxis, d = .5, r = 210 }, 
                new { pos = AxisPosition.Left, name = plot.yAxis, d = .5, r = 230 } }) {
                model.Axes.Add(new LinearAxis() {
                    Position = a.pos,
                    Title = a.name,
                    TitlePosition=a.d,
                    Minimum = -100,
                    Maximum = 100,                    
                    PositionAtZeroCrossing = true,
                    AxislineColor =borderColor,
                    AxislineStyle = LineStyle.Solid,
                    TickStyle = TickStyle.None,
                    TitleFontSize = 24,
                    AxisTitleDistance= a.r,
                    LabelFormatter = new Func<double, string>(x => "")
                });
            }


            return model;
        }

      

    }
}