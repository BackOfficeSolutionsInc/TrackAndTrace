using System.Diagnostics;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Event;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using RadialReview.App_Start;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using RadialReview.Models.Enums;
using RadialReview.Utilities.NHibernate;
using Microsoft.VisualStudio.Profiler;

namespace RadialReview.Utilities
{
    public static class NHSQL
    {       
        public static string NHibernateSQL { get; set; }
    }
    public class NHSQLInterceptor : EmptyInterceptor, IInterceptor
    {
        SqlString IInterceptor.OnPrepareStatement(SqlString sql)
        {
            NHSQL.NHibernateSQL = sql.ToString();
            return sql;
        }
    }

    public class HibernateSession
    {
        private static ISessionFactory factory;
        private static String DbFile = null;
        /*public static void SetDbFile(string file)
        {
            DbFile = file;
        }*/
        private static object lck = new object();
        public static ISession Session { get; set; }
        
        public static ISessionFactory GetDatabaseSessionFactory()
        {
            lock (lck)
            {
                if (factory == null)
                {
                    var config = System.Configuration.ConfigurationManager.AppSettings;
                    var connectionStrings = System.Configuration.ConfigurationManager.ConnectionStrings;
                    switch (Config.GetEnv())
                    {
                        case Env.local_sqlite:
                            {
								var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
                                var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
                                DbFile = file;
                                try
                                {
                                    var c = new Configuration();
                                    c.SetInterceptor(new NHSQLInterceptor());
                                    factory = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                                    .Mappings(m =>
                                    {
										m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										   .Conventions.Add<StringColumnLengthConvention>();
                                        m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\sqlite\");
										//m.AutoMappings.Add(CreateAutomappings);
                                        //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                                    }).ExposeConfiguration(BuildSchema)
                                    .BuildSessionFactory();
                                }
                                catch (Exception e)
                                {
                                    throw e;
                                }
                                break;
                            }
						case Env.local_mysql:
							{
								var c = new Configuration();
								c.SetInterceptor(new NHSQLInterceptor());
								factory = Fluently.Configure(c).Database(
											MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionLocalMysql"].ConnectionString).ShowSql())
								   .Mappings(m =>
								   {
									   m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										   .Conventions.Add<StringColumnLengthConvention>();
									   //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");

									   //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
									   //m.AutoMappings.Add(CreateAutomappings);
									   //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
								   }).ExposeConfiguration(BuildProductionMySqlSchema)
								   .BuildSessionFactory();
								break;
							}
                        case Env.production:
                            {
                                factory = Fluently.Configure().Database(
											MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionProduction"].ConnectionString).ShowSql())
                                   .Mappings(m =>
                                   {
									   m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										   .Conventions.Add<StringColumnLengthConvention>();
                                       //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                       //m.AutoMappings.Add(CreateAutomappings);
                                       //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                                   }).ExposeConfiguration(BuildProductionMySqlSchema)
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
	            DataCollection.MarkProfile(1);
                return factory;
            }
        }

        public static ISession GetCurrentSession()
        {
            return GetDatabaseSessionFactory().OpenSession();
            /*while(true)
            {
                lock (lck)
                {
                    if ( Session == null || !Session.IsOpen )
                    {
                        Session = GetDatabaseSessionFactory().OpenSession();
                        return Session;
                    }
                }
                Thread.Sleep(10);
            }*/
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

			var auditEvents = new AuditEventListener();
			config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
			config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
        }



        private static void BuildProductionMySqlSchema(Configuration config)
        {
	        var sw = Stopwatch.StartNew();
            //UPDATE DATABASE:
            var su = new SchemaUpdate(config);
			su.Execute(false, true);

	        var end =sw.Elapsed;

			var auditEvents = new AuditEventListener();
			config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
			config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };

            //KILL/CREATE DATABASE:
            //new SchemaExport(config).Execute(true, true, false);
			// DELETE THE EXISTING DB ON EACH RUN
            /*if (!File.Exists(DbFile))
            {
                new SchemaExport(config).Create(false, true);
            }
            else
            {
                new SchemaUpdate(config).Execute(false, true);
            }*/

        }


    }
}