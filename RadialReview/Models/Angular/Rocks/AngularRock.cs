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
			Owner = new AngularUser(rock.AccountableUser);
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }
	}
}