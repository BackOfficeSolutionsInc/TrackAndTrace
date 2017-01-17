﻿using Ionic.Zip;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserModels;
using RadialReview.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.ViewModels
{
    public class CreateUserOrganizationViewModel
    {
		//Creation variables
		#region Creation Variables
		[Required]
		public String FirstName { get; set; }
		[Required]
		public String LastName { get; set; }

		[Required]
		[EmailAddress]
		public String Email { get; set; }

        public bool IsManager { get; set; }
        public long OrgId { get; set; }
        public bool StrictlyHierarchical { get; set; }
        public long? ManagerNodeId { get; set; }
		public bool EvalOnly { get; set; }

		public bool IsClient { get; set; }
		public string ClientOrganizationName { get; set; }
		
		public bool SendEmail { get; set; }
		public long? NodeId { get; set; }
		

		public long? OrgPositionId {
			get {
				if (_OrgPositionId != null && Position != null)
					throw new PermissionsException("Position and PositionId cannot both be set");

				if (Position != null)
					return Position.PositionId;
				return _OrgPositionId;
			}
			set {
				_OrgPositionId = value;
			}
		}
		private long? _OrgPositionId { get; set; }

		#endregion

		//ViewModel Variables
		#region VM Variables
		public UserPositionViewModel Position { get; set; }
        public List<SelectListItem> PotentialManagers { get; set; }
		#endregion
	}

    public class EditUserOrganizationViewModel
    {
        public long UserId { get; set; }
        public bool IsManager { get; set; }
        //public long ManagerId { get; set; }
        public Boolean? ManagingOrganization { get; set; }
		public Boolean CanSetManagingOrganization { get; set; }
		public Boolean? EvalOnly { get; set; }

		//public bool StrictlyHierarchical { get; set; }
		//public List<SelectListItem> PotentialManagers { get; set; }
	}

    public class UserViewModel : ICompletable
    {
        public UserModel User { get; set; }

        public int ReviewToComplete { get; set; }

        public ICompletionModel GetCompletion(bool split = false)
        {
            int complete = 1;
            int total = 1;

	        if (User != null){
		        complete += (User.ImageGuid != null).ToInt();
				total++;
	        }

            return new CompletionModel(complete, total);
        }
    }

    public class UserOrganizationDetails
    {
		public long SelfId { get; set; }
		public UserOrganizationModel User { get; set; }
		public List<String> Responsibilities { get; set; }
		public List<RoleModel> Roles { get; set; }
		public List<RockModel> Rocks { get; set; }
		public List<MeasurableModel> Measurables { get; set; }
		public List<MeasurableModel> AdminMeasurables { get; set; }
		public bool ManagingOrganization { get; set; }
		public bool Editable {
			get
			{
				return ForceEditable || ManagingOrganization || User.GetPersonallyManaging() ||
				       (User.Organization.Settings.ManagersCanEditSelf  && User.Id == SelfId && User.ManagerAtOrganization) ||
					   (User.Organization.Settings.EmployeesCanEditSelf && User.Id == SelfId);
			}
		}
	    public bool ForceEditable { get; set; }

		public bool CanViewRocks { get; set; }

		public bool CanViewMeasurables { get; set; }
	}
}