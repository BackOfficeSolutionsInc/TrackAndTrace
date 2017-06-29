using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using NHibernate;
using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Areas.CoreProcess.Models.ViewModel;
using RadialReview.Models;
using RadialReview.Utilities;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace RadialReview.Areas.CoreProcess.Accessors
{
    public class ProcessDefAccessor : IProcessDefAccessor
    {
        public string Deploy(UserOrganizationModel caller, string key, List<object> files)
        {
            // call Comm Layer
            CommClass commClass = new CommClass();
            var result = commClass.Deploy(key, files);

            return string.Empty;
        }

        public long Create(UserOrganizationModel caller, string processName)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var created = CreateProcessDef(s, perms, processName);
                    tx.Commit();
                    s.Flush();
                    return created;
                }
            }
        }

        public long CreateProcessDef(ISession s, PermissionsUtility perms, string processName)
        {
            try
            {
                string localProcessDefId = Guid.NewGuid().ToString();
                
                //create empty bpmn file
                var getStream = CreateBpmnFile(processName,localProcessDefId);
                string guid = Guid.NewGuid().ToString();
                var path = "CoreProcess/" + guid + ".bpmn";

                //upload to server
                UploadCamundaFile(getStream, path);


                ProcessDef_Camunda processDef = new ProcessDef_Camunda();
                processDef.OrgId = perms.GetCaller().Organization.Id;
                processDef.ProcessDefKey = processName;
                processDef.LocalId = localProcessDefId;

                s.Save(processDef);

                ProcessDef_CamundaFile processDef_File = new ProcessDef_CamundaFile();
                processDef_File.FileKey = path;
                processDef_File.LocalProcessDefId = localProcessDefId;

                s.Save(processDef_File);

                return processDef.Id;
            }
            catch (Exception)
            {
                throw;
            }

        }

        public bool Edit(UserOrganizationModel caller, string localId, string processName)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller);
                    var updated = Edit(s, localId, processName);
                    tx.Commit();
                    s.Flush();
                    return updated;
                }
            }
        }

        public bool Edit(ISession s, string localId, string processName)
        {
            bool result = true;
            try
            {
                var getProcessDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.LocalId == localId).SingleOrDefault();
                getProcessDefDetails.ProcessDefKey = processName;
                s.Update(getProcessDefDetails);

                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    var getfileStream = GetCamundaFileFromServer(getProcessDefFileDetails.FileKey);
                    MemoryStream fileStream = new MemoryStream();

                    getfileStream.Seek(0, SeekOrigin.Begin);
                    XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                    XDocument xmlDocument = XDocument.Load(getfileStream);
                    var getElement = xmlDocument.Root.Element(bpmn + "process");
                    getElement.SetAttributeValue("name", processName);

                    xmlDocument.Save(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Position = 0;

                    UploadCamundaFile(fileStream, getProcessDefFileDetails.FileKey);

                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }

        public bool CreateTask(UserOrganizationModel caller, string localId, TaskViewModel model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                PermissionsUtility.Create(s, caller);
                var created = CreateTask(s, localId, model);
                return created;
            }

        }

        public bool CreateTask(ISession s, string localId, TaskViewModel model)
        {
            bool result = true;
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    var getfileStream = GetCamundaFileFromServer(getProcessDefFileDetails.FileKey);
                    MemoryStream fileStream = new MemoryStream();

                    getfileStream.Seek(0, SeekOrigin.Begin);
                    XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                    XDocument xmlDocument = XDocument.Load(getfileStream);
                    var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();
                    var getEndProcessElement = getAllElement.Where(t => (t.Attribute("id") != null ? t.Attribute("id").Value : "") == "EndEvent").FirstOrDefault();
                    var getStartProcessElement = getAllElement.Where(t => (t.Attribute("id") != null ? t.Attribute("id").Value : "") == "StartEvent").FirstOrDefault();

                    //getAllElement

                    int targetCounter = 0;
                    int sourceCounter = 0;
                    foreach (var item in getAllElement.ToList())
                    {
                        if (item.Attribute("targetRef") != null)
                        {
                            if (item.Attribute("targetRef").Value == "EndEvent")
                            {
                                targetCounter++;
                            }
                        }

                        if (item.Attribute("sourceRef") != null)
                        {
                            if (item.Attribute("sourceRef").Value == "StartEvent")
                            {
                                sourceCounter++;
                            }
                        }
                    }

                    string userTaskId = Guid.NewGuid().ToString();
                    if (sourceCounter == 0)
                    {
                        getAllElement.Where(m => m.Attribute("id").Value == getStartProcessElement.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()),
                                  new XAttribute("sourceRef", getStartProcessElement.Attribute("id").Value), new XAttribute("targetRef", userTaskId))
                                  );

                        if (targetCounter == 0)
                        {
                            getAllElement.Where(m => m.Attribute("id").Value == getEndProcessElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name),
                                        new XAttribute("description", model.description ?? "")),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()),
                                        new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );
                        }
                        else
                        {
                            var getEndEventSrc = getAllElement.Where(x => (x.Attribute("sourceRef") != null ? x.Attribute("sourceRef").Value : "") == getEndProcessElement.Attribute("id").Value).FirstOrDefault();
                            getAllElement.Where(m => m.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name),
                                        new XAttribute("description", model.description ?? "")),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()),
                                        new XAttribute("sourceRef", getEndEventSrc.Attribute("id").Value), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );
                        }

                    }
                    else
                    {
                        if (targetCounter == 0)
                        {
                            getAllElement.Where(m => m.Attribute("id").Value == getStartProcessElement.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                      new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()),
                                      new XAttribute("sourceRef", getStartProcessElement.Attribute("id").Value), new XAttribute("targetRef", userTaskId)),
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name),
                                        new XAttribute("description", model.description ?? "")),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()),
                                        new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );
                        }
                        else
                        {
                            var getEndEventSrc = getAllElement.Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == getEndProcessElement.Attribute("id").Value).FirstOrDefault();
                            getAllElement.Where(m => m.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                                        new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name),
                                        new XAttribute("description", model.description ?? "")),
                                        new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()),
                                        new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
                                      );

                            getAllElement.Where(x => x.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", userTaskId);
                        }
                    }

                    xmlDocument.Save(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Position = 0;

                    UploadCamundaFile(fileStream, getProcessDefFileDetails.FileKey);
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }

        public List<TaskViewModel> GetAllTask(UserOrganizationModel caller, string localId)
        {
            List<TaskViewModel> taskList = new List<TaskViewModel>();
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    PermissionsUtility.Create(s, caller);
                    var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                    if (getProcessDefFileDetails != null)
                    {
                        var getfileStream = GetCamundaFileFromServer(getProcessDefFileDetails.FileKey);
                        getfileStream.Seek(0, SeekOrigin.Begin);
                        XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                        XDocument xmlDocument = XDocument.Load(getfileStream);
                        var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask");

                        foreach (var item in getAllElement)
                        {
                            taskList.Add(new TaskViewModel()
                            {
                                description = (item.Attribute("description") != null ? item.Attribute("description").Value : ""),
                                name = item.Attribute("name").Value,
                                Id = Guid.Parse(item.Attribute("id").Value)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return taskList;
        }

        public bool UpdateTask(UserOrganizationModel caller, string localId, TaskViewModel model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                PermissionsUtility.Create(s, caller);
                var updated = UpdateTask(s, localId, model);
                return updated;
            }
        }

        public bool UpdateTask(ISession s, string localId, TaskViewModel model)
        {
            bool result = true;
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    var getfileStream = GetCamundaFileFromServer(getProcessDefFileDetails.FileKey);
                    MemoryStream fileStream = new MemoryStream();

                    getfileStream.Seek(0, SeekOrigin.Begin);
                    XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                    XDocument xmlDocument = XDocument.Load(getfileStream);
                    var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();

                    //update name element
                    getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue("name", model.name);

                    //update description element
                    getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue("description", model.description);

                    xmlDocument.Save(fileStream);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    fileStream.Position = 0;

                    UploadCamundaFile(fileStream, getProcessDefFileDetails.FileKey);
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }


        public IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller);
                    IEnumerable<ProcessDef_Camunda> processDefList = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id).List();
                    return processDefList;
                }
            }
        }

        public ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller);
                    ProcessDef_Camunda processDef = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id && x.Id == processId).SingleOrDefault();
                    return processDef;
                }
            }
        }

        public IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller)
        {
            throw new NotImplementedException();
        }

        public IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId)
        {
            throw new NotImplementedException();
        }

        public IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key)
        {
            CommClass commClass = new CommClass();
            return commClass.GetProcessDefByKey(key);
            //throw new NotImplementedException();
        }

        public Stream CreateBpmnFile(string processName,string localId)
        {
            var fileStm = new MemoryStream();
            try
            {
                XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                XDocument xmldocument = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement(bpmn + "definitions", new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(XNamespace.Xmlns + "bpmn", "http://www.omg.org/spec/BPMN/20100524/MODEL"), new XAttribute(XNamespace.Xmlns + "bpmndi", "http://www.omg.org/spec/BPMN/20100524/DI"), new XAttribute(XNamespace.Xmlns + "dc", "http://www.omg.org/spec/DD/20100524/DC"), new XAttribute(XNamespace.Xmlns + "camunda", "http://camunda.org/schema/1.0/bpmn"), new XAttribute(XNamespace.Xmlns + "di", "http://www.omg.org/spec/DD/20100524/DI"), new XAttribute("id", "Definitions_1"), new XAttribute("targetNamespace", "http://bpmn.io/schema/bpmn"),
                    new XElement(bpmn + "process", new XAttribute("id", localId), new XAttribute("name", processName), new XAttribute("isExecutable", "true"),
                    new XElement(bpmn + "startEvent", new XAttribute("id", "StartEvent"), new XAttribute("name", processName + "&#10;requested")),
                    new XElement(bpmn + "endEvent", new XAttribute("id", "EndEvent"), new XAttribute("name", processName + "&#10;finished")))));

                string dir = System.Web.HttpContext.Current.Server.MapPath("~/Areas/CoreProcess/CamundaFiles/");
                string dest = Path.Combine(dir, "blank.bpmn");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                xmldocument.Save(dest);
                FileStream fileStream = new FileStream(dest, FileMode.Open);
                fileStream.CopyTo(fileStm);
                fileStm.Seek(0, SeekOrigin.Begin);
                fileStream.Close();

            }
            catch (Exception)
            {
                throw;
            }
            return fileStm;
        }

        public bool ModifiyBpmnFile(UserOrganizationModel caller, string localId, string oldOrder, string newOrder)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller);
                    var result = ModifyBpmnFile(s, localId, oldOrder, newOrder);
                    return result;
                }
            }

        }

        public bool ModifyBpmnFile(ISession s,string localId, string oldOrder, string newOrder)
        {
            bool result = true;
            try
            {
                var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
                if (getProcessDefFileDetails != null)
                {
                    var getStream = GetCamundaFileFromServer(getProcessDefFileDetails.FileKey);

                    //Remove all elements under root node
                    var de_stream = DetachNode(getStream, oldOrder, newOrder);

                    //Insert element

                    var ins_stream = InsertNode(de_stream, oldOrder, newOrder);

                    //stream upload
                    UploadCamundaFile(ins_stream, getProcessDefFileDetails.FileKey);
                }
            }
            catch (Exception ex)
            {
                result = false;
                throw ex;
            }

            return result;
        }

        public Stream DetachNode(Stream stream, string oldOrder, string newOrder)
        {
            string deletenodeid = oldOrder;
            string afterNode = newOrder;

            MemoryStream fileStream = new MemoryStream();

            stream.Seek(0, SeekOrigin.Begin);
            XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
            XDocument xmlDocument = XDocument.Load(stream);

            //get node
            var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == deletenodeid).ToList();

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
                var getTargetElement = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("id") != null ? x.Attribute("id").Value : "") == target).FirstOrDefault();

                //apppend element
                elements.Where(m => m.Attribute("id").Value == getTargetElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
                          new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()), new XAttribute("sourceRef", source), new XAttribute("targetRef", target))
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

        public Stream InsertNode(Stream stream, string oldOrder, string newOrder)
        {

            MemoryStream fileStream = new MemoryStream();
            stream.Seek(0, SeekOrigin.Begin);

            try
            {
                string deletenodeId = oldOrder;
                string afterNode = newOrder;

                //file initilaize

                XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";

                XDocument xmlDocument = XDocument.Load(stream);

                //get node
                var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == afterNode).ToList();
                var getBeforeNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == afterNode).FirstOrDefault();

                var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();

                getAllElement.Where(t => t.Attribute("id").Value == getBeforeNode.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
                        new XElement(bpmn + "userTask", new XAttribute("id", deletenodeId), new XAttribute("name", "Review result")),
                       new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString()), new XAttribute("sourceRef", deletenodeId), new XAttribute("targetRef", afterNode))
                    );

                //update target element attr

                getAllElement.Where(t => t.Attribute("id").Value == getBeforeNode.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", deletenodeId);

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

        public void UploadCamundaFile(Stream stream, string path)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    //stream.CopyTo(ms);
                    //stream.Seek(0, SeekOrigin.Begin);
                    //ms.Seek(0, SeekOrigin.Begin);

                    var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = "Radial",
                        InputStream = stream,
                        StorageClass = S3StorageClass.Standard,
                        Key = path,
                        CannedACL = S3CannedACL.PublicRead,
                    };
                    //var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USWest2));
                    var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
                    fileTransferUtility.Upload(fileTransferUtilityRequest);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Stream GetCamundaFileFromServer(string keyName)
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

                    using (GetObjectResponse response = client.GetObject(request))
                    {
                        //string dir = System.Web.HttpContext.Current.Server.MapPath("~/Areas/CoreProcess/CamundaFiles/");
                        //string dest = Path.Combine(dir, keyName.Split('/')[1]);
                        //if (!Directory.Exists(dir))
                        //    Directory.CreateDirectory(dir);
                        //if (!File.Exists(dest))
                        //{
                        //    response.WriteResponseStreamToFile(dest);
                        //}

                        //XDocument xmlDocument = XDocument.Load(response.ResponseStream);
                        using (var ms = new MemoryStream())
                        {
                            response.ResponseStream.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            ms.CopyTo(stream);
                            ms.Seek(0, SeekOrigin.Begin);
                            stream.Seek(0, SeekOrigin.Begin);
                        }

                        //XDocument xmlDocument1 = XDocument.Load(stream);
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
}