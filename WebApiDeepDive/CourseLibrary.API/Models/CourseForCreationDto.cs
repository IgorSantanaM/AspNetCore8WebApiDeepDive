using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models;

public class CourseForCreationDto
{
    [Required(ErrorMessage = "This field is required")]
    [MaxLength(100, ErrorMessage = "The title must be shorter than `100`")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1500, ErrorMessage ="The description must be shorter than `1500`")]
    public string? Description { get; set; } = string.Empty;
}