using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Web;

namespace RadialReview.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class IndexAttribute : Attribute
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="IndexAttribute" /> class.
        ///   The index data annotation indicates that the database should contain an index
        ///   on the associated property, either with or without a uniqueness constraint.
        /// </summary>
        /// <param name="name">The index name, usually IX_{property}.</param>
        /// <param name="unique">
        ///   if set to <c>true</c> indicates that the index should have a uniqueness constraint.
        /// </param>
        public IndexAttribute(string name, bool unique = false)
        {
            Name = name;
            IsUnique = unique;
        }

        public string Name { get; private set; }

        public bool IsUnique { get; private set; }
    }

    /// <summary>
    ///   Class IndexInitializer - a database initialization strategy that extends the default implementation by
    ///   allowing index attributes to be applied as data annotations.
    /// </summary>
    /// <typeparam name="T">The DbContext derived type being initialized.</typeparam>
    /// <remarks>
    ///   copied from
    ///   http://stackoverflow.com/questions/8262590/entity-framework-code-first-fluent-api-adding-indexes-to-columns/13144786
    /// </remarks>
    public class IndexInitializer<T> : IDatabaseInitializer<T>  where T : DbContext
    {
        const string CreateIndexQueryTemplate = "CREATE {unique} INDEX {indexName} ON {tableName} ({columnName});";

        public void InitializeDatabase(T context)
        {
            const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
            var indexes = new Dictionary<IndexAttribute, List<string>>();
            string query = string.Empty;

            foreach (PropertyInfo dataSetProperty in
                typeof(T).GetProperties(PublicInstance).Where(p => p.PropertyType.Name == typeof(DbSet<>).Name))
            {
                Type entityType = dataSetProperty.PropertyType.GetGenericArguments().Single();
                var tableAttributes = (TableAttribute[])entityType.GetCustomAttributes(typeof(TableAttribute), false);

                indexes.Clear();
                string tableName = tableAttributes.Length != 0 ? tableAttributes[0].Name : dataSetProperty.Name;

                foreach (PropertyInfo property in entityType.GetProperties(PublicInstance))
                {
                    //var indexAttributes = (IndexAttribute[])property.GetCustomAttributes(typeof(IndexAttribute), false);
                    IEnumerable<IndexAttribute> indexAttributes = GetIndexAttributes(property);
                    var notMappedAttributes =
                        (NotMappedAttribute[])property.GetCustomAttributes(typeof(NotMappedAttribute), false);
                    if (indexAttributes.Count() > 0 && notMappedAttributes.Length == 0)
                    {
                        var columnAttributes =
                            (ColumnAttribute[])property.GetCustomAttributes(typeof(ColumnAttribute), false);

                        foreach (IndexAttribute indexAttribute in indexAttributes)
                        {
                            if (!indexes.ContainsKey(indexAttribute))
                                indexes.Add(indexAttribute, new List<string>());

                            if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                            {
                                string columnName = columnAttributes.Length != 0
                                                        ? columnAttributes[0].Name : property.Name;
                                indexes[indexAttribute].Add(columnName);
                            }
                            else
                                indexes[indexAttribute].Add(
                                    property.PropertyType.Name + "_" + GetKeyName(property.PropertyType));
                        }
                    }
                }

                foreach (IndexAttribute indexAttribute in indexes.Keys)
                {
                    query +=
                        CreateIndexQueryTemplate.Replace("{indexName}", indexAttribute.Name).Replace(
                            "{tableName}", tableName).Replace(
                                "{columnName}", string.Join(", ", indexes[indexAttribute].ToArray())).Replace(
                                    "{unique}", indexAttribute.IsUnique ? "UNIQUE" : string.Empty);
                }
            }

            if (context.Database.CreateIfNotExists())
                context.Database.ExecuteSqlCommand(query);
        }

        /// <summary>
        ///   Gets the index attributes on the specified property and the same property on any associated metadata type.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>IEnumerable{IndexAttribute}.</returns>
        IEnumerable<IndexAttribute> GetIndexAttributes(PropertyInfo property)
        {
            Type entityType = property.DeclaringType;
            var indexAttributes = (IndexAttribute[])property.GetCustomAttributes(typeof(IndexAttribute), false);
            var metadataAttribute =
                entityType.GetCustomAttribute(typeof(MetadataTypeAttribute)) as MetadataTypeAttribute;
            if (metadataAttribute == null)
                return indexAttributes; // No metadata type

            Type associatedMetadataType = metadataAttribute.MetadataClassType;
            PropertyInfo associatedProperty = associatedMetadataType.GetProperty(property.Name);
            if (associatedProperty == null)
                return indexAttributes; // No metadata on the property

            var associatedIndexAttributes =
                (IndexAttribute[])associatedProperty.GetCustomAttributes(typeof(IndexAttribute), false);
            return indexAttributes.Union(associatedIndexAttributes);
        }

        string GetKeyName(Type type)
        {
            PropertyInfo[] propertyInfos =
                type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetCustomAttribute(typeof(KeyAttribute), true) != null)
                    return propertyInfo.Name;
            }
            throw new Exception("No property was found with the attribute Key");
        }
    }
}