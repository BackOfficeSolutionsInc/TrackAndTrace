﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using RadialReview.Model.Enums;
using RadialReview.Models.VideoConference;

namespace RadialReview.Models.L10 {
	public enum PrioritizationType {
		[Display(Name = "By Votes")]
		Priority = 1,
		[Display(Name = "By Priority (1, 2 & 3)")]
		Rank = 2,
	}

	public enum L10TeamType {
		// Invalid =0,

		[Display(Name = "Leadership Team")]
		LeadershipTeam = 1,
		[Display(Name = "Departmental Team")]
		DepartmentalTeam = 2,
		[Display(Name = "Other")]
		Other = 100
	}

	public class L10Recurrence : ILongIdentifiable, IDeletable {

		public virtual long Id { get; set; }
		public virtual String Name { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual bool Pristine { get; set; }

		public virtual bool CountDown { get; set; }

		public virtual bool IncludeIndividualTodos { get; set; }
		public virtual bool IncludeAggregateTodoCompletion { get; set; }

		public virtual DayOfWeek? StartOfWeekOverride { get; set; }

		public virtual long? SelectedVideoProviderId { get; set; }
		public virtual AbstractVCProvider SelectedVideoProvider { get; set; }

		public virtual IList<L10Recurrence_Attendee> _DefaultAttendees { get; set; }
		public virtual IList<L10Recurrence_Measurable> _DefaultMeasurables { get; set; }
		public virtual IList<L10Recurrence_Rocks> _DefaultRocks { get; set; }
		public virtual IList<L10Recurrence_VideoConferenceProvider> _VideoConferenceProviders { get; set; }
		//public virtual IList<L10AgendaItem> _AgendaItems { get; set; }

		public virtual List<L10Note> _MeetingNotes { get; set; }
		public virtual long? MeetingInProgress { get; set; }

		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal SegueMinutes { get; set; }
		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal ScorecardMinutes { get; set; }
		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal RockReviewMinutes { get; set; }
		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal HeadlinesMinutes { get; set; }
		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal TodoListMinutes { get; set; }
		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal IDSMinutes { get; set; }
		[Range(typeof(decimal), "0", "500"), DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:0.##}")]
		public virtual decimal ConclusionMinutes { get; set; }
		public virtual long DefaultTodoOwner { get; set; }
		public virtual long? DefaultIssueOwner { get; set; }

		public virtual bool ReverseScorecard { get; set; }

		public virtual long CreatedById { get; set; }
		public virtual List<long> _WhoCanEdit { get; set; }
		public virtual string VideoId { get; set; }
		public virtual long VtoId { get; set; }
		public virtual string OrderIssueBy { get; set; }
		public virtual bool EnableTranscription { get; set; }

		public virtual string HeadlinesId { get; set; }
		[Obsolete("Do not use", false)]
		public virtual bool ShowHeadlinesBox { get; set; }
		public virtual PeopleHeadlineType HeadlineType { get; set; }

		public virtual L10TeamType TeamType { get; set; }
		public virtual bool IsLeadershipTeam { get; set; }
		public virtual bool CombineRocks { get; set; }

		public virtual PrioritizationType Prioritization { get; set; }


		public L10Recurrence() {
			VideoId = Guid.NewGuid().ToString();
			HeadlinesId = Guid.NewGuid().ToString();
			SegueMinutes = 5;
			ScorecardMinutes = 5;
			RockReviewMinutes = 5;
			HeadlinesMinutes = 5;
			TodoListMinutes = 5;
			IDSMinutes = 60;
			ConclusionMinutes = 5;
			IncludeIndividualTodos = false;
			IncludeAggregateTodoCompletion = false;
			EnableTranscription = false;
			CountDown = true;
			IsLeadershipTeam = true;
			Prioritization = PrioritizationType.Rank;
			ShowHeadlinesBox = false;
			HeadlineType = PeopleHeadlineType.HeadlinesList;
			TeamType = L10TeamType.LeadershipTeam;
			CombineRocks = false;

		}

		public class L10RecurrenceMap : ClassMap<L10Recurrence> {
			public L10RecurrenceMap() {
				Id(x => x.Id);
				Map(x => x.Name).Length(10000);
				Map(x => x.VideoId);
				Map(x => x.CombineRocks);
				Map(x => x.Pristine);
				Map(x => x.ShowHeadlinesBox);
				Map(x => x.HeadlineType);
				Map(x => x.HeadlinesId);
				Map(x => x.CreateTime);
				Map(x => x.MeetingInProgress);
				Map(x => x.DeleteTime);
				Map(x => x.CountDown);
				Map(x => x.StartOfWeekOverride);
				Map(x => x.IsLeadershipTeam);
				Map(x => x.VtoId);
				Map(x => x.EnableTranscription);
				Map(x => x.OrderIssueBy);
				Map(x => x.CreatedById);
				Map(x => x.SegueMinutes);
				Map(x => x.ScorecardMinutes);
				Map(x => x.RockReviewMinutes);
				Map(x => x.HeadlinesMinutes);
				Map(x => x.TodoListMinutes);
				Map(x => x.IDSMinutes);
				Map(x => x.ConclusionMinutes);
				Map(x => x.DefaultTodoOwner);
				Map(x => x.DefaultIssueOwner);
				Map(x => x.ReverseScorecard);
				Map(x => x.IncludeIndividualTodos);
				Map(x => x.IncludeAggregateTodoCompletion);
				Map(x => x.TeamType).CustomType<L10TeamType>();
				Map(x => x.Prioritization).CustomType<PrioritizationType>();
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.SelectedVideoProviderId).Column("SelectedVideoProviderId");
				References(x => x.SelectedVideoProvider).Column("SelectedVideoProviderId").LazyLoad().Nullable().ReadOnly();
			}
		}

		public class L10Recurrence_Rocks : ILongIdentifiable, IHistorical, IOneToMany {
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual RockModel ForRock { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }
			public L10Recurrence_Rocks() {
				CreateTime = DateTime.UtcNow;
			}

			public class L10Recurrence_RocksMap : ClassMap<L10Recurrence_Rocks> {
				public L10Recurrence_RocksMap() {
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					References(x => x.L10Recurrence).Column("L10RecurrenceId");
					References(x => x.ForRock).Column("RockId");
				}
			}

			public virtual object UniqueKey() {
				return Tuple.Create(ForRock.Id, L10Recurrence.Id, DeleteTime);
			}
		}

		public class L10Recurrence_Attendee : ILongIdentifiable, IDeletable, IOneToMany {
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }

			public L10Recurrence_Attendee() {
				CreateTime = DateTime.UtcNow;
			}
			public class L10Recurrence_AttendeeMap : ClassMap<L10Recurrence_Attendee> {
				public L10Recurrence_AttendeeMap() {
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					References(x => x.L10Recurrence).Column("L10RecurrenceId");
					References(x => x.User).Column("UserId");
				}
			}
			public virtual object UniqueKey() {
				return Tuple.Create(User.Id, L10Recurrence.Id, DeleteTime);
			}
		}
		public class L10Recurrence_Measurable : ILongIdentifiable, IDeletable, IOneToMany {
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual MeasurableModel Measurable { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }
			public virtual int _Ordering { get; set; }
			public virtual bool IsDivider { get; set; }
			public L10Recurrence_Measurable() {
				CreateTime = DateTime.UtcNow;
			}
			public class L10Recurrence_MeasurableMap : ClassMap<L10Recurrence_Measurable> {
				public L10Recurrence_MeasurableMap() {
					Id(x => x.Id);
					Map(x => x.IsDivider);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x._Ordering);
					References(x => x.L10Recurrence, "L10RecurrenceId");
					References(x => x.Measurable, "MeasurableId").Nullable();
				}
			}
			public virtual object UniqueKey() {
				return Tuple.Create(Measurable.NotNull(x => x.Id), L10Recurrence.Id, DeleteTime);
			}
			/*Hack: Treats all dividers as the same. WasModfided indicates that the ordering has already been set.*/
			public virtual bool _WasModified { get; set; }
			public virtual bool _Used { get; set; }
		}

		public class L10Recurrence_VideoConferenceProvider : ILongIdentifiable, IDeletable, IOneToMany {
			public virtual long Id { get; set; }
			public virtual DateTime LastUsed { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual AbstractVCProvider Provider { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }
			public virtual String GetName() {
				return Provider.FriendlyName;
			}

			public L10Recurrence_VideoConferenceProvider() {
				CreateTime = DateTime.UtcNow;
			}

			public class L10Recurrence_MeasurableMap : ClassMap<L10Recurrence_VideoConferenceProvider> {
				public L10Recurrence_MeasurableMap() {
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.LastUsed);
					References(x => x.L10Recurrence, "L10RecurrenceId");
					References(x => x.Provider, "VCProviderId").Nullable();
				}
			}

			public virtual object UniqueKey() {
				return Tuple.Create(Provider.NotNull(x => x.Id), L10Recurrence.Id, DeleteTime);
			}
		}
	}
}