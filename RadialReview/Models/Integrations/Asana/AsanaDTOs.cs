using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Integrations.Asana {

	public class AsanaWorkspaceDTO {
		public class Datum {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}
		public List<Datum> data { get; set; }
	}


	public class AsanaProjectDTO {
		public class Datum {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}
		public List<Datum> data { get; set; }
	}


	public class AsanaTaskDTO {

		public Data data { get; set; }
		public class Assignee {
			public long id { get; set; }
			public string name { get; set; }
		}

		public class Follower {
			public long id { get; set; }
			public string name { get; set; }
		}

		public class Workspace {
			public long id { get; set; }
			public string name { get; set; }
		}

		public class Project {
			public long id { get; set; }
			public string name { get; set; }
		}

		public class Data {
			public DateTime created_at { get; set; }
			public string name { get; set; }
			public object parent { get; set; }
			public string completed_at { get; set; }
			public string notes { get; set; }
			public DateTime modified_at { get; set; }
			public string assignee_status { get; set; }
			public Assignee assignee { get; set; }
			public bool completed { get; set; }
			public List<Follower> followers { get; set; }
			public Workspace workspace { get; set; }
			public string due_on { get; set; }
			public string due_at { get; set; }
			public long id { get; set; }
			public List<Project> projects { get; set; }
		}
	}

	public class TokenExchangeEndpointResponse {
		public string access_token { get; set; }
		public int expires_in { get; set; }
		public string token_type { get; set; }
		public Data data { get; set; }
		public string refresh_token { get; set; }

		public class Data {
			public long id { get; set; }
			public string name { get; set; }
			public string email { get; set; }
			//public string gid { get; set; }
		}
	}


	/*
	public class AsanaWebhookEvent {
		public List<Datum> data { get; set; }
		public string sync { get; set; }

		public class Resource {
			public int id { get; set; }
			public string name { get; set; }
		}

		public class User {
			public int id { get; set; }
			public string name { get; set; }
		}

		public class Datum {
			public Resource resource { get; set; }
			public object parent { get; set; }
			public DateTime created_at { get; set; }
			public User user { get; set; }
			public string action { get; set; }
			public string type { get; set; }
		}
	}*/

	public class AsanaWebhookEvent {
		public List<Event> events { get; set; }

		public class Event {
			public string action { get; set; }
			public DateTime created_at { get; set; }
			public object parent { get; set; }
			public long resource { get; set; }
			public string type { get; set; }
			public long user { get; set; }
		}
	}
	

	public class AsanaTask {
		public Data data { get; set; }

		public class Assignee {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}

		public class Follower {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}

		public class Project {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}

		public class Membership {
			public Project project { get; set; }
			public object section { get; set; }
		}

		public class Project2 {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}

		public class Workspace {
			public long id { get; set; }
			public string gid { get; set; }
			public string name { get; set; }
		}

		public class Data {
			public long id { get; set; }
			public string gid { get; set; }
			public Assignee assignee { get; set; }
			public string assignee_status { get; set; }
			public bool completed { get; set; }
			public object completed_at { get; set; }
			public DateTime created_at { get; set; }
			public DateTime due_at { get; set; }
			public string due_on { get; set; }
			public List<Follower> followers { get; set; }
			public bool hearted { get; set; }
			public List<object> hearts { get; set; }
			public bool liked { get; set; }
			public List<object> likes { get; set; }
			public List<Membership> memberships { get; set; }
			public DateTime modified_at { get; set; }
			public string name { get; set; }
			public string notes { get; set; }
			public int num_hearts { get; set; }
			public int num_likes { get; set; }
			public object parent { get; set; }
			public List<Project2> projects { get; set; }
			public object start_on { get; set; }
			public List<object> tags { get; set; }
			public Workspace workspace { get; set; }
		}

	}
}