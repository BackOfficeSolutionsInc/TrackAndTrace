using CamundaCSharpClient.Model.Task;
using NHibernate;
using RadialReview.Utilities;
using RadialReview.Utilities.CoreProcess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RadialReview.Areas.CoreProcess.Models.Process {
    public class TaskViewModel {

        public string Id { get; set; }
        [Required(ErrorMessage = "field is required")]
        public string name { get; set; }
        public string description { get; set; }
        public ProcessViewModel process { get; set; }

        public long[] SelectedMemberId { get; set; }

        public string SelectedMemberName { get; set; }
        public string SelectedIds { get; set; }
        public long? Assignee { get; set; }
        public List<CandidateGroupViewModel> CandidateList { get; set; }

        [Obsolete("Use static initializers instead")]
        public TaskViewModel() {
        }

        public static TaskViewModel Create(ISession s, PermissionsUtility perms, XElement item) {
            var candidateGroupsStr = BpmnUtility.GetAttributeValue(item, "candidateGroups", BpmnUtility.CAMUNDA_NAMESPACE);
            var candidateGroups = BpmnUtility.GetCandidateGroupsModels(s, perms, candidateGroupsStr);
            var tm = CreateMinimal(item);
            tm.SelectedMemberId = candidateGroups.Select(x => x.Id).ToArray();
            tm.SelectedMemberName = string.Join(", ", candidateGroups.Select(x => x.Name));
            tm.CandidateList = candidateGroups;

            return tm;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public static TaskViewModel CreateMinimal(XElement item) {
            return new TaskViewModel {
                Id = BpmnUtility.GetAttributeValue(item, "id"),
                name = BpmnUtility.GetAttributeValue(item, "name"),
                description = BpmnUtility.GetAttributeValue(item, "description"),
            };
        }


        public static TaskViewModel Create(TaskModel model) {
            return new TaskViewModel {
                Id = model.Id,
                name = model.Name,
                description = (string)model.Description,
                Assignee = ProcessAssignee(model.Assignee),
            };
        }
#pragma warning restore CS0618 // Type or member is obsolete

        private static long? ProcessAssignee(string assignee) {
            if (string.IsNullOrEmpty(assignee))
                return null;

            var split = assignee.Split('_');
            if (split.Length != 2 || split[0] != "u") {
                throw new ArgumentOutOfRangeException("unexpected assignee");
            }
            return long.Parse(split[1]);
        }

    }

    public class CandidateGroupViewModel {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
