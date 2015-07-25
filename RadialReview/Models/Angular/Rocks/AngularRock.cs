using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;

namespace RadialReview.Models.Angular.Meeting
{
	public class AngularRock : BaseAngular
	{
		public AngularRock(RockModel rock) : base(rock.Id)
		{
			Name = rock.Rock;
			Owner = AngularUser.CreateUser(rock.AccountableUser);
			Complete = rock.CompleteTime != null;
			DueDate = rock.DueDate;
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? DueDate { get; set; }
		public bool? Complete { get; set; }
	}
}