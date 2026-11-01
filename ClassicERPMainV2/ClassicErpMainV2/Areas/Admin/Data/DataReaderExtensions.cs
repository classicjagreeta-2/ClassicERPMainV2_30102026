using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Web;

namespace ClassicErpMainV2.Areas.Admin.Data
{
    public static class DataReaderExtensions
    {
        public static List<T> MapToList<T>(this DbDataReader reader) where T : new()
        {
            var list = new List<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Map column names to ordinals for efficiency
            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                fieldNames.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                var item = new T();
                foreach (var prop in properties)
                {
                    if (fieldNames.Contains(prop.Name) && !reader.IsDBNull(reader.GetOrdinal(prop.Name)))
                    {
                        var val = reader[prop.Name];
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                        prop.SetValue(item, Convert.ChangeType(val, targetType), null);
                    }
                }
                list.Add(item);
            }

            return list;
        }

        public static T MapToSingle<T>(this DbDataReader reader) where T : class, new()
        {
            var list = reader.MapToList<T>();
            return list.Count > 0 ? list[0] : null;
        }
    }
}