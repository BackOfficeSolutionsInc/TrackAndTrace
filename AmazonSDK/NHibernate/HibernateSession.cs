using System.Diagnostics;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Envers.Configuration;
using NHibernate.Event;
using NHibernate.SqlCommand;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.IO;
using FluentConfiguration = NHibernate.Envers.Configuration.Fluent.FluentConfiguration;
using RadialReview.Models;
using RadialReview.App_Start;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using System.Linq.Expressions;
using FluentNHibernate.Testing.Values;
using NHibernate.Mapping;
using System.Linq;

namespace AmazonSDK.NHibernate {
    public static class NHSQL {
        public static string NHibernateSQL { get; set; }
    }
    public class NHSQLInterceptor : EmptyInterceptor, IInterceptor {
        SqlString IInterceptor.OnPrepareStatement(SqlString sql) {
            NHSQL.NHibernateSQL = sql.ToString();
            return sql;
        }
    }



    public class HibernateSession {

        public class RuntimeNames {
            private Configuration cfg;

            public RuntimeNames(Configuration cfg) {
                this.cfg = cfg;
            }

            public string ColumnName<T>(Expression<Func<T, object>> property)
                where T : class, new() {
                var accessor = FluentNHibernate.Utils.Reflection
                    .ReflectionHelper.GetAccessor(property);

                var names = accessor.Name.Split('.');

                var classMapping = cfg.GetClassMapping(typeof(T));

                return WalkPropertyChain(classMapping.GetProperty(names.First()), 0, names);
            }

            private string WalkPropertyChain(Property property, int index, string[] names) {
                if (property.IsComposite)
                    return WalkPropertyChain(((Component)property.Value).GetProperty(names[++index]), index, names);

                return property.ColumnIterator.First().Text;
            }

            public string TableName<T>() where T : class, new() {
                return cfg.GetClassMapping(typeof(T)).Table.Name;
            }
        }
        private static Dictionary<string, ISessionFactory> factory = new Dictionary<string, ISessionFactory>();
        private static String DbFile = null;

        private static object lck = new object();
        public static ISession Session { get; set; }

        private static Dictionary<string, RuntimeNames> Names { get; set; }

        public HibernateSession() {
           Names = new Dictionary<string, RuntimeNames>();
        }

        public static RuntimeNames GetRuntimeNames(string connectionNameExt = "") {
            return Names[connectionNameExt];
        }

        public static ISessionFactory GetDatabaseSessionFactory(string connectionNameExt = "") {
            //factory = null;
            lock (lck) {
                if (!factory.ContainsKey(connectionNameExt)) {

                    //ChromeExtensionComms.SendCommand("dbStart");
                    var config = System.Configuration.ConfigurationManager.AppSettings;
                    var connectionStrings = System.Configuration.ConfigurationManager.ConnectionStrings;
                    Configuration c;
                    switch (Config.GetEnv()) {
                        case Env.local_sqlite: {
                                var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
                                var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
                                DbFile = file;
                                try {
                                    c = new Configuration();
                                    c.SetInterceptor(new NHSQLInterceptor());
                                    //SetupAudit(c);
                                    factory[connectionNameExt] = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                                    .Mappings(m => {
                                        //m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                        //   .Conventions.Add<StringColumnLengthConvention>();
                                        // m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\sqlite\");
                                        //m.AutoMappings.Add(CreateAutomappings);
                                        //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                                    })
                                   .ExposeConfiguration(SetupAudit)
                                   .ExposeConfiguration(BuildSchema)
                                   .BuildSessionFactory();
                                } catch (Exception e) {
                                    throw e;
                                }
                                break;
                            }
                        case Env.local_mysql: {
                                try {
                                    c = new Configuration();
                                    c.SetInterceptor(new NHSQLInterceptor());
                                    Console.WriteLine(connectionStrings["DefaultConnectionLocalMysqlScheduler" + connectionNameExt].ConnectionString);
                                    //SetupAudit(c);
                                    factory[connectionNameExt] = Fluently.Configure(c).Database(
                                                MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionLocalMysqlScheduler" + connectionNameExt].ConnectionString).ShowSql())
                                       .Mappings(m => {
                                           if (string.IsNullOrEmpty(connectionNameExt)) {
                                               m.FluentMappings.AddFromAssemblyOf<MessageQueueMap>().Conventions.Add<StringColumnLengthConvention>();
                                           } else {
                                               m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                          .Conventions.Add<StringColumnLengthConvention>();
                                           }
                                           //  m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                           ////m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                           ////m.AutoMappings.Add(CreateAutomappings);
                                           ////m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                                       })
                                       .ExposeConfiguration(SetupAudit)
                                       .ExposeConfiguration(BuildProductionMySqlSchema)
                                       .BuildSessionFactory();
                                } catch (Exception e) {
                                    var mbox = e.Message;
                                    if (e.InnerException != null && e.InnerException.Message != null)
                                        mbox = e.InnerException.Message;
                                    throw e;
                                }
                                break;
                            }
                        case Env.production: {
                                c = new Configuration();
                                //SetupAudit(c);
                                factory[connectionNameExt] = Fluently.Configure(c).Database(
                                            MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionProductionScheduler" + connectionNameExt].ConnectionString).ShowSql())
                                   .Mappings(m => {
                                       if (string.IsNullOrEmpty(connectionNameExt)) {
                                           m.FluentMappings.AddFromAssemblyOf<MessageQueueMap>().Conventions.Add<StringColumnLengthConvention>();
                                       } else {
                                           m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                      .Conventions.Add<StringColumnLengthConvention>();
                                       }
                                       //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                       //m.AutoMappings.Add(CreateAutomappings);
                                       //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                                   })
                                   .ExposeConfiguration(SetupAudit)
                                   .ExposeConfiguration(BuildProductionMySqlSchema)
                                   .BuildSessionFactory();
                                break;
                            }
                        case Env.local_test_sqlite: {

                                string Path = "C:\\UITests";//Config.GetAppSetting("DBPATH");//System.Environment.CurrentDirectory;
                                if (!Directory.Exists(Path))
                                    Directory.CreateDirectory(Path);
                                DbFile = Path + "\\_testdb.db";
                                AppDomain.CurrentDomain.SetData("DataDirectory", Path);
                                var connectionString = "Data Source=|DataDirectory|\\_testdb.db";
                                //var connectionString = "Data Source =" + Path;
                                try {
                                    c = new Configuration();
                                    c.SetInterceptor(new NHSQLInterceptor());
                                    //SetupAudit(c);
                                    factory[connectionNameExt] = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                                    .Mappings(m => {
                                        m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                           .Conventions.Add<StringColumnLengthConvention>();
                                        // m.FluentMappings.ExportTo(@"C:\Users\Lynnea\Desktop\temp\sqlite\");
                                        //m.AutoMappings.Add(CreateAutomappings);
                                        //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                                    })
                                   .ExposeConfiguration(SetupAudit)
                                   .ExposeConfiguration(BuildSchema)
                                   .BuildSessionFactory();
                                } catch (Exception e) {
                                    throw e;
                                }
                                break;
                            }
                        default:
                            throw new Exception("No database type");
                    }

                    Names[connectionNameExt] = new RuntimeNames(c);
                }

                return factory[connectionNameExt];
            }
        }

        public static bool CloseCurrentSession() {
            //var session = (SingleRequestSession)HttpContext.Current.Items["NHibernateSession"];
            //if (session != null)
            //{
            //    if (session.IsOpen)
            //    {
            //        session.Close();
            //    }

            //    if (session.WasDisposed)
            //    {
            //        session.GetBackingSession().Dispose();
            //    }
            //    HttpContext.Current.Items.Remove("NHibernateSession");
            //    return true;
            //}
            return false;
        }



        public static ISession GetCurrentSession(bool singleSession = true, string connectionName = "") {
            return new SingleRequestSession(GetDatabaseSessionFactory(connectionName).OpenSession(), true);
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

        private static void BuildSchema(Configuration config) {
            // delete the existing db on each run
            // if (Config.ShouldUpdateDB()) {
            if (!File.Exists(DbFile)) {
                new SchemaExport(config).Create(false, true);
            } else {
                new SchemaUpdate(config).Execute(false, true);
            }
            // Config.DbUpdateSuccessful();
            // }

            var auditEvents = new AuditEventListener();
            config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
            config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
        }



        private static void SetupAudit(Configuration nhConf) {

            var enversConf = new FluentConfiguration();
            nhConf.SetEnversProperty(ConfigurationKey.StoreDataAtDelete, true);
            nhConf.SetEnversProperty(ConfigurationKey.AuditStrategyValidityStoreRevendTimestamp, true);
            nhConf.SetEnversProperty(ConfigurationKey.AuditStrategy, typeof(CustomValidityAuditStrategy));
            nhConf.IntegrateWithEnvers(enversConf);
        }


        private static void BuildProductionMySqlSchema(Configuration config) {
            var sw = Stopwatch.StartNew();
            //UPDATE DATABASE:
            var updates = new List<string>();
            //Microsoft.VisualStudio.Profiler.DataCollection.MarkProfile(1);

            if (false && Config.ShouldUpdateDB()) {
                var su = new SchemaUpdate(config);
                su.Execute(updates.Add, true);
                Config.DbUpdateSuccessful();
            }
            //Microsoft.VisualStudio.Profiler.DataCollection.MarkProfile(3);

            var end = sw.Elapsed;

            var auditEvents = new AuditEventListener();
            config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
            config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };


            config.SetProperty("command_timeout", "600");
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