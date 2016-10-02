﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.IO;
using System.Net.Mime;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Users;
using RadialReview.Accessors;
using PdfSharp;

namespace RadialReview.Controllers {
	public class PdfController : BaseController {
		// GET: Svg
		public ActionResult Index() {
			return View();
		}

		//public class AngularAccountabilityChartOverride : AngularAccountabilityChart {
		//	public new AngularAccountabilityNodeOverride Root { get; set; }
		//}

		[HttpPost, ValidateInput(false)]
		[Access(AccessLevel.Any)]
		public ActionResult AC(PdfAccessor.AccNodeJs root, bool fit = false,PageSize ps = PageSize.Letter,double? pw=null, double? ph =null) {
			//using (var stream = new MemoryStream()) {
			//	var html = new HtmlDocument();
			//	var config = new GlobalConfig();
			//	html.LoadHtml(svg);
			//	html.OptionOutputAsXml = true;
			//	html.Save(stream);


			//	stream.Seek(0, SeekOrigin.Begin);
			//	XmlDocument xml = new XmlDocument();
			//	xml.Load(stream);
			//	var res = SvgDocument.Open(xml);
			//	var sp = new SynchronizedPechkin(config);
			//	byte[] pdfBuf = sp.Convert(svg);

			//	return File(pdfBuf, MediaTypeNames.Application.Pdf);
			//	//res.Draw(
			//}
			if (pw == null || ph == null) {
				var s = PageSizeConverter.ToSize(ps);
				pw = Math.Max(s.Width, s.Height) / 72.0;
				ph = Math.Min(s.Width, s.Height) / 72.0;
				
			}

			var pdf = PdfAccessor.GenerateAccountabilityChart(root,pw.Value,ph.Value,restrictSize: fit);


			return Pdf(pdf,"Accountability Chart",false);
			//return null;            
			//using (var stream = new MemoryStream()) {//	var html = new HtmlDocument();
			//	html.LoadHtml(svg);
			//	html.OptionOutputAsXml = true;
			//	html.Save(stream);
			//	stream.Seek(0, SeekOrigin.Begin);
			//	XmlDocument xml = new XmlDocument();
			//	xml.Load(stream);
			//	var res = SvgDocument.Open(xml);
			//	res.Draw(
			//	return null;
			//}


		}
	}
}