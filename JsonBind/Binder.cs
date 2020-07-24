using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Codeplex.Data;

namespace JsonBind
{
    public static class Binder
    {
        public static dynamic ReadJsonFile(string file)
        {
            using (var reader = new StreamReader(file))
            {
                var s = reader.ReadToEnd();
                return DynamicJson.Parse(s);
            }
        }

        private static Type GetGenricTypeFromArray(string arrayTypeFullName)
        {
            int start = arrayTypeFullName.LastIndexOf("[");
            int end = arrayTypeFullName.LastIndexOf("]");

            var sourceTypeName = arrayTypeFullName.Remove(start, end - start + 1);
            return Type.GetType(sourceTypeName);
        }

        public static dynamic CreateList(Type genericType,dynamic value)
        {
            var listtype = typeof(List<>).MakeGenericType(genericType);
            dynamic list = Activator.CreateInstance(listtype);

            if (genericType.IsPrimitive || genericType == typeof(string))
            {
                foreach (var item in value) { list.Add(item); }
                return list;
            }
            else if (genericType.IsArray)
            {
                var childGenericType = GetGenricTypeFromArray(genericType.FullName);
                foreach (var item in value) { list.Add(CreateList(childGenericType, item)); }
                return list;
            }
            else if (genericType.IsGenericType)
            {
                var childGenericType = genericType.GetGenericTypeDefinition();
                foreach (var item in value) { list.Add(CreateList(childGenericType, item)); }
                return list;
            }
            else
            {
                foreach (var item in value)
                {
                    var clazz = Bind(item, genericType);
                    list.Add(clazz);
                }
                return list;                
            }
        }

        public static object Bind(dynamic json, Type type)
        {
            var instance = Activator.CreateInstance(type);

            instance.GetType().GetProperties()
                .Select(p =>
                {
                    JsonKey attr = p.GetCustomAttributes(false).FirstOrDefault(x => x.GetType() == typeof(JsonKey)) as JsonKey;
                    return new { attr, property = p };
                })
                .Where(x => x.attr != null)
                .ToList()
                .ForEach(pair =>
                {
                    if (json.IsDefined(pair.attr.Name))
                    {
                        dynamic value = json[pair.attr.Name];

                        if (value.GetType().IsPrimitive || value.GetType() == typeof(string))
                        {
                            pair.property.SetValue(instance, Convert.ChangeType(value, pair.property.PropertyType));
                        }
                        else if (pair.property.PropertyType.IsArray)
                        {
                            var genericType = GetGenricTypeFromArray(pair.property.PropertyType.FullName);
                            pair.property.SetValue(instance, CreateList(genericType,value).ToArray());
                        }
                        else if (pair.property.PropertyType.GetInterface(typeof(IEnumerable<>).FullName) != null)
                        {
                            var genericType = pair.property.PropertyType.GetGenericArguments().First();
                            pair.property.SetValue(instance, CreateList(genericType,value));
                        }
                        else
                        {
                            var clazz = Bind(value, pair.property.PropertyType);
                            pair.property.SetValue(instance, clazz);
                        }
                    }
                });

            return instance;
        }

    }
}
