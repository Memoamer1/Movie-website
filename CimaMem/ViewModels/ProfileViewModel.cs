using System.ComponentModel.DataAnnotations;

namespace CimaMem.ViewModels;

public class ProfileViewModel
{
    [Display(Name = "اسم المستخدم")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "رابط الصورة الشخصية")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "رقم الهاتف")]
    public string? PhoneNumber { get; set; }

    public bool IsAdmin { get; set; }
}
