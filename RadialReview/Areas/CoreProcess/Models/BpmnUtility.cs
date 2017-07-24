using Amazon.S3;
using Amazon.S3.Model;
using RadialReview.Accessors;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace RadialReview.Areas.CoreProcess.Models
{
    public class BpmnUtility
    {
        private static XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
        private static XNamespace camunda = "http://camunda.org/schema/1.0/bpmn";
        public static string GetMemberName(UserOrganizationModel caller, string candidateGroupName, long[] memberIds)
        {
            long[] getMemberIds = null;
            if (memberIds != null && memberIds.Any())
            {
                getMemberIds = memberIds;
            }
            else
            {
                getMemberIds = GetMemberIds(candidateGroupName);
            }

            ResponsibilitiesAccessor respAccessor = new ResponsibilitiesAccessor();
            string memberName = string.Empty;
            if (getMemberIds != null)
            {
                if (getMemberIds.Any())
                {
                    foreach (var item in getMemberIds)
                    {
                        var getMemberName = respAccessor.GetResponsibilityGroup(caller, item).GetName();
                        if (!string.IsNullOrEmpty(getMemberName))
                        {
                            if (string.IsNullOrEmpty(memberName))
                                memberName = getMemberName;
                            else
                                memberName += ", " + getMemberName;
                        }
                    }
                }
            }
            return memberName;
        }

        public static long[] GetMemberIds(string memberIds)
        {
            List<long> idList = new List<long>();
            if (!string.IsNullOrEmpty(memberIds))
            {
                var getMemberIds = memberIds.Split(',');
                if (getMemberIds != null)
                {
                    if (getMemberIds.Any())
                    {
                        foreach (var item in getMemberIds)
                        {
                            var getItem = item.Split('_');
                            var id = 0l;
                            if (getItem.Length > 1 && long.TryParse(getItem[1], out id))
                            {
                                idList.Add(id);
                            }
                        }
                    }
                }
            }
            return idList.ToArray();
        }

        public static async Task<XDocument> GetBpmnFileXmlDoc(string keyName)
        {
            var getfileStream = await GetFileFromServer(keyName);
            getfileStream.Seek(0, SeekOrigin.Begin);
            return XDocument.Load(getfileStream);
        }

        public static Stream DetachNode(Stream stream, string deletedNodeId)
        {
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

            try
            {
                int targetCounter = 0;
                int sourceCounter = 0;
                foreach (var item in elements.ToList())
                {
                    if (item.Attribute("targetRef") != null)
                    {
                        if (item.Attribute("targetRef").Value == attrId)
                        {
                            source = item.Attribute("sourceRef").Value;
                            item.Remove();
                            targetCounter++;
                        }
                    }

                    if (item.Attribute("sourceRef") != null)
                    {
                        if (item.Attribute("sourceRef").Value == attrId)
                        {
                            target = item.Attribute("targetRef").Value;
                            item.Remove();
                            sourceCounter++;
                        }
                    }
                }

                if (targetCounter != 1)
                {
                    throw new Exception("Could not detach node. As targetRef occurs more than once.");
                }

                if (sourceCounter != 1)
                {
                    throw new Exception("Could not detach node. As sourceRef occurs more than once.");
                }

                deleteNode.Remove();

                //get target element
                var getTargetElement = elements.Where(x => (x.Attribute("id") != null ? x.Attribute("id").Value : "") == target).FirstOrDefault();

                //apppend element
                elements.Where(m => m.Attribute("id").Value == getTargetElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
                          new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")), new XAttribute("sourceRef", source), new XAttribute("targetRef", target))
                          );

                xmlDocument.Save(fileStream);
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Position = 0;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return fileStream;
        }

        public static Stream InsertNode(Stream stream, int oldOrder, int newOrder, string oldOrderId, string newOrderId, string name, string candidateGroups)
        {

            MemoryStream fileStream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);

            try
            {
                string deletenodeId = oldOrderId;
                string afterNode = newOrderId;

                //file initilaize

                XDocument xmlDocument = XDocument.Load(stream);

                //get node
                var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == afterNode).ToList();
                var getBeforeNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == afterNode).FirstOrDefault();

                var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();

                if (newOrder > oldOrder)
                {

                    var getSequenceNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("sourceRef") != null ? x.Attribute("sourceRef").Value : "") == afterNode).FirstOrDefault();

                    getAllElement.Where(t => t.Attribute("id").Value == getSequenceNode.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                           new XElement(bpmn + "userTask", new XAttribute("id", deletenodeId), new XAttribute("name", name), new XAttribute(camunda + "candidateGroups", candidateGroups)),
                          new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")), new XAttribute("sourceRef", deletenodeId), new XAttribute("targetRef", getSequenceNode.Attribute("targetRef").Value))
                       );

                    getAllElement.Where(t => t.Attribute("id").Value == getSequenceNode.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", deletenodeId);
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return fileStream;
        }

        public static void  GetNodeDetail(Stream stream, int oldOrder, int newOrder, out string oldOrderId, out string newOrderId, out string name, out string candidateGroups)
        {
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

        public static async Task<Stream> GetFileFromServer(string keyName)
        {
            Stream stream = new MemoryStream();
            try
            {
                IAmazonS3 client;
                using (client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1))
                {
                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = "Radial",
                        Key = keyName
                    };

                    using (GetObjectResponse response = await client.GetObjectAsync(request))
                    {
                        using (var ms = new MemoryStream())
                        {
                            response.ResponseStream.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            ms.CopyTo(stream);
                            ms.Seek(0, SeekOrigin.Begin);
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return stream;
        }

    }

    internal static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new
          TaskFactory(CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskContinuationOptions.None,
                      TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return AsyncHelper._myTaskFactory
              .StartNew<Task<TResult>>(func)
              .Unwrap<TResult>()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            AsyncHelper._myTaskFactory
              .StartNew<Task>(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}