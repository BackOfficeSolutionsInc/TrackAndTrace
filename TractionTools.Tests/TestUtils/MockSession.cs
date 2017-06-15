using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using Moq;
using System.Collections.Generic;
using System.Linq.Expressions;
using RadialReview.Utilities.DataTypes;
using RadialReview;

namespace TractionTools.Tests.TestUtils {
   
    public class MockSession {
        public Mock<ISession> SessionMock { get; private set; }
        public ISession Session { get { return SessionMock.Object; } }
        public Dictionary<Type,Mock> Lookups { get; private set; }

        public MockSession() {
            SessionMock = new Mock<ISession>();
            Lookups = new Dictionary<Type, Mock>();
        }


        private Mock<IQueryOver<T, T>> GenQueryOverMock<T>() where T : new() {
            var mock = new Mock<IQueryOver<T, T>>();
            mock.Setup(s => s.Where(It.IsAny<Expression<Func<T, bool>>>())).Returns(() => mock.Object);
            return mock;
        }



        public Mock<IQueryOver<T, T>> AddQueryOver<T>() where T: class,new() {
            var queryMock = GenQueryOverMock<T>();
            SessionMock.Setup(s => s.QueryOver<T>()).Returns(queryMock.Object);
            return queryMock;
        }

    }

    public static class SessionMockExtensions {

        private static IEnumerable<T> GenLazyList<T>() where T : new() {
            yield return new T();
            yield return new T();
            yield return new T();
        }

        public static Mock<IQueryOver<T, T>> AddFuture<T>(this Mock<IQueryOver<T, T>> mock, IEnumerable<T> list = null) where T : class, new() {
            mock.Setup(s => s.Future()).Returns(() => list ?? GenLazyList<T>());
            return mock;
        }
    }
}
