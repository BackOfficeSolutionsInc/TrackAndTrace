using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RadialReview.Models;
using NHibernate;

namespace RadialReview.Tests
{
    public static class TestExtensions
    {

        public static bool AssertException<EXCEPTION>(Action action) where EXCEPTION : Exception
        {
            try
            {
                action();
                Assert.Fail();
            }
            catch (EXCEPTION)
            {
                return true;
            }catch{
                Assert.Fail();
            }
            return false;
        }
    }
}
