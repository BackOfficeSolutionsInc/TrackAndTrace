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
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities.NHibernate;
using NHibernate.Envers.Configuration.Fluent;
using FluentConfiguration = NHibernate.Envers.Configuration.Fluent.FluentConfiguration;
using RadialReview.Models.Payments;

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
										m.FluentMappings.AddFromAssemblyOf<ApplicationWideModel>()
										   .Conventions.Add<StringColumnLengthConvention>();
                                        m.FluentMappings.ExportTo(@"C:\Users\Clay\Desktop\temp\sqlite\");
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
								var c = new Configuration();
								c.SetInterceptor(new NHSQLInterceptor());
								//SetupAudit(c);
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
								   })
								   .ExposeConfiguration(SetupAudit)
								   .ExposeConfiguration(BuildProductionMySqlSchema)
								   .BuildSessionFactory();
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
                        /*case "connectionString":
                            {
                                factory = Fluently.Configure().
                            }*/
                        default: throw new Exception("No database type");
                    }
                }
	           // DataCollection.MarkProfile(1);
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

		

	    private static void SetupAudit(Configuration nhConf)
	    {

			var enversConf = new FluentConfiguration();
			nhConf.SetEnversProperty(ConfigurationKey.StoreDataAtDelete, true);
			nhConf.SetEnversProperty(ConfigurationKey.AuditStrategyValidityStoreRevendTimestamp, true);
            nhConf.SetEnversProperty(ConfigurationKey.AuditStrategy, typeof(CustomValidityAuditStrategy));
			
			
			enversConf.Audit<VtoModel.VtoItem>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel.VtoItem_Bool>().ExcludeRelationData(x=>x.Vto);
			enversConf.Audit<VtoModel.VtoItem_String>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel.VtoItem_DateTime>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel.VtoItem_Decimal>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel>();
			enversConf.Audit<VtoModel.CoreFocusModel>().ExcludeRelationData(x=>x.Vto);
			enversConf.Audit<VtoModel.MarketingStrategyModel>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel.OneYearPlanModel>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel.QuarterlyRocksModel>().ExcludeRelationData(x => x.Vto);
			enversConf.Audit<VtoModel.ThreeYearPictureModel>().ExcludeRelationData(x => x.Vto);

			enversConf.Audit<TodoModel>();
			enversConf.Audit<IssueModel>();
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
			enversConf.Audit<RoleModel>();
		    enversConf.Audit<UserOrganizationModel>()
			    .ExcludeRelationData(x => x.Groups)
			    .ExcludeRelationData(x => x.ManagingGroups);
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
			enversConf.Audit<UserLookup>();
			enversConf.Audit<UserModel>();
			enversConf.Audit<UserLogin>();
            enversConf.Audit<UserRoleModel>();
            enversConf.Audit<IdentityUserClaim>();

			enversConf.Audit<PaymentSpringsToken>();
			enversConf.Audit<ScheduledTask>();

			nhConf.IntegrateWithEnvers(enversConf);
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