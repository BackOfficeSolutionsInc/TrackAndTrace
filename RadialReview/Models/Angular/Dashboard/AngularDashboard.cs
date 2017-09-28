﻿using RadialReview.Exceptions;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.CoreProcess;
using RadialReview.Models.Angular.DataType;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Angular.Dashboard {

	public static class AngularTileKeys{
		public static string L10TodoList(long recurrenceId) {
			return "Tile_L10Todo_" + recurrenceId;
		}
		public static string L10IssuesList(long recurrenceId) {
			return "Tile_L10Issues_" + recurrenceId;
		}
		public static string L10RocksList(long recurrenceId) {
			return "Tile_L10Rocks_" + recurrenceId;
		}
		public static string L10IssuesSolvedList(long recurrenceId) {
			return "Tile_L10IssuesSolved_" + recurrenceId;
		}
		public static string L10Scorecard(long recurrenceId) {
			return "Tile_L10Scorecard_" + recurrenceId;
		}
		public static string ErrorTile(long tileId) {
			return "Tile_Error_" + tileId;
		}
	}

	public class AngularTileId<T> : BaseStringAngular {

        public long KeyId { get; set; }
        public string Title { get; set; }
        public T Contents { get; set; }
        public bool HasError { get; set; }
        public string Message { get; set; }

		public AngularTileId(string id) : base(id) {				
		}

        public AngularTileId(long tile, long keyId, string title, string key) : base(key) {
            KeyId = keyId;
            Title = title;
        }

        public static AngularTileId<T> Error(long tile, long keyId, Exception e) {
            var message = "Could not load tile";
            if (e is PermissionsException)
                message = (e as PermissionsException).Message;

            return new AngularTileId<T>(tile, keyId, "Error", AngularTileKeys.ErrorTile(tile)) {
                HasError = true,
                Message = message,
            };
        }
    }

    public class AngularCoreProcessData : BaseAngular {
        public AngularCoreProcessData() :base(-1) { }
        //public AngularCoreProcessData(long userId) : base(userId) {
        //}

        public IEnumerable<AngularTask> Tasks { get; set; }
        public IEnumerable<AngularCoreProcess> Processes { get; set; }
    }

    public class ListDataVM : BaseAngular {
        public string Name { get; set; }
        public IEnumerable<AngularTodo> Todos { get; set; }
        public IEnumerable<AngularTodo> Milestones { get; set; }
        public AngularScorecard Scorecard { get; set; }
        public IEnumerable<AngularRock> Rocks { get; set; }
        public IEnumerable<AngularUser> Members { get; set; }

        public AngularCoreProcessData CoreProcess { get; set; }

        public AngularDateRange date { get; set; }

        public class DateVM {
            public DateTime startDate { get; set; }
            public DateTime endDate { get; set; }
        }

        public IEnumerable<AngularRole> Roles { get; set; }
        public IEnumerable<AngularCompanyValue> CoreValues { get; set; }
        public IEnumerable<AngularNotification> Notifications { get; set; }


        public List<AngularTileId<AngularScorecard>> L10Scorecards { get; set; }
        public List<AngularTileId<List<AngularRock>>> L10Rocks { get; set; }
        public List<AngularTileId<AngularIssuesList>> L10Issues { get; set; }
        public List<AngularTileId<AngularIssuesSolved>> L10SolvedIssues { get; set; }
        public List<AngularTileId<List<AngularTodo>>> L10Todos { get; set; }

        public List<AngularString> LoadUrls { get; set; }

        public ListDataVM(long id) : base(id) {
            L10Scorecards = new List<AngularTileId<AngularScorecard>>();
            L10Rocks = new List<AngularTileId<List<AngularRock>>>();
            L10Issues = new List<AngularTileId<AngularIssuesList>>();
            L10SolvedIssues = new List<AngularTileId<AngularIssuesSolved>>();
            L10Todos = new List<AngularTileId<List<AngularTodo>>>();
            LoadUrls = new List<AngularString>();
            CoreProcess = new AngularCoreProcessData();
        }
    }
}