
namespace CourseLibrary.API.Services
{
    public interface IPropertyMappingService
    {
        Dictionary<string, PropertyMappingValue> GetPropertyMappping<TSource, TDestination>();
        public bool ValidMappingExistsFor<TSource, TDestination>(string fields);
    }
}