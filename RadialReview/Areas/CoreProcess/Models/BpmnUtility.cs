using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace RadialReview.Areas.CoreProcess.Models {
	public class BpmnUtility {
		private static XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
		private static XNamespace camunda = "http://camunda.org/schema/1.0/bpmn";
		public static string GetMemberNames(UserOrganizationModel caller, long[] memberIds) {
			ResponsibilitiesAccessor respAccessor = new ResponsibilitiesAccessor();
			string memberName = string.Empty;
			if (memberIds != null) {
				memberName = String.Join(",", memberIds.Select(x => respAccessor.GetResponsibilityGroup(caller, x).GetName()));
			}
			//if (getMemberIds != null) {
			//	if (getMemberIds.Any()) {
			//		foreach (var item in getMemberIds) {
			//			var getMemberName = respAccessor.GetResponsibilityGroup(caller, item).GetName();
			//			if (!string.IsNullOrEmpty(getMemberName)) {
			//				if (string.IsNullOrEmpty(memberName))
			//					memberName = getMemberName;
			//				else
			//					memberName += ", " + getMemberName;
			//			}
			//		}
			//	}
			//}
			return memberName;
		}

		public static string GetMemberNamesFromString(UserOrganizationModel caller, string candidateGroupName) {
			long[] getMemberIds = null;
			getMemberIds = GetParseMemberId(candidateGroupName);
			string memberName = GetMemberNames(caller, getMemberIds);
			return memberName;
		}

		public static string GetMemberNamesFromRGM(UserOrganizationModel caller, long[] memberIds) {
			string memberName = GetMemberNames(caller, memberIds);
			return memberName;
		}





		public static long[] GetParseMemberId(string memberIds) {
			List<long> idList = new List<long>();
			if (string.IsNullOrEmpty(memberIds)) {
				return idList.ToArray();
			}

			var getMemberIds = memberIds.Split(',');
			foreach (var item in getMemberIds) {
				var getItem = item.Split('_');
				var id = 0L;
				if (getItem.Length > 1 && long.TryParse(getItem[1], out id)) {
					idList.Add(id);
				}
			}
			return idList.ToArray();
		}

		public static async Task<XDocument> GetBpmnFileXmlDoc(string keyName) {
			var getfileStream = await GetFileFromServer(keyName);
			getfileStream.Seek(0, SeekOrigin.Begin);
			return XDocument.Load(getfileStream);
		}

		public static Stream DetachNode(Stream stream, string deletedNodeId) {
			MemoryStream fileStream = new MemoryStream();

			stream.Seek(0, SeekOrigin.Begin);

			XDocument xmlDocument = XDocument.Load(stream);

			//get node
			var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == deletedNodeId).ToList();

			var deleteNode = current.FirstOrDefault();
			string attrId = deleteNode.Attribute("id").Value;

			string source = string.Empty;
			string target = string.Empty;
			var elements = xmlDocument.Root.Element(bpmn + "process").Elements();

			int targetCounter = 0;
			int sourceCounter = 0;
			foreach (var item in elements.ToList()) {
				if (item.Attribute("targetRef") != null) {
					if (item.Attribute("targetRef").Value == attrId) {
						source = item.Attribute("sourceRef").Value;
						item.Remove();
						targetCounter++;
					}
				}

				if (item.Attribute("sourceRef") != null) {
					if (item.Attribute("sourceRef").Value == attrId) {
						target = item.Attribute("targetRef").Value;
						item.Remove();
						sourceCounter++;
					}
				}
			}

			if (targetCounter != 1) {
				throw new Exception("Could not detach node. As targetRef occurs more than once.");
			}

			if (sourceCounter != 1) {
				throw new Exception("Could not detach node. As sourceRef occurs more than once.");
			}

			deleteNode.Remove();

			//get target element
			var getTargetElement = BpmnUtility.FindElementByAttribute(elements, "id", target);
			//elements.Where(x => (x.Attribute("id") != null ? x.Attribute("id").Value : "") == target).FirstOrDefault();

			//apppend element
			elements.Where(m => m.Attribute("id").Value == getTargetElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
					 AppendSequenceFlow(source, target));

			xmlDocument.Save(fileStream);
			fileStream.Seek(0, SeekOrigin.Begin);
			fileStream.Position = 0;

			return fileStream;
		}

		public static XElement AppendSequenceFlow(string source, string target) {
			return new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + GenerateGuid()), new XAttribute("sourceRef", source), new XAttribute("targetRef", target));
		}

		public static Stream InsertNode(Stream stream, int oldOrder, int newOrder, string oldOrderId, string newOrderId, string name, string candidateGroups) {

			MemoryStream fileStream = new MemoryStream();
			stream.Seek(0, SeekOrigin.Begin);

			try {
				string deletenodeId = oldOrderId;
				string afterNode = newOrderId;

				//file initilaize

				XDocument xmlDocument = XDocument.Load(stream);

				//get node
				var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == afterNode).ToList();
				var getBeforeNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == afterNode).FirstOrDefault();

				var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();

				if (newOrder > oldOrder) {

					var getSequenceNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("sourceRef") != null ? x.Attribute("sourceRef").Value : "") == afterNode).FirstOrDefault();

					getAllElement.Where(t => t.Attribute("id").Value == getSequenceNode.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
						   new XElement(bpmn + "userTask", new XAttribute("id", deletenodeId), new XAttribute("name", name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
						  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")), new XAttribute("sourceRef", deletenodeId), new XAttribute("targetRef", getSequenceNode.Attribute("targetRef").Value))
					   );

					getAllElement.Where(t => t.Attribute("id").Value == getSequenceNode.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", deletenodeId);
				} else {
					getAllElement.Where(t => t.Attribute("id").Value == getBeforeNode.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
							new XElement(bpmn + "userTask", new XAttribute("id", deletenodeId), new XAttribute("name", name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
						   new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")), new XAttribute("sourceRef", deletenodeId), new XAttribute("targetRef", afterNode))
						);

					//update target element attr

					getAllElement.Where(t => t.Attribute("id").Value == getBeforeNode.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", deletenodeId);
				}



				xmlDocument.Save(fileStream);
				fileStream.Seek(0, SeekOrigin.Begin);
				fileStream.Position = 0;
			} catch (Exception ex) {
				throw ex;
			}

			return fileStream;
		}

		public static void GetNodeDetail(Stream stream, int oldOrder, int newOrder, out string oldOrderId, out string newOrderId, out string name, out string candidateGroups) {
			stream.Seek(0, SeekOrigin.Begin);
			XDocument xmlDocument = XDocument.Load(stream);

			//get user task
			var getAlltask = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask").ToList();

			string deletenodeid = getAlltask[oldOrder].Attribute("id").Value;
			string afterNode = getAlltask[newOrder].Attribute("id").Value;

			//get name and description of deleted node
			name = (getAlltask[oldOrder].Attribute("name") != null ? getAlltask[oldOrder].Attribute("name").Value : "");
			//description = (getAlltask[oldOrder].Attribute("description") != null ? getAlltask[oldOrder].Attribute("description").Value : "");
			candidateGroups = (getAlltask[oldOrder].Attribute(camunda + "candidateGroups") != null ? getAlltask[oldOrder].Attribute(camunda + "candidateGroups").Value : "");

			oldOrderId = deletenodeid;
			newOrderId = afterNode;
		}

		public static async Task<Stream> GetFileFromServer(string keyName) {
			Stream stream = new MemoryStream();
			try {
				if (Config.ShouldDeploy()) {
					stream = await GetFileFromAmazon(keyName);
				} else {
					stream = await Task.Run(() => GetFileFromLocal(keyName));
				}
			} catch (Exception ex) {
				throw ex;
			}
			return stream;
		}

		private static async Task<Stream> GetFileFromAmazon(string keyName) {
			Stream stream = new MemoryStream();
			IAmazonS3 client;
			using (client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1)) {
				GetObjectRequest request = new GetObjectRequest {
					BucketName = "Radial",
					Key = keyName
				};

				using (GetObjectResponse response = await client.GetObjectAsync(request)) {
					using (var ms = new MemoryStream()) {
						response.ResponseStream.CopyTo(ms);
						ms.Seek(0, SeekOrigin.Begin);
						ms.CopyTo(stream);
						ms.Seek(0, SeekOrigin.Begin);
						stream.Seek(0, SeekOrigin.Begin);
					}
				}

				return stream;
			}
		}

		private static Stream GetFileFromLocal(string keyName) {
			Stream stream = new MemoryStream();
			string dir = GetBpmnFilesLocalPath();
			string fileName = keyName.Split('/')[1];
			var fullPath = Path.Combine(dir, fileName);

			try {
				if (System.IO.File.Exists(fullPath)) {
					byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
					stream = new MemoryStream(bytes);
					stream.Seek(0, SeekOrigin.Begin);
				}
			} catch (Exception ex) {
				throw ex;
			}

			return stream;
		}

		public static async System.Threading.Tasks.Task UploadFileToServer(Stream stream, string path) {
			try {
				if (Config.ShouldDeploy()) {
					await UploadFileToAmazon(stream, path);

				} else {
					await Task.Run(() => UploadFileToLocal(stream, path));
				}

			} catch (Exception ex) {
				throw ex;
			}
		}

		private static void UploadFileToLocal(Stream stream, string path) {
			string dir = GetBpmnFilesLocalPath();
			string fileName = path.Split('/')[1];
			string dest = Path.Combine(dir, fileName);
			try {
				byte[] bytes = ((MemoryStream)stream).ToArray();
				File.WriteAllBytes(dest, bytes);
			} catch (Exception ex) {

				throw ex;
			}

		}

		private static async System.Threading.Tasks.Task UploadFileToAmazon(Stream stream, string path) {
			var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
				BucketName = "Radial",
				InputStream = stream,
				StorageClass = S3StorageClass.Standard,
				Key = path,
				CannedACL = S3CannedACL.PublicRead,
			};
			//var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USWest2));
			var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
			await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
		}

		public static async System.Threading.Tasks.Task DeleteFileFromServer(string keyName) {

			try {
				if (Config.ShouldDeploy()) {
					await DeleteFileFromAmazon(keyName);

				} else {
					await Task.Run(() => DeleteFileFromLocal(keyName));
				}
			} catch (Exception ex) {
				throw ex;
			}
		}

		private static void DeleteFileFromLocal(string keyName) {
			string dir = GetBpmnFilesLocalPath();
			string fileName = keyName.Split('/')[1];
			string dest = Path.Combine(dir, fileName);
			try {
				if (System.IO.File.Exists(dest)) {
					File.Delete(dest);
				}
			} catch (Exception ex) {

				throw ex;
			}

		}

		private static async System.Threading.Tasks.Task DeleteFileFromAmazon(string keyName) {
			IAmazonS3 client;
			using (client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1)) {
				DeleteObjectRequest deleteObjectRequest = new DeleteObjectRequest {
					BucketName = "Radial",
					Key = keyName
				};
				try {
					var response = await client.DeleteObjectAsync(deleteObjectRequest);
					//Console.WriteLine("Deleting an object");
				} catch (Exception ex) {
					throw ex;
				}
			}
		}

		public static string GetBpmnFilesLocalPath() {
			var dir = Path.Combine(Path.GetTempPath(), "CoreProcess");
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}
			return dir;
		}

		public static Stream CreateBlankFile(string processName, string bpmnId) {
			Stream fileStm = new MemoryStream();
			string id = bpmnId.Replace("-", "");
			XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
			XDocument xmldocument = new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement(bpmn + "definitions", new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(XNamespace.Xmlns + "bpmn", "http://www.omg.org/spec/BPMN/20100524/MODEL"), new XAttribute(XNamespace.Xmlns + "bpmndi", "http://www.omg.org/spec/BPMN/20100524/DI"), new XAttribute(XNamespace.Xmlns + "dc", "http://www.omg.org/spec/DD/20100524/DC"), new XAttribute(XNamespace.Xmlns + "camunda", "http://camunda.org/schema/1.0/bpmn"), new XAttribute(XNamespace.Xmlns + "di", "http://www.omg.org/spec/DD/20100524/DI"), new XAttribute("id", "Definitions_1"), new XAttribute("targetNamespace", "http://bpmn.io/schema/bpmn"),
				new XElement(bpmn + "process", new XAttribute("id", processName.Replace(" ", "") + id), new XAttribute("name", processName), new XAttribute("isExecutable", "true"),
				new XElement(bpmn + "startEvent", new XAttribute("id", "StartEvent"), new XAttribute("name", processName + "&#10;requested")),
				new XElement(bpmn + "endEvent", new XAttribute("id", "EndEvent"), new XAttribute("name", processName + "&#10;finished")))));

			// Save XDocument into the stream
			xmldocument.Save(fileStm);
			fileStm.Seek(0, SeekOrigin.Begin);
			fileStm.Position = 0;
			return fileStm;
		}

		public static IEnumerable<XElement> GetAllElement(XDocument xDoc) {
			return xDoc.Root.Element(bpmn + "process").Elements();
		}

		public static IEnumerable<XElement> GetAllElementByAttr (XDocument xDoc,string attrName ) {
			return xDoc.Root.Element(bpmn + "process").Elements(bpmn + attrName);
		}

		public static XElement FindElementByAttribute(IEnumerable<XElement> elments, string attrName, string attrValue) {
			return elments.Where(t => (t.Attribute(attrName) != null ? t.Attribute(attrName).Value : "") == attrValue).FirstOrDefault();
		}

		public static string GetAttributeValue(XElement elments, string attrName, XNamespace nameSpace = null) {
			if (nameSpace != null)
				return (elments.Attribute(nameSpace + attrName) != null ? (elments.Attribute(nameSpace + attrName).Value) : "");
			else
				return (elments.Attribute(attrName) != null ? (elments.Attribute(attrName).Value) : "");
		}

		public static string ConcatedCandidateString(long[] list) {

			return String.Join(",", list.Select(x => "rgm_" + x));
		}

		public static string GenerateGuid() {
			return Guid.NewGuid().ToString().Replace("-", "");
		}

	}
}