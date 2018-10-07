using LogParser.Models;
using Newtonsoft.Json;
using ParserUtilities;
using ParserUtilities.Utilities.Colors;
using ParserUtilities.Utilities.DataTypes;
using ParserUtilities.Utilities.LogFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LogParser.Downloaders.AwsCloudWatchDownloader;
using LogParser.Downloaders;
using LogParser.Properties;

namespace LogParser.Output {
	public class DurationChart {

		//public static string[] Pallet1 = new string[] { "#7C4338", "#745D7B", "#318382", "#7A9349", "#834548", "#646785", "#358975", "#959241", "#844A5A", "#51728A", "#478E66", "#B19041", "#7F526C", "#3D7B89", "#5F9257", "#CA8C4A" };
		//public static string[] Pallet2 = new string[] { "#f23d3d", "#e5b073", "#3df2ce", "#c200f2", "#e57373", "#f2b63d", "#3de6f2", "#e639c3", "#ff2200", "#d9d26c", "#0099e6", "#d9368d", "#d96236", "#cad900", "#73bfe6", "#d90057", "#ffa280", "#aaff00", "#397ee6", "#f27999", "#ff6600", "#a6d96c", "#4073ff", "#d9986c", "#50e639", "#3d00e6", "#e57a00", "#36d98d", "#b56cd9"};
		//public static string[] Scale_Pallet1 = new string[] { "#80423C", "#8A474C", "#904D5D", "#93566E", "#926080", "#8D6C91", "#8478A0", "#7785AC", "#6692B4", "#549EB8", "#43A9B7", "#39B4B2", "#3DBEAA", "#4FC79E", "#67CF91", "#83D582", "#A2DA74", "#C1DE69", "#E2E062" };



		public static void SaveDurationChart(string output, LogFile<LogLine> file, Func<LogLine, object> colorBy = null, IPallet pallet = null, IEnumerable<DataChartModel> charts = null) {

			var builder = new StringBuilder();


			

			var lines = file.GetFilteredLines();
			var start = DateTime.MinValue;
			var end = DateTime.MaxValue;
			if (lines.Any()) {
				start = lines.Min(x => x.StartTime);
				end = lines.Max(x => x.EndTime);
			}
			var colorLookup = GetColorLookup(colorBy, lines, pallet);
			var totalDuration = end - start;

			var secondAsPercentage = TimeSpan.FromSeconds(1).TotalSeconds / totalDuration.TotalSeconds * 100.0;
			builder.Append("<html>");
			AppendStyles(builder, secondAsPercentage);


            ///////////////////////////////////
            // Bar Container
            ///////////////////////////////////
            builder.Append("<body><div class='bar-container'>");
			var legend = AppendCharts(builder, start, end, charts);
			builder.Append("<div>");
			var i = 0;
			foreach (var line in lines) {
				var startOffset = (line.StartTime - start).TotalSeconds / totalDuration.TotalSeconds * 100;
				var duration = (line.EndTime - line.StartTime).TotalSeconds;
				var width = duration / totalDuration.TotalSeconds * 100;
				var barColor = "red";
				if (colorBy != null)
					barColor = colorLookup[colorBy(line)];

				var flag = "";
				var flagText = "";
				if (line.Flag != FlagType.None) {
					flag += "flag ";
					foreach (var f in line.Flag.GetIndividualFlags()) {
						flag += "flag-" + f + " ";
						flagText += f + " ";
					}
				}

				var urlLoc = "right";
				if (startOffset > 50)
					urlLoc = "left";
				if (width > 50)
					urlLoc = "center";

				var stripe = (line.GroupNumber % 2 == 1 ? "stripe" : "");

				builder.Append("<div class='row " + flag + stripe + " group-" + line.GroupNumber + "' id='line_" + i + "' data-guid='" + line.Guid + "'>");
				builder.Append("<span class='click-flag-text' title='" + flagText + "'>(flag)</span>");
				builder.Append("<div class='bar ' style='left:" + startOffset + "%;width:" + width + "%;background-color:" + barColor + ";border-left:1px solid " + barColor + ";' title='" + line.StartTime.ToString("HH:mm:ss") + " [" + ((int)(duration * 1000)) / 1000.0 + "s] "/*+ line.EndTime.ToString("HH:mm:ss")*/ + "'>");
				builder.Append("<span class='url url-" + urlLoc + "'>" + line.csUriStem + "</span>");
				builder.Append("</div>");

				if (line.Flag != FlagType.None) {
					builder.Append("<div class='flag-icon-container'>");
					foreach (var t in EnumExtensions.GetAllFlags<FlagType>()) {
						builder.Append("<span class='flag-icon flag-icon-" + t + "' title='" + t + "'>(flag)(" + t + ")</span>");
					}
					builder.Append("</div>");
				}

				builder.Append("</div>");
				i++;
			}
			builder.Append("</div>");


            ///////////////////////////////////
            // Data Container
            ///////////////////////////////////
            builder.Append("</div><div class='data-container'>");
			builder.Append("<div class='row-data json'>");
			i = 0;
			foreach (var line in lines) {
				builder.Append("<div class='hidden line-data fixedFont copyable1' id='line_" + i + "_data'>" + JsonConvert.SerializeObject(line, Formatting.Indented) + "</div>");
				i++;
			}
			builder.Append("</div>");


			builder.Append(legend);


			// Info Container			
			var info = new {
				start = start,
				end = end,
				duration = totalDuration,
				allUsersCount = lines.GroupByUsers(false).Count(),
				activeUsersCount = lines.GroupByUsers(true).Count(),
				activeUsers = lines.GroupByUsers(true).Select(x => new { name = x.Key, pages = x.Count() }).OrderByDescending(x => x.pages)
			};
			builder.Append("<div class='info fixedFont json copyable1'>" + JsonConvert.SerializeObject(info, Formatting.Indented) + "</div>");


			// Status Container			
			builder.Append("<div class='status fixedFont'><span class='status-text'>&nbsp;</span></div>");


			builder.Append("</div>");
			
			///////////////////////////////////
			// Screen Overlays
			///////////////////////////////////
			builder.Append("<div class='mousebar fixedFont'><span></span></div>");
			builder.Append("<div class='dragbar hidden fixedFont'><span></span></div>");


            ////////////////////////////
            //Slices
            ////////////////////////////
            AppendSlices(builder, file.GetSlices(), start, end);

            //builder.Append(@"<script src=""https://code.jquery.com/jquery-3.3.1.min.js"" integrity=""sha256-FgpCb/KJQlLNfOu91ta32o/NMZxltwRo8QtmkMRdAu8="" crossorigin=""anonymous""></script>");
            builder.Append("<script>" + Resources.Jquery + "</script>");
            AppendScripts(builder, start, end);
			builder.Append("</body>");
			builder.Append("</html>");
			File.WriteAllText(output, builder.ToString());
		}

        private static void AppendSlices(StringBuilder builder, List<TimeSlice> slices,DateTime start,DateTime end) {
            builder.Append("<div class='slice-container'>");
            var totalDuration = end - start;
            foreach(var s in slices) {
                var startOffset = (s.Range.Start - start).TotalSeconds / totalDuration.TotalSeconds * 100;
                var width = (s.Range.End - s.Range.Start).TotalSeconds / totalDuration.TotalSeconds * 100;
                var nonZero = "non-zero";
                if (width == 0)
                    nonZero = "";

                builder.Append("<div class='slice "+nonZero+"' style='left:" + startOffset + "%;width:"+width+ "%'><span>"+s.Name+"</span></div>");
            }
            builder.Append("</div>");
        }

        private static StringBuilder AppendCharts(StringBuilder chartBuilder, DateTime start, DateTime end, IEnumerable<DataChartModel> charts) {
			var legendBuider = new StringBuilder();
			if (charts != null) {

				var pallet = Pallets.Stratified;

				legendBuider.Append("<form>");
				var i = 0;
				foreach (var c in charts) {
					var color = pallet.NextColor();
					
					if (c.Datapoints.Any()) {
                        var key = "" + (i + 1);
                        if (i >= 9) {
                            var keys = new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
                            key = keys[i - 9];
                        }
                        var mc = new MetricChart(c, start, end, "50%", "10%");
                        MetricChart.CreateChart("50%","10%",chartBuilder, start, end, legendBuider, key, c, color, i);

                        
                        i += 1;
                    }
                }
				legendBuider.Append("</form>");
			}
			return legendBuider;
		}

       

        private static DefaultDictionary<object, string> GetColorLookup(Func<LogLine, object> colorBy, IEnumerable<LogLine> lines, IPallet pallet) {
			var colorLookup = new DefaultDictionary<object, string>(x => "red");
			if (colorBy != null) {
				pallet = pallet ?? new StratifiedPallet();
				var colorKeys = lines.GroupBy(colorBy).Select(x => x.Key).Distinct().OrderBy(x => x).ToArray();
				var i = 0.0;
				var rangeable = Rangeable.GetRangeable(colorKeys);

				foreach (var c in colorKeys) {
					if (rangeable != Rangeable.Invalid) {
						colorLookup[c] = pallet.GetColor(rangeable.GetPercentage(c));
					} else {
						colorLookup[c] = pallet.GetColor(i / (colorKeys.Length - 1));
					}
					i++;
				}
			}
			return colorLookup;
		}

		private static void AppendStyles(StringBuilder builder, double secondAsPercentage) {
			builder.Append(
            @"<style>
	body{
		margin-top: 0px;
		margin-bottom: 0px;
	}

	svg{
		position:fixed;
		bottom:0px;
		z-index:1;
		pointer-events:none;
	}

	polyline{
		fill: none;
		stroke-width: 2;
	}

	.legend-dot{
		width: 10px;
		height: 11px;
		border-radius: 5px;
		display: inline-block;
		top: -3px;
		position: relative;
		font-size: 10px;
		padding-left: 1px;
        text-align:center;
		color: white;
	}

	input[type='checkbox']{
	}

    form > div{
        display: inline-block;
        width: 50%;
    }

	.chart-max{
		opacity: 0.7;
		z-index: 1;
		font-size: 69%;
		margin-left: 10px;
	}

    .chart svg {
        border-top: 1px dotted gray;
    }

	.fixedFont{
		font-size:1.2vh;
	}

	.noselect{
	  -moz-user-select: none;
	  -khtml-user-select: none;
	  -webkit-user-select: none;
	  user-select: none;
	}
	.url {
		display: inline-block;
		position: absolute;
		white-space: nowrap;
		font-size:12px;
		left: 102%;
		pointer-events:none;
	}
	.bar-container{
		width:50%;
		border-right:1px solid #333;
		display:inline-block;
		cursor:crosshair;
		padding-top:30px;
		padding-bottom:20px;
	}
	.data-container{
		width:calc(50% - 8px);
		border-right:1px solid lightgray;
		display:inline-block;
		position: fixed;
		top: 0;
		bottom: 0;
		padding-left:8px;
	}
	.info{
		height:calc(60% - 1.2vh);
		overflow:auto;
	}


	.row-data{
		height:40%;
		overflow:auto;
		border-bottom:1px solid #333;
	}

	.row{
		width:calc(100%);
		position:relative;
		padding:1px;
	}

	.row-hover{
		background-color:#d8d8d8;/*#efefef;*/
	}

	.row-hover.stripe{
		background-color:#d8d8d8;
	}

		
	.bar {
	    position: relative;
		margin:1px;
		height:12px;
	}

	.full-res .bar:after,
	.bar:hover:after{
		content: ' ';
		position: absolute;
		right: -" + secondAsPercentage / 2 + @"vw;
		left: " + secondAsPercentage / 2 + @"vw;
		height: 2px;
		background-color: red;
		top: 4px;
	}

	.full-res .bar:before,
	.bar:hover:before{
		content: ' ';
		position: absolute;
		right: -" + secondAsPercentage / 2 + @"vw;
		left: -1px;
		height: 6px;
		background-color: #ff000088;
		top: 2px;
		border-radius:3px;
	}


	
	.full-res .bar:hover{
        margin-left:" + secondAsPercentage / 2 + @"vw;        
    }	
	.full-res .bar:hover:before{
		left: " + secondAsPercentage / 2 + @"vw;
		right: 0;    
    }    
	.full-res .bar:hover:after{        
		right: " + secondAsPercentage / 2 + @"vw;
		left: -" + secondAsPercentage / 2 + @"vw;
    }

	.url{		
		text-shadow:1px 0 1px white, 0 1px 1px white, 1px 1px 1px white, 0 0 1px white, 1px 0 1px white, 0 1px 1px white, 1px 1px 1px white, 0 0 1px white;
	}

	.url-left{
		left: initial;
		padding-right: 104%;		
		right: 20px;
	}
	.url-center{
		left: 6px;
	}

	.stripe{
		background-color:#f7f7f7;
	}
	

	.flag.row-hover{
		background-color:#f9c7a5;
	}

	.hidden{
		display:none;
	}
	
	.json{
		white-space: pre;
		background-color: #ffffffe3;
		/* width: 50%; */
		padding: 5px 10px;
	}

	.mousebar{
		position:fixed;
		left:0;
		width:0px;
		top:0;
		bottom:0;
		border-right:1px dotted #333333;
		text-align:right;
		color:#333;
		pointer-events:none;
		white-space:nowrap;		
	}
	.mousebar span{
		background-color:white;
	}

	.dragbar{
		position:fixed;
		top:0;
		bottom:0;
		border-left:1px dotted #333333;
		border-right:1px dotted #333333;
		background-color:#33333333;
		padding-top:1.4vh;
		text-align:center;
		pointer-events:none;
	}

	.status{
		position: fixed;
		left: 50%;
		right: 0;
		bottom: 0;
		padding: 2px;
		font-family: monospace;
		background-color: #333;
		color: white;
	}

	.status-text{
		cursor:pointer;
	}

	.copyable{
		cursor:copy;
	}	

	.flag .bar{
	}
	
	.flag {    
		background-color: #fbcbff;
	}

	.flag-UserFlag{
		background-color:#e1baf5;
	}

	.flag-UnusuallyLongRequest{
		background-color: #fdf6cb;
	}


	.flag-PotentialCauses{
		background-color: #bbf9e1;
	}

	.flag-HasError{
		background-color: #fbcbff;
	}

	.flag-LikelyCause{
		background-color: #ffc9ce !important;
		/*color: white !important;*/
	}

		.flag-LikelyCause .bar{
			/*border-left:1px solid white!important;
			border-right:1px solid white!important;
			background-color:white !important;*/
		}

	.flag-ByGuid{
		background-color: lightblue;
	}
	.flag-Fixed{
		background-color: #c5ffbe;
	}

	.click-flag{
		background-color: #ecffdd;    
		border-right: 7px Solid limegreen;
		border-radius: 8px;
	}
	.click-flag-text{
		display:none;
		width: 0px;
		height: 0px;
		font-size: 70%;
		color: #333333cc;		
	}
	/*.flag .click-flag-text,*/
	.click-flag .click-flag-text{
		display: inline-block;   
		z-index: 1;
		position: relative;
	}
	
	.flag-icon-container{
		height:14px;
		display:inline-block;
		position: absolute;
		right: 0px;
		top: 0px;
		text-align:right;
	}	
	.flag-icon{
		display: none;
		height: 10px;
		width: 10px;
		margin: 1px;
		/*float: right;*/
		border-radius:7px;
		cursor:help;
		opacity:.9;
		border:1px solid white;
	}

	.flag-PotentialCauses .flag-icon-PotentialCauses,
	.flag-UnusuallyLongRequest .flag-icon-UnusuallyLongRequest,
	.flag-UserFlag .flag-icon-UserFlag,
	.flag-LikelyCause .flag-icon-LikelyCause,
	.flag-ByGuid .flag-icon-ByGuid,
	.flag-HasError .flag-icon-HasError,
	.flag-Fixed .flag-icon-Fixed
	{
		display:inline-block;
		color:#ffffff11;
		font-size: 12px;
		line-height: 4px;
		text-align:right;
		/*direction: rtl;*/
	}

	.flag-icon-UserFlag{
		background-color:#bb23bd;
	}
	.flag-icon-UnusuallyLongRequest{
		background-color:#FFC107;
	}
	.flag-icon-PotentialCauses{
		background-color:#037a88;
	}
	.flag-icon-LikelyCause{
		background-color:red;
	}

	.flag-icon-ByGuid{
		background-color:#7676e0;
	}
	.flag-icon-HasError{
		background-color:deeppink;
	}

	.flag-icon-Fixed{
		background-color:lime;
	}
    .slice-container{
        pointer-events:none;    
        left:0px;
        width:50%; 
        position:fixed;
        top:0;
        bottom:0;
    }

    .slice-container .slice{
        position:relative;       
        border-left:1px dashed blue;
        background-color:#0000ff09;
        height: 100%;   
    }
    .slice-container .slice.non-zero{
        border-right:1px dashed blue;
    }
    .slice-container .slice span{
        position:absolute;
        bottom:0px;
        left:0px;
        color:blue;
        font-size:70%;
        background-color: #ffffff99;
    }

    .full-res .slice-container{
        display:none;
    }

	@keyframes blinker {
	  50% {
		background-color: #f3c99f;
	  }
	}

</style>");
		}

		private static void AppendScripts(StringBuilder builder, DateTime start, DateTime end) {
			builder.Append("<script>");
			builder.Append("var start=" + start.ToUniversalTime().ToJsMs() + ";var scale = " + (end.ToJsMs() - start.ToJsMs()) + ";");
			builder.Append(@"
var logFileVarName='logFile';

//Hovering over a row
$('.row').hover(function(){
	$('.line-data').hide();
	$('.row-hover').removeClass('row-hover');
	var id = $(this).attr('id');
	$('#'+id+'_data').show();
	$(this).addClass('row-hover');
},function(){
	//var id = $(this).attr('id');
	//$('#'+id+'_data').hide();
});

//Show vertical line and timestamp
$('body').mousemove(function(e){
	var time = start + e.pageX/(window.innerWidth/2) * scale;
	var date = new Date(time);

	$('.mousebar').css('width',e.pageX-1).find('span').html(date.toLocaleString()+' (local)');	

	if (dragging){
		$('.dragbar').css('left',Math.min(e.pageX,dragStartX));	
		$('.dragbar').css('width',Math.abs(e.pageX-dragStartX)-1);
		$('.dragbar span').html(Math.abs(Math.floor(dragStartTime-time)/1000)+'s');
		
		var now = new Date();
		var t = now.getTime();
		var offset = now.getTimezoneOffset();
		offset = offset * 60000;
		

		$('.status-text').html(logFileVarName+'.FilterRange(new TimeRange('+Math.floor(Math.min(time,dragStartTime)- offset)+'.ToDateTime(),'+Math.floor(Math.max(time,dragStartTime)- offset)+'.ToDateTime(),DateTimeKind.Local));');
	}
});

//Flag right clicked row
$('.row').contextmenu(function(e){

		$(this).toggleClass('click-flag');
		if ($(this).hasClass('click-flag')){
			var status = $(this).data('guid');
			$('.status-text').html(logFileVarName+'.Flag(x=>x.Guid==""'+status+'"",FlagType.ByGuid);');
		}

		e.preventDefault();		
});

$('[name=\'chart\']').change(function(){
	$('.chart').addClass('hidden');
	$('[name=\'chart\']:checked').each(function(x){
		var chart = $(this).attr('value');
		$('.chart.chart-'+chart).removeClass('hidden');
	});
});

$('.status').click(function(){
	var text = $('.status-text').text();
	if (text.trim()!=''){
		copyToClipboard(text);
		$(this).flash();
	}
});
$('.copyable').click(function(){
	var text = $(this).text();
	if (text.trim()!=''){
		copyToClipboard(text);
	}
});

//Clear status
$(document).keyup(function(e){
	if (e.keyCode == 27) {
		$('.dragbar').hide();
		$('.status-text').html('&nbsp;');
		$('.chart').addClass('hidden');
		$('[name=\'chart\']:checked').attr('checked',false);
		$('[name=\'chart\']:checked').prop('checked',false);
    }

    var keyMaps = {
        81 : 9,
        87 : 10,
        69 : 11,
        82 : 12,
        84 : 13,
        89 : 14,
        85 : 15,
        73 : 16,
        79 : 17,
        80 : 18,
    };       

	//show charts;
	if(e.keyCode >=49 && e.keyCode<=57 || e.keyCode >=97 && e.keyCode<=105 || e.keyCode in keyMaps){
		var id = e.keyCode-49;
		if(e.keyCode >=97 && e.keyCode<=105)
			id = e.keyCode-97;
        if (e.keyCode in keyMaps)
            id = keyMaps[e.keyCode];
		var selected = $('[name=\'chart\'][data-chart-num='+id+']');
		var shouldSelect = selected.is(':checked');
		selected.attr('checked',!shouldSelect);
		selected.prop('checked',!shouldSelect);
		selected.trigger('change');
	}

	if(e.keyCode==16){
		$('body').removeClass('full-res');
	}
});
$(document).keydown(function(e){
	if(e.keyCode==16){
		$('body').addClass('full-res');
	}
});


//Select a range
var dragStartX = 0;
var dragging = false;
var dragStartTime= 0;
$('.bar-container').mousedown(function(e) {
	if (event.which==1){
		$('body').addClass('noselect');
		dragStartTime = start + e.pageX/(window.innerWidth/2) * scale;
		dragStartX =e.pageX;
		$('.dragbar').show();
		dragging = true;
	}
});
$(document).mouseup(function(e) {
	if (event.which==1){
		$('body').removeClass('noselect');
		dragging = false;
		//$('.dragbar').hide();
	}
});

//Copy to clipboard
function copyToClipboard(str){
  const el = document.createElement('textarea');
  el.value = str;
  el.setAttribute('readonly', '');
  el.style.position = 'absolute';
  el.style.left = '-9999px';
  document.body.appendChild(el);
  el.select();
  document.execCommand('copy');
  document.body.removeChild(el);
};

</script>");
		}
	}
}
