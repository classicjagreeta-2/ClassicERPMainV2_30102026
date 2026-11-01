using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Web;

namespace ClassicErpMainV2.Areas.Admin.Data
{
    public static class DbReaderExtensions
    {
        public static List<T> MapToList<T>(this IDataReader dr) where T : new()
        {
            var list = new List<T>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Map available column names in the reader
            var columns = Enumerable.Range(0, dr.FieldCount)
                                    .Select(i => dr.GetName(i).ToUpperInvariant())
                                    .ToList();

            while (dr.Read())
            {
                var obj = new T();
                foreach (var prop in properties)
                {
                    // Match property name with column name (case-insensitive)
                    string colName = columns.FirstOrDefault(c => c.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(colName) && dr[colName] != DBNull.Value)
                    {
                        object val = dr[colName];

                        // Handle trimming for string values automatically
                        if (val is string strVal)
                            val = strVal.Trim();

                        prop.SetValue(obj, Convert.ChangeType(val, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
                    }
                }
                list.Add(obj);
            }

            return list;
        }

        public static T MapToSingle<T>(this IDataReader reader) where T : class, new()
        {
            var list = reader.MapToList<T>();
            return list.Count > 0 ? list[0] : null;
        }
    }
}