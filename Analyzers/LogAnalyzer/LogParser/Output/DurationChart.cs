using LogParser.Models;
using Newtonsoft.Json;
using ParserUtilities;
using ParserUtilities.Utilities.DataTypes;
using ParserUtilities.Utilities.LogFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Output {
	public class DurationChart {

		public static string[] Pallet1 = new string[] { "#7C4338", "#745D7B", "#318382", "#7A9349", "#834548", "#646785", "#358975", "#959241", "#844A5A", "#51728A", "#478E66", "#B19041", "#7F526C", "#3D7B89", "#5F9257", "#CA8C4A" };
		public static string[] Pallet2 = new string[] { "#f23d3d", "#e5b073", "#3df2ce", "#c200f2", "#e57373", "#f2b63d", "#3de6f2", "#e639c3", "#ff2200", "#d9d26c", "#0099e6", "#d9368d", "#d96236", "#cad900", "#73bfe6", "#d90057", "#ffa280", "#aaff00", "#397ee6", "#f27999", "#ff6600", "#a6d96c", "#4073ff", "#d9986c", "#50e639", "#3d00e6", "#e57a00", "#36d98d", "#b56cd9"};

		public static void SaveDurationChart(string output, LogFile<LogLine> file, Func<LogLine,object> colorBy=null) {

			var builder = new StringBuilder();
			builder.Append("<html>");
			AppendStyles(builder);

			///////////////////////////////////
			// Bar Container
			///////////////////////////////////
			builder.Append("<body><div class='bar-container'>");
			var lines = file.GetFilteredLines();
			var start = DateTime.MinValue;
			var end = DateTime.MaxValue;
			if (lines.Any()) {
				start = lines.Min(x=>x.StartTime);
				end = lines.Max(x=>x.EndTime);
			}
			var colorLookup=GetColorLookup(colorBy, lines);
			var totalDuration = end - start;

			var i = 0;
			foreach (var line in lines) {
				var startOffset = (line.StartTime - start).TotalSeconds / totalDuration.TotalSeconds * 100;
				var duration = (line.EndTime - line.StartTime).TotalSeconds;
				var width = duration / totalDuration.TotalSeconds * 100;
				var barColor = "red";
				if (colorBy != null)
					barColor = colorLookup[colorBy(line)];

				var flag = (line.IsFlagged ? "flag" : "");
				var stripe = (line.GroupNumber%2==1? "stripe" : "");

				builder.Append("<div class='row "+flag+" "+ stripe + " group-"+line.GroupNumber+"' id='line_" + i + "' >");
				builder.Append("<div class='bar ' style='height:14px;left:" + startOffset + "%;width:" + width + "%;background-color:" + barColor + ";border-left:1px solid " + barColor + ";' title='" + line.StartTime.ToString("HH:mm:ss") + " [" + ((int)(duration * 1000))/1000.0 + "s] "/*+ line.EndTime.ToString("HH:mm:ss")*/ + "'>");
				builder.Append("<span class='url'>" + line.csUriStem + "</span>");
				builder.Append("</div>");
				builder.Append("</div>");
				i++;
			}

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


			// Info Container			
			var info = new {
				start = start,
				end = end,
				duration = totalDuration,
				allUsersCount = lines.GroupByUsers(false).Count(),
				activeUsersCount = lines.GroupByUsers(true).Count(),
				activeUsers = lines.GroupByUsers(true).Select(x => new { name = x.Key, pages = x.Count() }).OrderByDescending(x=>x.pages)
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


			builder.Append(@"<script src=""https://code.jquery.com/jquery-3.3.1.min.js"" integrity=""sha256-FgpCb/KJQlLNfOu91ta32o/NMZxltwRo8QtmkMRdAu8="" crossorigin=""anonymous""></script>");
			AppendScripts(builder, start, end);
			builder.Append("</body>");
			builder.Append("</html>");
			File.WriteAllText(output, builder.ToString());
		}

		private static DefaultDictionary<object, string> GetColorLookup(Func<LogLine, object> colorBy, IEnumerable<LogLine> lines) {
			var colorLookup = new DefaultDictionary<object, string>(x => "red");
			if (colorBy != null) {
				var pallet = Pallet2;
				var colorKeys = lines.GroupBy(colorBy).Select(x => x.Key).Distinct();
				var i = 0;
				foreach (var c in colorKeys) {
					colorLookup[c] = pallet[i % pallet.Length];
					i++;
				}
			}
			return colorLookup;
		}

		private static void AppendStyles(StringBuilder builder) {
			builder.Append(
			@"<style>
	body{
		margin-top: 0px;
		margin-bottom: 0px;
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
		
	.bar {
	    position: relative;
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
		padding-top:20px;
		padding-bottom:20px;
	}
	.data-container{
		width:49%;
		border-right:1px solid lightgray;
		display:inline-block;
		position: fixed;
		top: 0;
		bottom: 0;
	}

	.row-data{
		height:40%;
		overflow:auto;
		border-bottom:1px solid #333;
	}

	.info{
		height:calc(60% - 1.2vh);
		overflow:auto;
	}

	.row{
		width:100%;		
	}

	.stripe{
		background-color:#f7f7f7;
	}

	.bar:hover .url{
		left: initial;
		padding-right: 104%;
		
		right: 20px;
	}

	.row-hover{
		background-color:#d8d8d8;/*#efefef;*/
	}

	.row-hover.stripe{
		background-color:#d8d8d8;
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
		background-color: #ffecdd;
		border-left:8px dotted red;
		/* animation: blinker 3s linear infinite; */
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
		

		$('.status-text').html(logFileVarName+'.FilterRange('+Math.floor(Math.min(time,dragStartTime)- offset)+'.ToDateTime(DateTimeKind.Local),'+Math.floor(Math.max(time,dragStartTime)- offset)+'.ToDateTime(DateTimeKind.Local));');
	}
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
