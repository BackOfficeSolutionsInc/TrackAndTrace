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
        /*public static void SetDbFile(string file)
        {
            DbFile = file;
        }*/

        public static ISessionFactory GetDatabaseSessionFactory()
        {
            if (factory == null)
            {
                var config = System.Configuration.ConfigurationManager.AppSettings;
                var connectionStrings = System.Configuration.ConfigurationManager.ConnectionStrings;
                var dbType = config["DbType"];
                switch (dbType.ToLower())
                {
                    case "sqlite":
                        {
                            var connectionString = connectionStrings["DefaultConnection"].ConnectionString;
                            var file = connectionString.Split(new String[]{"Data Source="},StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
                            DbFile = file;
                            factory = Fluently.Configure().Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                            .Mappings(m =>
                            {
                                m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>();
                                m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                                //m.AutoMappings.Add(CreateAutomappings);
                                //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                            }).ExposeConfiguration(BuildSchema)
                            .BuildSessionFactory();
                            break;
                        }
                    case "mysql":
                        {
                            factory = Fluently.Configure().Database(
                                        MySQLConfiguration.Standard.ConnectionString(connectionStrings["DefaultConnection"].ConnectionString).ShowSql())
                               .Mappings(m =>
                               {
                                   m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>();
                                   //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                   //m.AutoMappings.Add(CreateAutomappings);
                                   //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                               }).ExposeConfiguration(BuildMySqlSchema)
                               .BuildSessionFactory();
                            break;
                        }
                    /*case "connectionString":
                        {
                            factory = Fluently.Configure().
                        }*/
                    default: throw new Exception("No database type");
                }
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
        private static void BuildMySqlSchema(Configuration config)
        {
            // delete the existing db on each run
            /*if (!File.Exists(DbFile))
            {
                new SchemaExport(config).Create(false, true);
            }
            else
            {
                new SchemaUpdate(config).Execute(false, true);
            }*/

            //Update Database:
            //new SchemaUpdate(config).Execute(true, true);
            
            //Kill/Create Database:
            //new SchemaExport(config).Execute(true, true, false);

        }


    }
}