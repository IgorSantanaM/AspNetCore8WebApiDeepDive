
namespace CourseLibrary.API.Services
{
    public interface IPropertyMappingService
    {
        Dictionary<string, PropertyMappingValue> GetPropertyMappping<TSource, TDestination>();
        public bool ValidaMappingExists<TSource, TDestination>(string fields);
    }
}