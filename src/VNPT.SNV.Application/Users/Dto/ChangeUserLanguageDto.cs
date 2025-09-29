using System.ComponentModel.DataAnnotations;

namespace VNPT.SNV.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}