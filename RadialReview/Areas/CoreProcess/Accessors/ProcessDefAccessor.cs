using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CamundaCSharpClient.Model.Deployment;
using log4net;
using NHibernate;
using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Areas.CoreProcess.Models.ViewModel;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace RadialReview.Areas.CoreProcess.Accessors {
	public class ProcessDefAccessor : IProcessDefAccessor {

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public async Task<bool> Deploy(UserOrganizationModel caller, string localId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var deployed = await Deploy(s, localId);
					tx.Commit();
					s.Flush();
					return deployed;
				}
			}
		}

		public async Task<bool> Deploy(ISession s, string localId) {
			bool result = true;
			try {
				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					var getfileStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);


					List<object> fileObject = new List<object>();
					byte[] bytes = ((MemoryStream)getfileStream).ToArray();
					fileObject.Add(new FileParameter(bytes, getProcessDefFileDetails.FileKey.Split('/')[1].Replace("-", "")));

					getfileStream.Seek(0, SeekOrigin.Begin);
					XDocument x1 = XDocument.Load(getfileStream);

					var getProcessDefDetail = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.LocalId == localId).SingleOrDefault();
					string key = getProcessDefDetail.ProcessDefKey;

					// call Comm Layer
					CommClass commClass = new CommClass();
					var getDeploymentId = commClass.Deploy(key, fileObject);

					getProcessDefFileDetails.DeploymentId = getDeploymentId;
					s.Update(getProcessDefFileDetails);

					//get process def
					var getProcessDef = commClass.GetProcessDefByKey(key.Replace(" ", "") + localId.Replace("-", ""));

					if (getProcessDefDetail != null) {
						getProcessDefDetail.CamundaId = getProcessDef.GetId();
						s.Update(getProcessDefFileDetails);
					}
				}
			} catch (Exception ex) {
				result = false;
				throw ex;
			}
			return result;
		}


		public ProcessDef_Camunda ProcessStart(UserOrganizationModel caller, long processId) {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						PermissionsUtility.Create(s, caller);
						var getProcessDefDetail = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processId).SingleOrDefault();
						if (getProcessDefDetail != null) {
							// call Comm Layer
							CommClass commClass = new CommClass();
							var startProcess = commClass.ProcessStart(getProcessDefDetail.CamundaId);

							ProcessInstance_Camunda processIns = new ProcessInstance_Camunda();
							processIns.LocalProcessInstanceId = getProcessDefDetail.LocalId;
							//processIns.ProcessDefId = startProcess.DefinitionId;
							processIns.Suspended = startProcess.Suspended;
							processIns.CamundaProcessInstanceId = startProcess.Id;
							s.Save(processIns);

							tx.Commit();
							s.Flush();

							return getProcessDefDetail;

						} else {
							throw new PermissionsException("Cannot start process.");
						}
					}
				}
			} catch (Exception ex) {
				log.Error(ex);
				throw new PermissionsException("Cannot start process.");
			}

			return new ProcessDef_Camunda();
		}

		public bool ProcessSuspend(UserOrganizationModel caller, string processInsId, bool isSuspend) {
			bool result = false;
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						PermissionsUtility.Create(s, caller);
						var getProcessInsDetail = s.QueryOver<ProcessInstance_Camunda>().Where(x => x.DeleteTime == null && x.CamundaProcessInstanceId == processInsId).SingleOrDefault();
						if (getProcessInsDetail != null) {
							// call Comm Layer
							CommClass commClass = new CommClass();
							var startProcess = commClass.ProcessSuspend(processInsId, isSuspend);
							if (startProcess.TNoContentStatus.ToString() == "Success") {
								getProcessInsDetail.Suspended = isSuspend;
								s.Update(getProcessInsDetail);

								tx.Commit();
								s.Flush();
								result = true;
							}
						}
					}
				}
			} catch (Exception ex) {
				throw ex;
			}
			return result;
		}
		public List<ProcessInstanceViewModel> GetProcessInstanceList(string localId) {
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					return s.QueryOver<ProcessInstance_Camunda>().Where(x => x.DeleteTime == null
					&& x.LocalProcessInstanceId == localId
					&& x.CompleteTime == null
					).List()
						.Select(x => new ProcessInstanceViewModel() {
							Id = x.CamundaProcessInstanceId,
							DefinitionId = x.LocalProcessInstanceId,
							Suspended = x.Suspended
						}).ToList();
				}
			} catch (Exception ex) {

				throw ex;
			}

		}
		public async Task<long> Create(UserOrganizationModel caller, string processName) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var created = await CreateProcessDef(s, perms, processName);
					tx.Commit();
					s.Flush();
					return created;
				}
			}
		}

		public async Task<long> CreateProcessDef(ISession s, PermissionsUtility perms, string processName) {
			try {
				string localProcessDefId = Guid.NewGuid().ToString();

				//create empty bpmn file
				var getStream = CreateBpmnFile(processName, localProcessDefId);
				string guid = Guid.NewGuid().ToString();
				var path = "CoreProcess/" + guid + ".bpmn";

				//upload to server
				await UploadFileToServer(getStream, path);


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
			} catch (Exception) {
				throw;
			}

		}

		public async Task<bool> Edit(UserOrganizationModel caller, string localId, string processName) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller);
					var updated = await Edit(s, localId, processName);
					tx.Commit();
					s.Flush();
					return updated;
				}
			}
		}

		public async Task<bool> Edit(ISession s, string localId, string processName) {
			bool result = true;
			try {
				var getProcessDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.LocalId == localId).SingleOrDefault();
				getProcessDefDetails.ProcessDefKey = processName;
				s.Update(getProcessDefDetails);

				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					var getfileStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);
					MemoryStream fileStream = new MemoryStream();

					getfileStream.Seek(0, SeekOrigin.Begin);
					XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
					XDocument xmlDocument = XDocument.Load(getfileStream);
					var getElement = xmlDocument.Root.Element(bpmn + "process");
					getElement.SetAttributeValue("name", processName);

					xmlDocument.Save(fileStream);
					fileStream.Seek(0, SeekOrigin.Begin);
					fileStream.Position = 0;

					await UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

				}
			} catch (Exception ex) {
				result = false;
				throw ex;
			}

			return result;
		}

		public async Task<bool> Delete(UserOrganizationModel caller, long processId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller);
					var deleted = await Delete(s, processId);
					tx.Commit();
					s.Flush();
					return deleted;
				}
			}
		}

		public async Task<bool> Delete(ISession s, long processId) {
			bool result = true;
			try {
				var getProcessDefDetails = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.Id == processId).SingleOrDefault();
				getProcessDefDetails.DeleteTime = DateTime.UtcNow;

				s.Update(getProcessDefDetails);

				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == getProcessDefDetails.LocalId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					await DeleteFileFromServer(getProcessDefFileDetails.FileKey);
				}
			} catch (Exception ex) {
				result = false;
				throw ex;
			}

			return result;
		}

		public async Task<TaskViewModel> CreateTask(UserOrganizationModel caller, string localId, TaskViewModel model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				PermissionsUtility.Create(s, caller);
				var created = await CreateTask(s, localId, model);
				return created;
			}

		}

		public async Task<TaskViewModel> CreateTask(ISession s, string localId, TaskViewModel model) {
			TaskViewModel modelObj = new TaskViewModel();
			modelObj = model;
			try {
				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					var getfileStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);
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
					foreach (var item in getAllElement.ToList()) {
						if (item.Attribute("targetRef") != null) {
							if (item.Attribute("targetRef").Value == "EndEvent") {
								targetCounter++;
							}
						}

						if (item.Attribute("sourceRef") != null) {
							if (item.Attribute("sourceRef").Value == "StartEvent") {
								sourceCounter++;
							}
						}
					}

					string userTaskId = "Task" + Guid.NewGuid().ToString().Replace("-", "");
					if (sourceCounter == 0) {
						getAllElement.Where(m => m.Attribute("id").Value == getStartProcessElement.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
								  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
								  new XAttribute("sourceRef", getStartProcessElement.Attribute("id").Value), new XAttribute("targetRef", userTaskId))
								  );

						if (targetCounter == 0) {
							getAllElement.Where(m => m.Attribute("id").Value == getEndProcessElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
										new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name)),
										new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
										new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
									  );
						} else {
							var getEndEventSrc = getAllElement.Where(x => (x.Attribute("sourceRef") != null ? x.Attribute("sourceRef").Value : "") == getEndProcessElement.Attribute("id").Value).FirstOrDefault();
							getAllElement.Where(m => m.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
										new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name)),
										new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
										new XAttribute("sourceRef", getEndEventSrc.Attribute("id").Value), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
									  );
						}

					} else {
						if (targetCounter == 0) {
							getAllElement.Where(m => m.Attribute("id").Value == getStartProcessElement.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
									  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
									  new XAttribute("sourceRef", getStartProcessElement.Attribute("id").Value), new XAttribute("targetRef", userTaskId)),
										new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name)),
										new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
										new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
									  );
						} else {
							var getEndEventSrc = getAllElement.Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == getEndProcessElement.Attribute("id").Value).FirstOrDefault();
							getAllElement.Where(m => m.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
										new XElement(bpmn + "userTask", new XAttribute("id", userTaskId), new XAttribute("name", model.name)),
										new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")),
										new XAttribute("sourceRef", userTaskId), new XAttribute("targetRef", getEndProcessElement.Attribute("id").Value))
									  );

							getAllElement.Where(x => x.Attribute("id").Value == getEndEventSrc.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", userTaskId);
						}
					}

					xmlDocument.Save(fileStream);
					fileStream.Seek(0, SeekOrigin.Begin);
					fileStream.Position = 0;

					await UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

					modelObj.Id = userTaskId;
				}
			} catch (Exception ex) {
				throw ex;
			}
			return modelObj;
		}


		public async Task<List<TaskViewModel>> GetAllTask(UserOrganizationModel caller, string localId) {
			List<TaskViewModel> taskList = new List<TaskViewModel>();
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					PermissionsUtility.Create(s, caller);
					var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
					if (getProcessDefFileDetails != null) {
						var getfileStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);
						getfileStream.Seek(0, SeekOrigin.Begin);
						XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
						XDocument xmlDocument = XDocument.Load(getfileStream);
						var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask");

						foreach (var item in getAllElement) {
							taskList.Add(new TaskViewModel() {
								description = (item.Attribute("description") != null ? item.Attribute("description").Value : ""),
								name = item.Attribute("name").Value,
								Id = item.Attribute("id").Value
							});
						}
					}
				}
			} catch (Exception ex) {
				throw ex;
			}

			return taskList;
		}

		public async Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, string localId, TaskViewModel model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				PermissionsUtility.Create(s, caller);
				var updated = await UpdateTask(s, localId, model);
				return updated;
			}
		}

		public async Task<TaskViewModel> UpdateTask(ISession s, string localId, TaskViewModel model) {
			TaskViewModel modelObj = new TaskViewModel();
			modelObj = model;
			try {
				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					var getfileStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);
					MemoryStream fileStream = new MemoryStream();

					getfileStream.Seek(0, SeekOrigin.Begin);
					XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
					XDocument xmlDocument = XDocument.Load(getfileStream);
					var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();

					//update name element
					getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue("name", model.name);

					//update description element
					//getAllElement.Where(x => x.Attribute("id").Value == model.Id.ToString()).FirstOrDefault().SetAttributeValue("description", model.description);

					xmlDocument.Save(fileStream);
					fileStream.Seek(0, SeekOrigin.Begin);
					fileStream.Position = 0;

					await UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);

				}
			} catch (Exception ex) {
				throw ex;
			}

			return modelObj;
		}

		public async Task<bool> DeleteTask(UserOrganizationModel caller, string taskId, string localId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				PermissionsUtility.Create(s, caller);
				var result = await DeleteTask(s, localId, taskId);
				return result;
			}
		}

		public async Task<bool> DeleteTask(ISession s, string localId, string taskId) {
			bool result = true;
			try {
				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					var getfileStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);
					MemoryStream fileStream = new MemoryStream();

					//Detach Node
					var getModifiedStream = DetachNode(getfileStream, taskId);

					getModifiedStream.Seek(0, SeekOrigin.Begin);
					XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
					XDocument xmlDocument = XDocument.Load(getModifiedStream);


					xmlDocument.Save(fileStream);
					fileStream.Seek(0, SeekOrigin.Begin);
					fileStream.Position = 0;

					await UploadFileToServer(fileStream, getProcessDefFileDetails.FileKey);
				}
			} catch (Exception ex) {
				result = false;
				throw ex;

			}
			return result;
		}

		public IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller);
					IEnumerable<ProcessDef_Camunda> processDefList = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id).List();
					return processDefList;
				}
			}
		}

		public ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller);
					ProcessDef_Camunda processDef = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null && x.OrgId == caller.Organization.Id && x.Id == processId).SingleOrDefault();
					return processDef;
				}
			}
		}

		public IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller) {
			throw new NotImplementedException();
		}

		public IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId) {
			throw new NotImplementedException();
		}

		public IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key) {
			CommClass commClass = new CommClass();
			return commClass.GetProcessDefByKey(key);
			//throw new NotImplementedException();
		}

		public Stream CreateBpmnFile(string processName, string localId) {
			var fileStm = new MemoryStream();
			try {
				string id = localId.Replace("-", "");
				XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
				XDocument xmldocument = new XDocument(
					new XDeclaration("1.0", "utf-8", null),
					new XElement(bpmn + "definitions", new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(XNamespace.Xmlns + "bpmn", "http://www.omg.org/spec/BPMN/20100524/MODEL"), new XAttribute(XNamespace.Xmlns + "bpmndi", "http://www.omg.org/spec/BPMN/20100524/DI"), new XAttribute(XNamespace.Xmlns + "dc", "http://www.omg.org/spec/DD/20100524/DC"), new XAttribute(XNamespace.Xmlns + "camunda", "http://camunda.org/schema/1.0/bpmn"), new XAttribute(XNamespace.Xmlns + "di", "http://www.omg.org/spec/DD/20100524/DI"), new XAttribute("id", "Definitions_1"), new XAttribute("targetNamespace", "http://bpmn.io/schema/bpmn"),
					new XElement(bpmn + "process", new XAttribute("id", processName.Replace(" ", "") + id), new XAttribute("name", processName), new XAttribute("isExecutable", "true"),
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

			} catch (Exception) {
				throw;
			}
			return fileStm;
		}

		public async Task<bool> ModifiyBpmnFile(UserOrganizationModel caller, string localId, int oldOrder, int newOrder) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller);
					var result = await ModifyBpmnFile(s, localId, oldOrder, newOrder);
					return result;
				}
			}

		}

		public async Task<bool> ModifyBpmnFile(ISession s, string localId, int oldOrder, int newOrder) {
			bool result = true;
			try {
				string oldOrderId = string.Empty;
				string newOrderId = string.Empty;
				string name = string.Empty;
				string description = string.Empty;
				var getProcessDefFileDetails = s.QueryOver<ProcessDef_CamundaFile>().Where(x => x.DeleteTime == null && x.LocalProcessDefId == localId).SingleOrDefault();
				if (getProcessDefFileDetails != null) {
					var getStream = await GetFileFromServer(getProcessDefFileDetails.FileKey);

					GetNodeDetail(getStream, oldOrder, newOrder, out oldOrderId, out newOrderId, out name, out description);

					//Remove all elements under root node
					var de_stream = DetachNode(getStream, oldOrderId);

					//Insert element

					var ins_stream = InsertNode(de_stream, oldOrder, newOrder, oldOrderId, newOrderId, name, description);

					//stream upload
					await UploadFileToServer(ins_stream, getProcessDefFileDetails.FileKey);
				}
			} catch (Exception ex) {
				result = false;
				throw ex;
			}

			return result;
		}

		public void GetNodeDetail(Stream stream, int oldOrder, int newOrder, out string oldOrderId, out string newOrderId, out string name, out string description) {
			stream.Seek(0, SeekOrigin.Begin);
			XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
			XDocument xmlDocument = XDocument.Load(stream);

			//get user task
			var getAlltask = xmlDocument.Root.Element(bpmn + "process").Elements(bpmn + "userTask").ToList();

			string deletenodeid = getAlltask[oldOrder].Attribute("id").Value;
			string afterNode = getAlltask[newOrder].Attribute("id").Value;

			//get name and description of deleted node
			name = (getAlltask[oldOrder].Attribute("name") != null ? getAlltask[oldOrder].Attribute("name").Value : "");
			description = (getAlltask[oldOrder].Attribute("description") != null ? getAlltask[oldOrder].Attribute("description").Value : "");

			oldOrderId = deletenodeid;
			newOrderId = afterNode;
		}

		public Stream DetachNode(Stream stream, string deletedNodeId) {
			MemoryStream fileStream = new MemoryStream();

			stream.Seek(0, SeekOrigin.Begin);
			XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
			XDocument xmlDocument = XDocument.Load(stream);

			//get node
			var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == deletedNodeId).ToList();

			var deleteNode = current.FirstOrDefault();
			string attrId = deleteNode.Attribute("id").Value;

			string source = string.Empty;
			string target = string.Empty;
			var elements = xmlDocument.Root.Element(bpmn + "process").Elements();

			try {
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
				var getTargetElement = elements.Where(x => (x.Attribute("id") != null ? x.Attribute("id").Value : "") == target).FirstOrDefault();

				//apppend element
				elements.Where(m => m.Attribute("id").Value == getTargetElement.Attribute("id").Value).FirstOrDefault().AddBeforeSelf(
						  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")), new XAttribute("sourceRef", source), new XAttribute("targetRef", target))
						  );

				xmlDocument.Save(fileStream);
				fileStream.Seek(0, SeekOrigin.Begin);
				fileStream.Position = 0;

			} catch (Exception ex) {
				throw ex;
			}

			return fileStream;
		}

		public Stream InsertNode(Stream stream, int oldOrder, int newOrder, string oldOrderId, string newOrderId, string name, string description) {

			MemoryStream fileStream = new MemoryStream();
			stream.Seek(0, SeekOrigin.Begin);

			try {
				string deletenodeId = oldOrderId;
				string afterNode = newOrderId;

				//file initilaize

				XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";

				XDocument xmlDocument = XDocument.Load(stream);

				//get node
				var current = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => x.Attribute("id").Value == afterNode).ToList();
				var getBeforeNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("targetRef") != null ? x.Attribute("targetRef").Value : "") == afterNode).FirstOrDefault();

				var getAllElement = xmlDocument.Root.Element(bpmn + "process").Elements();

				if (newOrder > oldOrder) {

					var getSequenceNode = xmlDocument.Root.Element(bpmn + "process").Elements().Where(x => (x.Attribute("sourceRef") != null ? x.Attribute("sourceRef").Value : "") == afterNode).FirstOrDefault();

					getAllElement.Where(t => t.Attribute("id").Value == getSequenceNode.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
						   new XElement(bpmn + "userTask", new XAttribute("id", deletenodeId), new XAttribute("name", name), new XAttribute("description", description)),
						  new XElement(bpmn + "sequenceFlow", new XAttribute("id", "sequenceFlow_" + Guid.NewGuid().ToString().Replace("-", "")), new XAttribute("sourceRef", deletenodeId), new XAttribute("targetRef", getSequenceNode.Attribute("targetRef").Value))
					   );

					getAllElement.Where(t => t.Attribute("id").Value == getSequenceNode.Attribute("id").Value).FirstOrDefault().SetAttributeValue("targetRef", deletenodeId);
				} else {
					getAllElement.Where(t => t.Attribute("id").Value == getBeforeNode.Attribute("id").Value).FirstOrDefault().AddAfterSelf(
							new XElement(bpmn + "userTask", new XAttribute("id", deletenodeId), new XAttribute("name", name), new XAttribute("description", description)),
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

		public async System.Threading.Tasks.Task UploadFileToServer(Stream stream, string path) {
			try {
				//stream.CopyTo(ms);
				//stream.Seek(0, SeekOrigin.Begin);
				//ms.Seek(0, SeekOrigin.Begin);

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

			} catch (Exception ex) {
				throw;
			}
		}

		public async Task<Stream> GetFileFromServer(string keyName) {
			Stream stream = new MemoryStream();
			try {
				IAmazonS3 client;
				using (client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1)) {
					GetObjectRequest request = new GetObjectRequest {
						BucketName = "Radial",
						Key = keyName
					};

					using (GetObjectResponse response = await client.GetObjectAsync(request)) {
						//string dir = System.Web.HttpContext.Current.Server.MapPath("~/Areas/CoreProcess/CamundaFiles/");
						//string dest = Path.Combine(dir, keyName.Split('/')[1]);
						//if (!Directory.Exists(dir))
						//    Directory.CreateDirectory(dir);
						//if (!File.Exists(dest))
						//{
						//    response.WriteResponseStreamToFile(dest);
						//}

						//XDocument xmlDocument = XDocument.Load(response.ResponseStream);
						using (var ms = new MemoryStream()) {
							response.ResponseStream.CopyTo(ms);
							ms.Seek(0, SeekOrigin.Begin);
							ms.CopyTo(stream);
							ms.Seek(0, SeekOrigin.Begin);
							stream.Seek(0, SeekOrigin.Begin);
						}

						//XDocument xmlDocument1 = XDocument.Load(stream);
					}
				}
			} catch (Exception ex) {

				throw;
			}
			return stream;

		}


		public async System.Threading.Tasks.Task DeleteFileFromServer(string keyName) {
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

		public List<TaskViewModel> GetTaskListByProcessDefId(UserOrganizationModel caller, List<string> processDefId) {
			var selectedList = new List<TaskViewModel>();
			using (var s = HibernateSession.GetCurrentSession()) {
				var perms = PermissionsUtility.Create(s, caller);
				var getProcessDefList = s.QueryOver<ProcessDef_Camunda>().Where(x => x.DeleteTime == null
				&& x.OrgId == perms.GetCaller().Organization.Id).List();


				CommClass commClass = new CommClass();
				var getTaskList = commClass.GetTaskList(processDefId[0]).ToList();
				foreach (var item in getProcessDefList) {
					var getProcessInstanceList = getTaskList.Where(t => (t.ProcessDefinitionId ?? "").Contains(item.CamundaId)).ToList().Select(x => new TaskViewModel() {
						Id = x.Id,
						name = x.Name ?? ""
					}).ToList();

					if (getProcessInstanceList.Any())
						selectedList.AddRange(getProcessInstanceList);
				}
			}
			return selectedList;
		}
	}
}