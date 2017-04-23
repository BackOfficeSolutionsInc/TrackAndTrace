using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;

namespace RadialReview.Models.Angular.Rocks
{
	public class AngularRock : BaseAngular
    {
        public AngularRock() { }
        public AngularRock(long id):base(id) {}

        public AngularRock(L10Recurrence.L10Recurrence_Rocks rock) : this(rock.ForRock)
        {
            RecurrenceRockId = rock.Id;
        }

		public AngularRock(RockModel rock) : base(rock.Id)
		{
			Name = rock.Rock;
			Owner = AngularUser.CreateUser(rock.AccountableUser);
			Complete = rock.CompleteTime != null;
			DueDate = rock.DueDate;
			Completion = rock.Completion;
            CompanyRock = rock.CompanyRock;
            CreateTime = rock.CreateTime;
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? DueDate { get; set; }
		public bool? Complete { get; set; }
		public RockState? Completion { get; set; }
        public long? RecurrenceRockId { get; set; }
        public bool? CompanyRock { get; set; }
        public long? ForceOrder { get; set; }
        public DateTime? CreateTime { get; set; }
	}
}