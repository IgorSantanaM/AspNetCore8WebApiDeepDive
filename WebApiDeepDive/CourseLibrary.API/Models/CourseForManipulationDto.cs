using CourseLibrary.API.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models
{
    [CourseTitleMustBeDifferentFromDescription]
    public abstract class CourseForManipulationDto //:IValidatableObject
    {
        [Required(ErrorMessage = "This field is required")]
        [MaxLength(100, ErrorMessage = "The title must be shorter than `100`")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1500, ErrorMessage = "The description must be shorter than `1500`")]
        public virtual string Description {  get; set; } = string.Empty;

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    if (Title == Description)
        //    {
        //        yield return new ValidationResult("The provided description should be different from the title.",
        //            new[] {"course"});
        //    }
        //}
    }
}
