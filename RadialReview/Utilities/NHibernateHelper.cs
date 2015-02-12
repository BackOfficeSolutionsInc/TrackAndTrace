using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using NHibernate;
using NHibernate.Persister.Entity;

namespace RadialReview.Utilities
{
	/// <summary>
	/// NHibernate helper class
	/// </summary>
	/// <remarks>
	/// Assumes you are using NHibernate version 3.1.0.4000 or greater (Not tested on previous versions)
	/// </remarks>
	public class NHibernateHelper
	{
		
		/// <summary>
		/// Creates a dictionary of property and database column/field name given an
		/// NHibernate mapped entity
		/// </summary>
		/// <remarks>
		/// This method uses reflection to obtain an NHibernate internal private dictionary.
		/// This is the easiest method I know that will also work with entitys that have mapped components.
		/// </remarks>
		/// <param name="sessionFactory">NHibernate SessionFactory</param>
		/// <param name="type">An mapped type</param>
		/// <returns>Entity Property/Database column dictionary</returns>
		public static Dictionary<string, string> GetPropertyAndColumnNames(ISessionFactory sessionFactory, Type type)
		{
			// Get the objects type
			//Type type = entity.GetType();

			// Get the entity's NHibernate metadata
			var metaData = sessionFactory.GetClassMetadata(type.ToString());

			// Gets the entity's persister
			var persister = (AbstractEntityPersister)metaData;

			// Creating our own Dictionary<Entity property name, Database column/filed name>()
			var d = new Dictionary<string, string>();

			// Get the entity's identifier
			var entityIdentifier = metaData.IdentifierPropertyName;

			// Get the database identifier
			// Note: We are only getting the first key column.
			// Adjust this code to your needs if you are using composite keys!
			var databaseIdentifier = persister.KeyColumnNames[0];

			// Adding the identifier as the first entry
			d.Add(entityIdentifier, databaseIdentifier);

			// Using reflection to get a private field on the AbstractEntityPersister class
			var fieldInfo = typeof(AbstractEntityPersister).GetField("subclassPropertyColumnNames", BindingFlags.NonPublic | BindingFlags.Instance);

			// This internal NHibernate dictionary contains the entity property name as a key and
			// database column/field name as the value
			var pairs = (Dictionary<string, string[]>)fieldInfo.GetValue(persister);

			foreach (var pair in pairs)
			{
				if (pair.Value.Length > 0)
				{
					// The database identifier typically appears more than once in the NHibernate dictionary
					// so we are just filtering it out since we have already added it to our own dictionary
					if (pair.Value[0] == databaseIdentifier)
						break;

					d.Add(pair.Key, pair.Value[0]);
				}
			}

			return d;
		}
	}
}