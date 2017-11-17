using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.Todo {
    public class TodoVM {
        //private long? _forModelId;
       // private string _forModelType;

        [Required]
        public long MeetingId { get; set; }

        [Required]
        public long ByUserId { get; set; }

        [Required]
        [Display(Name = "To-do")]
        [AllowHtml]
        public String Message { get; set; }

        [Display(Name = "To-do Details")]
        [AllowHtml]
        public string Details { get; set; }

        public long RecurrenceId { get; set; }

        [Required]

        [Display(Name = "Who's Accountable")]
        public long[] AccountabilityId
        {
            get{
                if (_AccountabilityId != null && _AccountabilityId.Length == 1 && _AccountabilityId[0] == 0 && PossibleUsers != null && PossibleUsers.Count >= 1 && PossibleUsers[0] != null)
                    return new[] { PossibleUsers[0].id };
                return _AccountabilityId;
            }
            set{
                _AccountabilityId = value;
            }
        }

        public long[] _AccountabilityId { get; set; }

        public List<AccountableUserVM> PossibleUsers { get; set; }

        public DateTime DueDate { get; set; }

        public long? ForModelId { get; set; }

        public string ForModelType { get; set; }

        public TodoVM() {
            DueDate = DateTime.UtcNow.Date.AddDays(7);
        }

        public TodoVM(long accountableUserId) : this() {
            AccountabilityId = new[] { accountableUserId };

        }
    }

    public class AccountableUserVM {
        public long id { get; set; }
        public string name { get; set; }
        public string imageUrl { get; set; }
    }
}