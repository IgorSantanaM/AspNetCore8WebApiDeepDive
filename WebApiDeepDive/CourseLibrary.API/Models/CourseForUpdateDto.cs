using System.ComponentModel.DataAnnotations;

namespace CourseLibrary.API.Models
{
    public class CourseForUpdateDto : CourseForManipulationDto
    {
        [Required(ErrorMessage = "The description field is required.")]
        public override string Description { get => base.Description; set => base.Description = value; }
    }
}
