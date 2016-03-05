using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Productivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace TractionTools.Tests.TestUtils
{
    public static class TestObjectExtensions
    {
        public class Setter<T>
        {

            private PropertyInfo prop;
            private FieldInfo field;
            private T obj;
            public Setter(T obj, PropertyInfo prop)
            {
                if (prop == null)
                    throw new ArgumentNullException("prop", "PropertyInfo was null");
                this.prop = prop;
                this.obj = obj;
            }
            public Setter(T obj, FieldInfo field)
            {
                if (field == null)
                    throw new ArgumentNullException("field", "FieldInfo was null");
                this.field = field;
                this.obj = obj;
            }
            public void Set<TRef>(TRef value)
            {
                if (prop != null)
                    prop.SetValue(obj, value);
                else if (field != null)
                    field.SetValue(obj, value);
            }
        }
        private static Setter<T> GetSetter<T>(this T obj, string propertyName)
        {
            Setter<T> propInfo = null;
            var type = obj.GetType();
            do
            {
                var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                    propInfo = new Setter<T>(obj, prop);
                else
                {
                    var field = type.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                        propInfo = new Setter<T>(obj, field);
                }
                //propInfo = type.getfie
                type = type.BaseType;
            }
            while (propInfo == null && type != null);
            return propInfo;
        }
        public static void SetValue<T, TRef>(this T obj, string field, TRef value)
        {
            //string p;
            //if (field.Body is UnaryExpression)
            //{
            //    p = ((UnaryExpression)field.Body).Operand.ToString();
            //    p = p.Substring(p.IndexOf(".") + 1);
            //}
            //if (field.Body is MemberExpression)
            //{
            //    //p = ((MemberExpression)selector.Body).Member.Name.ToString();
            //    p = ((MemberExpression)field.Body).ToString();
            //    p = p.Substring(p.IndexOf(".") + 1);
            //}
            var setter = GetSetter(obj, field);
            //var prop = .GetProperties(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setter.Set(value);
        }
        //public static Func<T, R> SetFieldAccessor<T, R>(string fieldName)
        //{
        //    ParameterExpression param = Expression.Parameter(typeof(T), "arg");

        //    MemberExpression member = Expression.Field(param, fieldName);

        //    LambdaExpression lambda = Expression.Lambda(typeof(Func<T, R>), member, param);

        //    Func<T, R> compiled = (Func<T, R>)lambda.Compile();
        //    return compiled;
        //}
    }
    public class BaseTest
    {
        //[ClassInitialize]
        //public void Startup()
        //{
        //    ChromeExtensionComms.SendCommand("testStart");
        //}
        //[ClassCleanup]
        //public void Teardown()
        //{
        //    ChromeExtensionComms.SendCommand("testEnd");
        //}

        private static bool ApplicationCreated;
        protected void MockApplication()
        {
           if (!ApplicationCreated)
                new ApplicationAccessor().EnsureApplicationExists();
           ApplicationCreated = true;
        }
        public void MockHttpContext(){
            HttpContext.Current = new HttpContext(new HttpRequest("", "http://fake.url", ""),new HttpResponse(new StringWriter()));
        }
        public static void Throws<T>(Action func) where T : Exception
        {
            var exceptionThrown = false;
            try{
                func.Invoke();
            }catch (T){
                exceptionThrown = true;
            }

            if (!exceptionThrown)
                throw new AssertFailedException(String.Format("An exception of type {0} was expected, but not thrown", typeof(T)));
            
        }

        public void DbCommit(Action<ISession> sFunc)
        {
            DbExecute(sFunc, true);
        }

        public void DbExecute(Action<ISession> sFunc, bool commit = false)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (s.Connection.ConnectionString != "Data Source=|DataDirectory|\\_testdb.db")
                        throw new Exception("ConnectionString must be 'Data Source=|DataDirectory|\\_testdb.db'");

                    sFunc(s);
                    if (commit)
                    {
                        tx.Commit();
                        s.Flush();
                    }
                }

            }
        }
        private static UserOrganizationModel _AdminUser = null;

        public UserOrganizationModel GetAdminUser()
        {
            if (_AdminUser == null){
                DbCommit(x =>
                {
                    _AdminUser=new UserOrganizationModel(){
                            IsRadialAdmin = true
                        };
                    x.Save(_AdminUser);
                });
            }

            return _AdminUser;
        }

        public UserOrganizationModel GetCaller()
        {
            return new UserOrganizationModel()
            {
                IsRadialAdmin = true
            };
        }
    }
}
