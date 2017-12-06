using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using System.Runtime.Serialization;

namespace RadialReview.Models.Angular.Rocks
{
	public class AngularRock : BaseAngular
    {
        public AngularRock() { }
        public AngularRock(long rockId):base(rockId) {}

		public AngularRock(L10Meeting.L10Meeting_Rock meetingRock) : this(meetingRock.ForRock, meetingRock.VtoRock) {
		}

		public AngularRock(L10Recurrence.L10Recurrence_Rocks recurRock) : this(recurRock.ForRock, recurRock.VtoRock)
        {
            RecurrenceRockId = recurRock.Id;
        }

		public AngularRock(RockModel rock, bool? vtoRock) : base(rock.Id)
		{
			Name = rock.Rock;
			Owner = AngularUser.CreateUser(rock.AccountableUser);
			Complete = rock.CompleteTime != null;
			DueDate = rock.DueDate;
			Completion = rock.Completion;
			VtoRock = vtoRock;//rock.CompanyRock;
            CreateTime = rock.CreateTime;
			Archived = rock.Archived;
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? DueDate { get; set; }
		public bool? Complete { get; set; }
		public RockState? Completion { get; set; }
        public DateTime? CreateTime { get; set; }
		public bool Archived { get; set; }

		[IgnoreDataMember]
		public long? RecurrenceRockId { get; set; }
		[IgnoreDataMember]
        public bool? VtoRock { get; set; }
		[IgnoreDataMember]
		public long? ForceOrder { get; set; }
	}
}