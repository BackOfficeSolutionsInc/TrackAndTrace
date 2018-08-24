using System;
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
using PdfSharp.Pdf;
using RadialReview.Accessors.PDF;
using RadialReview.Accessors.PDF.JS;
using RadialReview.Utilities;
using RadialReview.Utilities.Pdf;
using MigraDoc.DocumentObjectModel;
using PdfSharp.Drawing;

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
		[Access(AccessLevel.UserOrganization)]
		public ActionResult AC(PdfAccessor.AccNodeJs root = null, bool fit = false, bool department = false, PageSize ps = PageSize.Letter, double? pw = null, double? ph = null, long? selected = null, bool? compact = null, bool userCheck = false) {
			
			if (pw == null || ph == null) {
				var s = PageSizeConverter.ToSize(ps);
				pw = Math.Max(s.Width, s.Height) /*/ 72.0*/;
				ph = Math.Min(s.Width, s.Height) /*/ 72.0*/;
			}

            var width = XUnit.FromInch(pw.Value);
            var height = XUnit.FromInch(ph.Value);

            var properties = new AccountabilityChartPDF.AccountabilityChartSettings(GetUser().Organization.Settings);

			PdfDocument pdf=null;
			var merger = new DocumentMerger();
			if (root != null && (root.children!=null || root._children!=null)) {

				var tree = AccountabilityAccessor.GetTree(GetUser(), GetUser().Organization.AccountabilityChartId, expandAll: true);
				if (!department)
					pdf = AccountabilityChartPDF.GenerateAccountabilityChart(tree.Root, width, height, properties, restrictSize: fit, selectedNode: root.NodeId);
				else {
					if (selected == null) {
						var nodes = new List<AngularAccountabilityNode>();
						//Add nodes from the tree.
						if (!userCheck) {
							tree.Dive(x => {
								if (x.User != null)
									nodes.Add(x);
							});
						} else {
							tree.Dive(x => {
								nodes.Add(x);
							});
						}

						nodes = nodes.Where(t => root.NodeId.Contains(t.Id)).ToList();
						var selectedNode = diveSeletedNode(nodes, root.NodeId);
						merger.AddDocs(AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(selectedNode, width, height, properties, restrictSize: fit).Select(x=>x.Document));
						pdf = merger.Flatten("", false, false);

					} else {
						var nodes = new List<AngularAccountabilityNode>();
						var topNodes = tree.Root.GetDirectChildren();

						//Add nodes from the tree.
						tree.Dive(x => {
							if (x.Id == selected)
								nodes.Add(x);
						});

						if (nodes.Any()) {
							foreach (var item in nodes.FirstOrDefault().children) {
								nodes.Add(item);
							}
						}

						nodes = nodes.Where(t => root.NodeId.Contains(t.Id)).ToList();
						var selectedNode = diveSeletedNode(nodes, root.NodeId);
						merger.AddDocs(AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(selectedNode, width, height, properties, restrictSize: fit).Select(x => x.Document));
						pdf = merger.Flatten("", false, false);
					}
				}
			} else {
				var settings = new Tree.TreeSettings() {
					compact = compact ?? false
				};

				if (selected != null) {
					var tree = AccountabilityAccessor.GetTree(GetUser(), GetUser().Organization.AccountabilityChartId, expandAll: true);
					if (!department) {
					AngularAccountabilityNode node = null;
					tree.Dive(x => {
						if (x.Id == selected)
							node = x;
					});
					pdf = AccountabilityChartPDF.GenerateAccountabilityChart(node, width, height, properties, restrictSize: fit,settings: settings);
					} else {

						var nodes = new List<AngularAccountabilityNode>();
						var topNodes = tree.Root.GetDirectChildren(); //tree.Root.children.Where(t => t.Id == selected).FirstOrDefault().GetDirectChildren();
																	  //var tree1 = tree.Root.children.Where(t => t.Id == selected).FirstOrDefault();


						//Add nodes from the tree.
						tree.Dive(x => {
							if (x.Id == selected)
								nodes.Add(x);
						});

						if (nodes.Any()) {
							foreach (var item in nodes.FirstOrDefault().children) {
								nodes.Add(item);
							}
						}

						//Setup if has parents
						//foreach (var n in nodes)
						//{
						//    n._hasParent = topNodes.All(x => x.Id != n.Id);
						//}


						merger.AddDocs(AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(nodes, width, height, properties, restrictSize: fit, settings: settings).Select(x => x.Document));
						pdf = merger.Flatten("", false, false);
					}
				}

				if (selected == null || pdf == null) {
					var tree = AccountabilityAccessor.GetTree(GetUser(), GetUser().Organization.AccountabilityChartId, expandAll: true);
					if (!department) {

						pdf = AccountabilityChartPDF.GenerateAccountabilityChart(tree.Root, width, height, properties, restrictSize: fit, settings: settings);
					} else {
						var nodes = new List<AngularAccountabilityNode>();
						var topNodes = tree.Root.GetDirectChildren();

						//Add nodes from the tree.
						if (!userCheck) {
							tree.Dive(x => {
								if (x.User != null)
									nodes.Add(x);
							});
						} else {
							tree.Dive(x => {
								nodes.Add(x);
							});
						}

						//Setup if has parents
						foreach (var n in nodes) {
							n._hasParent = topNodes.All(x => x.Id != n.Id);
						}


						merger.AddDocs(AccountabilityChartPDF.GenerateAccountabilityChartSingleLevels(nodes, width, height, properties, restrictSize: fit, settings: settings).Select(x => x.Document));
						pdf = merger.Flatten("", false, false);
					}
				}

			}


			return Pdf(pdf, "Accountability Chart", false);
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

		private List<AngularAccountabilityNode> diveSeletedNode(List<AngularAccountabilityNode> root, List<long> selectedIds) {
			List<AngularAccountabilityNode> list = new List<AngularAccountabilityNode>();
			foreach (var item in root) {
				item.children = item.children.Where(t => selectedIds.Contains(t.Id));
				item.children = diveSeletedNode(item.children.ToList(), selectedIds);
				list.Add(item);
			}
			return list;
		}
	}
}
