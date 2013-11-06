using RadialReview.Properties;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models
{
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
        [Required]
        [Display(Name = "username", ResourceType = typeof(DisplayNameStrings))]
        public string UserName { get; set; }

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
        public string FirstName {get;set;}

        [Display(Name = "lastName", ResourceType = typeof(DisplayNameStrings))]
        [Required]
        public string LastName {get;set;}

        public string ReturnUrl { get; set; }


    }
}
