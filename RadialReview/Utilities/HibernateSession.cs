using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class HibernateSession
    {
        private static ISessionFactory factory;
        private static String DbFile = null;
        public static void SetDbFile(string file)
        {
            DbFile = file;
        }

        public static ISessionFactory GetDatabaseSessionFactory()
        {
            if (factory == null)
            {
                DbFile = DbFile ?? System.Configuration.ConfigurationManager.AppSettings["DbFile"];
                factory = Fluently.Configure().Database(SQLiteConfiguration.Standard.UsingFile(DbFile))
                .Mappings(m =>
                {
                    m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>();
                    m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                    //m.AutoMappings.Add(CreateAutomappings);
                    //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                }).ExposeConfiguration(BuildSchema)
                .BuildSessionFactory();
            }
            return factory;
        }

        public static ISession GetCurrentSession()
        {
            return GetDatabaseSessionFactory().OpenSession();
        }
        /*
        private static AutoPersistenceModel CreateAutomappings()
        {
            // This is the actual automapping - use AutoMap to start automapping,
            // then pick one of the static methods to specify what to map (in this case
            // all the classes in the assembly that contains Employee), and then either
            // use the Setup and Where methods to restrict that behaviour, or (preferably)
            // supply a configuration instance of your definition to control the automapper.
            return AutoMap
                .AssemblyOf<UserOrganizationModel>(new AutomappingConfiguration())
                .Conventions.Add<CascadeConvention>();
        }*/

        private static void BuildSchema(Configuration config)
        {
            // delete the existing db on each run
            if (!File.Exists(DbFile))
            {
                new SchemaExport(config).Create(false, true);
            }
            else
            {
                new SchemaUpdate(config).Execute(false, true); 
            }

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
        }
        
       
    }
}