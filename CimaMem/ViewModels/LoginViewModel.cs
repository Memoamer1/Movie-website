using System.ComponentModel.DataAnnotations;

namespace CimaMem.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني أو اسم المستخدم مطلوب")]
    [Display(Name = "البريد الإلكتروني أو اسم المستخدم")]
    public string EmailOrUserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "تذكرني")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
