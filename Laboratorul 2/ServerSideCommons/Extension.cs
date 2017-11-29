using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using Common;

namespace ServerSideCommons
{
    public static class Extension
    {
        public static List<T> ApplyFilters<T>(this List<T> collection, Request request)
        {
            IEnumerable<T> result = collection;
            if (request.FilterBy != null)
            {
                result = collection.FilterBy(request.FilterBy);
            }

            if (request.OrderBy != null)
            {
                var query = request.OrderBy.Property + (request.OrderBy.Descending ? " Descending" : " Ascending");

                result = result.OrderBy(query);
            }

            //if (message.GroupBy != null)
            //{
            //    IEnumerable group = result.GroupBy(message.GroupBy.Property, message.GroupBy.Property);
            //    foreach (IGrouping<string, string> grouping in group)
            //    {

            //    }
            //}

            return result.ToList();

        }

        private static IEnumerable<T> FilterBy<T>(this IEnumerable<T> collection, FilterBy filter)
        {
            var value = filter.Value;
            if (typeof(T).GetProperty(filter.Property).PropertyType == typeof(int))
            {
                value = int.Parse(filter.Value.ToString());
            }

            return collection.Where($"{filter.Property} = @0", value);
        }
    }
}