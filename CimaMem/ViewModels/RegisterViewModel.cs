using System.ComponentModel.DataAnnotations;

namespace CimaMem.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [Display(Name = "اسم المستخدم")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور {2} حروف على الأقل.", MinimumLength = 6)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    [Compare("Password", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
