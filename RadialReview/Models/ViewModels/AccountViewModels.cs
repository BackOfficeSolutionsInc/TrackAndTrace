using RadialReview.Properties;
using System;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models
{
	public class PasswordConstants {
		public const string PasswordRegex = @"^(?=.{8,})(?=.*[a-z])(?=.*[A-Z])(?=.*[@#$%^&+=]).*$";
		public const string PasswordRegexError = "Password must have 1 capital letter, 1 lowercase letter, and 1 special character from @#$%^&+=";
		public const string PasswordLengthError = "The {0} must be at least {2} characters long.";
		public const int PasswordMin = 8;
		public const int PasswordMax = 24;

	}

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
		[StringLength(PasswordConstants.PasswordMax, ErrorMessage = PasswordConstants.PasswordLengthError, MinimumLength = PasswordConstants.PasswordMin)]
		[RegularExpression(PasswordConstants.PasswordRegex, ErrorMessage = PasswordConstants.PasswordRegexError)]
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
		[StringLength(PasswordConstants.PasswordMax, ErrorMessage = PasswordConstants.PasswordLengthError, MinimumLength = PasswordConstants.PasswordMin)]
		[RegularExpression(PasswordConstants.PasswordRegex, ErrorMessage = PasswordConstants.PasswordRegexError)]
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
		[StringLength(PasswordConstants.PasswordMax, ErrorMessage = PasswordConstants.PasswordLengthError, MinimumLength = PasswordConstants.PasswordMin)]
		[RegularExpression(PasswordConstants.PasswordRegex, ErrorMessage = PasswordConstants.PasswordRegexError)]
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
