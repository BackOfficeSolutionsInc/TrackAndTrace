﻿using System;
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
		public string data { get; set; }
		public string refresh_token { get; set; }
	}

	public class TokenExchangeEndpointResponseDataResponse {
		public long id { get; set; }
		public string name { get; set; }
		public string email { get; set; }
	}
}