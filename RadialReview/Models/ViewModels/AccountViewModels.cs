using RadialReview.Properties;
using System;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public String Email { get; set; }

    }

    public class ResetPasswordWithTokenViewModel
    {
        public String Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public String Password { get; set; }
    }


    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "username",ResourceType=typeof(DisplayNameStrings))]
        public string UserName { get; set; }
    }

    public class ManageUserViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "currentPassword", ResourceType = typeof(DisplayNameStrings))]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessageResourceName = "minCharLength", ErrorMessageResourceType=typeof(ErrorMessageStrings), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "newPassword", ResourceType = typeof(DisplayNameStrings))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]        
        [Display(Name = "confirmPassword", ResourceType = typeof(DisplayNameStrings))]
        [Compare("NewPassword", ErrorMessageResourceName = "newPasswordMatch", ErrorMessageResourceType=typeof(ErrorMessageStrings))]
        public string ConfirmPassword { get; set; }

    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "username", ResourceType = typeof(DisplayNameStrings))]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "password", ResourceType = typeof(DisplayNameStrings))]
        public string Password { get; set; }

        [Display(Name = "rememberMe", ResourceType = typeof(DisplayNameStrings))]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
	    /*
        [Required]
        [Display(Name = "username", ResourceType = typeof(DisplayNameStrings))]
        public string UserName { get; set; }
        */
        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "email", ResourceType = typeof(DisplayNameStrings))]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessageResourceName = "minCharLength",ErrorMessageResourceType=typeof(ErrorMessageStrings), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "password", ResourceType = typeof(DisplayNameStrings))]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "confirmPassword", ResourceType = typeof(DisplayNameStrings))]
        [Compare("Password", ErrorMessageResourceName = "passwordMatch", ErrorMessageResourceType = typeof(ErrorMessageStrings))]
        public string ConfirmPassword { get; set; }

        [Display(Name = "firstName", ResourceType = typeof(DisplayNameStrings))]
        [Required]
        public string fname {get;set;}

        [Display(Name = "lastName", ResourceType = typeof(DisplayNameStrings))]
        [Required]
        public string lname {get;set;}

		public bool IsClient { get; set; }

        public string ReturnUrl { get; set; }

		public string OrganizationName { get; set; }

		public string ProfileImageUrl { get; set; }
	}
}
