using CourseLibrary.API.Services;
using System.Linq.Dynamic.Core;

namespace CourseLibrary.API.Helpers
{
    public static class IQueryableExtensions 
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy,
            Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if(source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if(string.IsNullOrWhiteSpace(orderBy))
            {
                throw new ArgumentNullException(nameof(orderBy));
            }
            if (mappingDictionary == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var orderByString = string.Empty;

            // the orderBy string is separated by ",", so we split it
            var orderByAfterSplit = orderBy.Split(",");

            foreach(var orderByClause in orderByAfterSplit)
            { 
                // trim the orderBy clause, as it might contain leading
                // or trailing spaces. Can't trim the var in foreach,
                // so use another var.
                var trimmedOrderByClause = orderByClause.Trim();

                // if the sort option ends with with " desc", we order
                // descending, otherwise ascending 
                var orderDescending = trimmedOrderByClause.EndsWith(" desc");

                // remove " asc" or " desc" from the orderBy clause, so we
                // get the property name to look for  in the mapping dictionary
                var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ? trimmedOrderByClause :
                    trimmedOrderByClause.Remove(indexOfFirstSpace);

                // find the matching property
                if (!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentException($"Key mapping for {propertyName} is missing.");
                }

                // get the PropertyMappingValue
                var propertyMappingValue = mappingDictionary[propertyName];
                if(propertyMappingValue == null)
                {
                    throw new ArgumentNullException(nameof(propertyMappingValue));
                }
                if (propertyMappingValue.Revert)
                {
                    orderDescending = !orderDescending;
                }
                //Run through the property nmaes
                foreach(var destinationProperty in 
                        propertyMappingValue.DestinationProperties)
                {
                    orderByString = orderByString +
                        (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ", ")
                        + destinationProperty
                        + (orderDescending ? " descending" : " ascending");
                }
            }
            return source.OrderBy(orderByString);
        }
    }
}
