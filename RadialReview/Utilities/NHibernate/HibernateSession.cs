using System.Diagnostics;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using Microsoft.AspNet.Identity.EntityFramework;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Envers;
using NHibernate.Envers.Configuration;
using NHibernate.Envers.Configuration.Attributes;
using NHibernate.Envers.Strategy;
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
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities.NHibernate;
using NHibernate.Envers.Configuration.Fluent;
using FluentConfiguration = NHibernate.Envers.Configuration.Fluent.FluentConfiguration;
using RadialReview.Models.Payments;
using NHibernate.Driver;
using RadialReview.Utilities.Productivity;
using RadialReview.Models.VideoConference;
using RadialReview.Models.Rocks;

//using Microsoft.VisualStudio.Profiler;

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

                    ChromeExtensionComms.SendCommand("dbStart");
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
                                    //SetupAudit(c);
                                    factory = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                                    .Mappings(m =>
                                    {
                                        //m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                        //   .Conventions.Add<StringColumnLengthConvention>();
                                       // m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\sqlite\");
                                        //m.AutoMappings.Add(CreateAutomappings);
                                        //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                                    })
                                   .ExposeConfiguration(SetupAudit)
                                   .ExposeConfiguration(BuildSchema)
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
                                try
                                {
                                    var c = new Configuration();
                                    c.SetInterceptor(new NHSQLInterceptor());
                                    //SetupAudit(c);
                                    factory = Fluently.Configure(c).Database(
                                                MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionLocalMysql"].ConnectionString).ShowSql())
                                       .Mappings(m =>
                                       {
                                           m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                               .Conventions.Add<StringColumnLengthConvention>();
                                           //  m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                           ////m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                           ////m.AutoMappings.Add(CreateAutomappings);
                                           ////m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                                       })
                                       .ExposeConfiguration(SetupAudit)
                                       .ExposeConfiguration(BuildProductionMySqlSchema)
                                       .BuildSessionFactory();
                                }
                                catch (Exception e)
                                {
                                    var mbox = e.Message;
                                    if (e.InnerException != null && e.InnerException.Message != null)
                                        mbox = e.InnerException.Message;

                                    ChromeExtensionComms.SendCommand("dbError",mbox);
                                    throw e;
                                }
                                break;
                            }
                        case Env.production:
                            {
                                var c = new Configuration();
                                //SetupAudit(c);
                                factory = Fluently.Configure(c).Database(
                                            MySQLConfiguration.Standard.Dialect<MySQL5Dialect>().ConnectionString(connectionStrings["DefaultConnectionProduction"].ConnectionString).ShowSql())
                                   .Mappings(m =>
                                   {
                                       m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                           .Conventions.Add<StringColumnLengthConvention>();
                                       //m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\mysql\");
                                       //m.AutoMappings.Add(CreateAutomappings);
                                       //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");
                                   })
                                   .ExposeConfiguration(SetupAudit)
                                   .ExposeConfiguration(BuildProductionMySqlSchema)
                                   .BuildSessionFactory();
                                break;
                            }
                        case Env.local_test_sqlite:
                            {
                                //var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
                                //var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];
                                //DbFile = file;
                                //var connectionString = connectionStrings["DefaultConnectionLocalSqlite"].ConnectionString;
                               // var file = connectionString.Split(new String[] { "Data Source=" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(';')[0];


                                string Path = "C:\\UITests";//Config.GetAppSetting("DBPATH");//System.Environment.CurrentDirectory;
                                if (!Directory.Exists(Path))
                                    Directory.CreateDirectory(Path);
                                DbFile = Path+"\\_testdb.db";
                               // string[] appPath = Path.Split(new string[] { "bin" }, StringSplitOptions.None);
                                AppDomain.CurrentDomain.SetData("DataDirectory", Path);
                                var connectionString = "Data Source=|DataDirectory|\\_testdb.db";
                                //var connectionString = "Data Source =" + Path;
                                try
                                {
                                    var c = new Configuration();
                                    c.SetInterceptor(new NHSQLInterceptor());
                                    //SetupAudit(c);
                                    factory = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
                                    .Mappings(m =>
                                    {
                                        m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
                                           .Conventions.Add<StringColumnLengthConvention>();
                                       // m.FluentMappings.ExportTo(@"C:\Users\Lynnea\Desktop\temp\sqlite\");
                                        //m.AutoMappings.Add(CreateAutomappings);
                                        //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

                                    })
                                   .ExposeConfiguration(SetupAudit)
                                   .ExposeConfiguration(BuildSchema)
                                   .BuildSessionFactory();
                                }
                                catch (Exception e)
                                {
                                    throw e;
                                }
                                break;
//                                try
//                                {
//                                    var c = new Configuration();
//                                    c.SetProperty("connection.release_mode", "on_close")
//                                    .SetProperty("dialect", typeof(SQLiteDialect).AssemblyQualifiedName)
//                                    .SetProperty("connection.driver_class", typeof(SQLite20Driver).AssemblyQualifiedName)
//                                    ;//.SetProperty("connection.connection_string", "data source=:memory:")
////                                    ;//.SetProperty(Environment.ProxyFactoryFactoryClass, typeof(ProxyFactoryFactory).AssemblyQualifiedName);

//                                    //c.SetInterceptor(new NHSQLInterceptor());
//                                    factory = Fluently.Configure(c).Database(SQLiteConfiguration.Standard.ConnectionString("Data Source=:memory:;Version=3;New=True;"))
//                                    .Mappings(m =>
//                                    {
//                                        m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
//                                           .Conventions.Add<StringColumnLengthConvention>();
//                                        m.FluentMappings.ExportTo(@"C:\Users\Lynnea\Desktop\temp\sqlite");
//                                        //m.AutoMappings.Add(CreateAutomappings);
//                                        //m.AutoMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\");

//                                    })
//                                   .ExposeConfiguration(SetupAudit)
//                                   .ExposeConfiguration(BuildSchema)
//                                   .BuildSessionFactory();
//                                }catch (Exception e){
//                                    throw e;
//                                }
                                break;
                            }
                        /*case "connectionString":
                            {
                                factory = Fluently.Configure().
                            }*/
                        default: throw new Exception("No database type");
                    }

                    ChromeExtensionComms.SendCommand("dbComplete");
                     
                }
                // DataCollection.MarkProfile(1);
                return factory;
            }
        }


        public static bool CloseCurrentSession()
        {
            var session = (SessionPerRequest)HttpContext.Current.Items["NHibernateSession"];
            if (session != null)
            {
                if (session.IsOpen)
                {
                    session.Close();
                }

                if (session.WasDisposed)
                {
                    session.GetBackingSession().Dispose();
                }
                HttpContext.Current.Items.Remove("NHibernateSession");
                return true;
            }
            return false;
        }



        public static ISession GetCurrentSession(bool singleSession = true) {
			
			if (singleSession && !(HttpContext.Current == null || HttpContext.Current.Items == null) && HttpContext.Current.Items["IsTest"]==null)
            {
                try
                {
                    var session = (SessionPerRequest)HttpContext.Current.Items["NHibernateSession"];
                    if (session == null)
                    {
                        session = new SessionPerRequest(GetDatabaseSessionFactory().OpenSession()); // Create session, like SessionFactory.createSession()...
                        HttpContext.Current.Items.Add("NHibernateSession", session);
                    }
                    return session;
                }
                catch (Exception)
                {
                    //Something went wrong.. revert
                    //var a = "Error";
                }
            }
			if (!(HttpContext.Current == null || HttpContext.Current.Items == null) && HttpContext.Current.Items["IsTest"] != null)
				return GetDatabaseSessionFactory().OpenSession();

			return new SessionPerRequest(GetDatabaseSessionFactory().OpenSession(),true);
			//GetDatabaseSessionFactory().OpenSession();
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
            if (Config.ShouldUpdateDB()) {
                if (!File.Exists(DbFile)) {
                    new SchemaExport(config).Create(false, true);
                } else {
                    new SchemaUpdate(config).Execute(false, true);
                }
                Config.DbUpdateSuccessful();
            }

            var auditEvents = new AuditEventListener();
            config.EventListeners.PreInsertEventListeners = new IPreInsertEventListener[] { auditEvents };
            config.EventListeners.PreUpdateEventListeners = new IPreUpdateEventListener[] { auditEvents };

            // this NHibernate tool takes a configuration (with mapping info in)
            // and exports a database schema from it
        }



        private static void SetupAudit(Configuration nhConf)
        {

            var enversConf = new FluentConfiguration();
            nhConf.SetEnversProperty(ConfigurationKey.StoreDataAtDelete, true);
            nhConf.SetEnversProperty(ConfigurationKey.AuditStrategyValidityStoreRevendTimestamp, true);
            nhConf.SetEnversProperty(ConfigurationKey.AuditStrategy, typeof(CustomValidityAuditStrategy));


            enversConf.Audit<VtoItem>().ExcludeRelationData(x => x.Vto);
            enversConf.Audit<VtoItem_Bool>().ExcludeRelationData(x => x.Vto);
            enversConf.Audit<VtoItem_String>().ExcludeRelationData(x => x.Vto);
            enversConf.Audit<VtoItem_DateTime>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoItem_Decimal>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<Vto_Rocks>().ExcludeRelationData(x => x.Vto);

			enversConf.Audit<CoreFocusModel>();//.ExcludeRelationData(x => x.Vto);
            enversConf.Audit<MarketingStrategyModel>();//.ExcludeRelationData(x => x.Vto);
            enversConf.Audit<OneYearPlanModel>();//.ExcludeRelationData(x => x.Vto);
            enversConf.Audit<QuarterlyRocksModel>();//.ExcludeRelationData(x => x.Vto);
            enversConf.Audit<ThreeYearPictureModel>();//.ExcludeRelationData(x => x.Vto);
            enversConf.Audit<VtoModel>()
                .ExcludeRelationData(x => x.CoreFocus)
                .ExcludeRelationData(x => x.MarketingStrategy)
                .ExcludeRelationData(x => x.OneYearPlan)
                .ExcludeRelationData(x => x.QuarterlyRocks)
                .ExcludeRelationData(x => x.ThreeYearPicture);

            enversConf.Audit<TodoModel>();
            enversConf.Audit<IssueModel>();
            enversConf.Audit<ScoreModel>();
            enversConf.Audit<MeasurableModel>();
            enversConf.Audit<L10Meeting>();
            enversConf.Audit<L10Recurrence>();

            enversConf.Audit<ClientReviewModel>();
            enversConf.Audit<LongModel>();
            enversConf.Audit<LongTuple>();
            enversConf.Audit<PaymentModel>();
            enversConf.Audit<PaymentPlanModel>();
            enversConf.Audit<InvoiceModel>();
            enversConf.Audit<InvoiceItemModel>();
            enversConf.Audit<QuestionCategoryModel>();
            enversConf.Audit<LocalizedStringModel>();
            enversConf.Audit<LocalizedStringPairModel>();
            enversConf.Audit<ImageModel>();

            enversConf.Audit<PeriodModel>();
            enversConf.Audit<ReviewModel>();
            enversConf.Audit<ReviewsModel>();
			enversConf.Audit<RockModel>();
			enversConf.Audit<Milestone>();

			enversConf.Audit<L10Recurrence.L10Recurrence_Page>()
				.ExcludeRelationData(x => x.L10Recurrence);

			enversConf.Audit<RoleModel>();
            enversConf.Audit<UserOrganizationModel>()
                .ExcludeRelationData(x => x.Groups)
                .ExcludeRelationData(x => x.ManagingGroups)
                .Exclude(x => x.Cache);
            //.ExcludeRelationData(x => x.CustomQuestions);
            enversConf.Audit<PositionDurationModel>();
            enversConf.Audit<QuestionModel>();
            enversConf.Audit<TeamDurationModel>();
            enversConf.Audit<ManagerDuration>();
            enversConf.Audit<OrganizationTeamModel>();
            enversConf.Audit<OrganizationPositionModel>();
            enversConf.Audit<PositionModel>();

            enversConf.Audit<OrganizationModel>();
            enversConf.Audit<ResponsibilityGroupModel>();
            enversConf.Audit<ResponsibilityModel>();
            enversConf.Audit<TempUserModel>();
            //enversConf.Audit<UserLookup>()
            //    .Exclude(x => x.LastLogin);
            enversConf.Audit<UserModel>();
            enversConf.Audit<UserLogin>();
            enversConf.Audit<UserRoleModel>();
            enversConf.Audit<IdentityUserClaim>();

            enversConf.Audit<PaymentSpringsToken>();
            enversConf.Audit<ScheduledTask>();


            enversConf.Audit<Dashboard>();
			enversConf.Audit<TileModel>();

			enversConf.Audit<AbstractVCProvider>();
			enversConf.Audit<ZoomUserLink>();
			enversConf.Audit<WebhookDetails>();
			enversConf.Audit<WebhookEventsSubscription>();

			nhConf.IntegrateWithEnvers(enversConf);
        }


        private static void BuildProductionMySqlSchema(Configuration config)
        {
            var sw = Stopwatch.StartNew();
            //UPDATE DATABASE:
            var updates = new List<string>();
            //Microsoft.VisualStudio.Profiler.DataCollection.MarkProfile(1);

            if (Config.ShouldUpdateDB()) {
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