using System.ComponentModel.DataAnnotations;

namespace BTL_WEBDEV2025.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Country/Region")]
        public string Country { get; set; } = "Vietnam";

        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 50 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", ErrorMessage = "Password must contain at least one uppercase, one lowercase, and one number")]
        public string? Password { get; set; }

        public bool ShowPassword { get; set; }

        // Registration inline fields when email is not yet registered
        public bool IsNewUser { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [Display(Name = "Accept policies")]
        public bool AcceptPolicy { get; set; }

        // Extra fields for inline registration (to match Join Us step 2)
        [Required(ErrorMessage = "Shopping preference is required")]
        [Display(Name = "Shopping preference")]
        public string? Preference { get; set; }

        [Range(1,31)] public int? BirthDay { get; set; }
        [Range(1,12)] public int? BirthMonth { get; set; }
        [Range(1900,2100)] public int? BirthYear { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Shopping preference")]
        public string Preference { get; set; } = string.Empty;

        [Range(1,31)] public int? BirthDay { get; set; }
        [Range(1,12)] public int? BirthMonth { get; set; }
        [Range(1900,2100)] public int? BirthYear { get; set; }

        [Required]
        [Display(Name = "Accept policies")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to continue")]
        public bool AcceptPolicy { get; set; }
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 200 characters")]
        [Display(Name = "Delivery address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(\+84|0)[0-9]{9,10}$", ErrorMessage = "Please enter a valid Vietnamese phone number")]
        [Display(Name = "Phone number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required")]
        [Display(Name = "Payment method")]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}