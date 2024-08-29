using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using System.Runtime.CompilerServices;

namespace CourseLibrary.API.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {
        private readonly Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
        new(StringComparer.OrdinalIgnoreCase)
            {
                {"Id", new(new[] {"Id"}) },
                {"MainCategory", new(new[] {"MainCategory"}) },
                {"Age", new(new[] {"DateOfBirth"}) },
                {"Name", new(new[] {"FirstName","LastName"}) },
            };

        private readonly IList<IPropertyMapping> _propertyMappings;

        public PropertyMappingService()
        {
            _propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }

        public Dictionary<string, PropertyMappingValue> GetPropertyMappping<TSource, TDestination>()
        {
            // get matching mapping 
            var mathingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

            if (mathingMapping.Count() == 1) return mathingMapping.First().MappingDictionary;

            throw new Exception($"Cannot find exact property mapping instance for <{typeof(TSource)}, {typeof(TDestination)}>");

        }
        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMappping<TSource, TDestination>();

            if(string.IsNullOrEmpty(fields)) return true;

            var fieldsAfterSplit = fields.Split(',');

            foreach(var field in fieldsAfterSplit)
            {
                var trimmedField = field.Trim();

                var indexOfFirstSpace = trimmedField.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ? trimmedField :
                    trimmedField.Remove(indexOfFirstSpace);

                if(!propertyMapping.ContainsKey(propertyName))  return false;
            }
            return true;
        }
    }
}
