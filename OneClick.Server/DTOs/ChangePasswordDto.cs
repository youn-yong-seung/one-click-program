using System.ComponentModel.DataAnnotations;

namespace OneClick.Server.DTOs;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "비밀번호는 최소 6자 이상이어야 합니다.")]
    public string NewPassword { get; set; } = string.Empty;
}
