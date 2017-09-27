﻿using System;
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
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? DueDate { get; set; }
		public bool? Complete { get; set; }
		public RockState? Completion { get; set; }
        public long? RecurrenceRockId { get; set; }
        public bool? VtoRock { get; set; }
        public long? ForceOrder { get; set; }
        public DateTime? CreateTime { get; set; }
	}
}