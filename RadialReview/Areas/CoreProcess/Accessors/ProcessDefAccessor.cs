using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using NHibernate;
using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Models;
using RadialReview.Utilities;
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

        public bool Create(UserOrganizationModel caller, string processName)
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

        public bool CreateProcessDef(ISession s, PermissionsUtility perms, string processName)
        {
            bool result = false;
            try
            {
                //create empty bpmn file
                var getStream = CreateBmpnFile(processName);
                string guid = Guid.NewGuid().ToString();
                var path = "CoreProcess/" + guid + ".bpmn";

                //upload to server
                UploadCamundaFile(getStream, path);


                ProcessDef_Camunda processDef = new ProcessDef_Camunda();
                processDef.OrgId = perms.GetCaller().Organization.Id;
                processDef.ProcessDefKey = processName;
                string localProcessDefId = Guid.NewGuid().ToString();
                processDef.LocalId = localProcessDefId;

                s.Save(processDef);

                ProcessDef_CamundaFile processDef_File = new ProcessDef_CamundaFile();
                processDef_File.FileKey = path;
                processDef_File.LocalProcessDefId = localProcessDefId;

                s.Save(processDef_File);

                result = true;
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }

        public bool Edit(UserOrganizationModel caller, string processDefId)
        {
            throw new NotImplementedException();
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

        public Stream CreateBmpnFile(string processName)
        {
            var fileStm = new MemoryStream();
            try
            {
                XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
                XDocument xmldocument = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement(bpmn + "definitions", new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(XNamespace.Xmlns + "bpmn", "http://www.omg.org/spec/BPMN/20100524/MODEL"), new XAttribute(XNamespace.Xmlns + "bpmndi", "http://www.omg.org/spec/BPMN/20100524/DI"), new XAttribute(XNamespace.Xmlns + "dc", "http://www.omg.org/spec/DD/20100524/DC"), new XAttribute(XNamespace.Xmlns + "camunda", "http://camunda.org/schema/1.0/bpmn"), new XAttribute(XNamespace.Xmlns + "di", "http://www.omg.org/spec/DD/20100524/DI"), new XAttribute("id", "Definitions_1"), new XAttribute("targetNamespace", "http://bpmn.io/schema/bpmn"),
                    new XElement(bpmn + "process", new XAttribute("id", processName), new XAttribute("name", processName), new XAttribute("isExecutable", "true"),
                    new XElement(bpmn + "startEvent", new XAttribute("id", "StartEvent"), new XAttribute("name", processName + "&#10;requested")),
                    new XElement(bpmn + "endEvent", new XAttribute("id", "EndEvent"), new XAttribute("name", processName + "&#10;finished")))));

                string dir = System.Web.HttpContext.Current.Server.MapPath("~/Areas/CoreProcess/CamundaFiles/");
                string dest = Path.Combine(dir, "blank.bpmn");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);


                if (!System.IO.File.Exists(dest))
                {
                    xmldocument.Save(dest);
                    FileStream fileStream = new FileStream(dest, FileMode.Open);
                    fileStream.CopyTo(fileStm);
                    fileStm.Seek(0, SeekOrigin.Begin);
                    fileStream.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return fileStm;
        }

        public Stream ModifiyCamundaFile(string keyName, string processName)
        {
            var fileStm = new MemoryStream();
            GetCamundaFileFromServer(keyName);

            string dir = System.Web.HttpContext.Current.Server.MapPath("~/Areas/CoreProcess/CamundaFiles/");
            string dest = Path.Combine(dir, keyName.Split('/')[1]);

            if (File.Exists(dest))
            {
                //Remove all elements under root node
                // XDocument xmldocument = XDocument.Load(dest);
                // xmldocument.Root.Elements().Where(t=>t.Element("usertask").).Remove();
            }


            return fileStm;
        }


        public void UploadCamundaFile(Stream stream, string path)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    stream.Seek(0, SeekOrigin.Begin);
                    ms.Seek(0, SeekOrigin.Begin);

                    var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = "Radial",
                        InputStream = ms,
                        StorageClass = S3StorageClass.Standard,
                        Key = path,
                        CannedACL = S3CannedACL.PublicRead,
                    };
                    var fileTransferUtility = new TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));
                    fileTransferUtility.Upload(fileTransferUtilityRequest);
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public void GetCamundaFileFromServer(string keyName)
        {
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
                        string dir = System.Web.HttpContext.Current.Server.MapPath("~/Areas/CoreProcess/CamundaFiles/");
                        string dest = Path.Combine(dir, keyName.Split('/')[1]);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        if (!File.Exists(dest))
                        {
                            response.WriteResponseStreamToFile(dest);
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}