using RadialReview.Properties;
using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.ViewModels 
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "username", ResourceType = typeof(DisplayNameStrings))]
        public string UserName { get; set; }
    }

    public class ManageUserViewModel
    {
        [Required]
        [DataType(DataType.Password)]

        [Display(Name = "currentPassword", ResourceType = typeof(DisplayNameStrings))]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessageResourceName = "minCharLength",ErrorMessageResourceType=typeof(ErrorMessageStrings), MinimumLength = 6)]
        [DataType(DataType.Password)]

        [Display(Name = "newPassword", ResourceType = typeof(DisplayNameStrings))]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "confirmNewPassword", ResourceType = typeof(DisplayNameStrings))]
        [Compare("NewPassword", ErrorMessageResourceType = typeof(ErrorMessageStrings), ErrorMessageResourceName = "newPasswordMatch")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "email", ResourceType = typeof(DisplayNameStrings))]
        public string Email { get; set; }

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
        [Display(Name = "email", ResourceType = typeof(DisplayNameStrings))]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [Display(Name = "firstName", ResourceType = typeof(DisplayNameStrings))]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "lastName", ResourceType = typeof(DisplayNameStrings))]
        public string LastName { get; set; }
        
        [Required]
        [StringLength(100, ErrorMessageResourceName = "minCharLength",ErrorMessageResourceType=typeof(ErrorMessageStrings), MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "password", ResourceType = typeof(DisplayNameStrings))]
        public string Password { get; set; }
    }
}
