using Moq;
using RadialReview;
using RadialReview.Controllers;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace TractionTools.Tests.TestUtils
{
    public static class ControllerExtensions
    {
        public static void MockUser(this BaseController controller, UserOrganizationModel user)
        {
            controller.SetValue("MockUser", user);
        }

        public static void SetupRequest(this BaseController controller, UserOrganizationModel user)
        {
            controller.MockUser(user);
            var moqRequest = new Mock<HttpRequestBase>();
            var request = new Mock<HttpRequestBase>();
            // Not working - IsAjaxRequest() is static extension method and cannot be mocked
            // request.Setup(x => x.IsAjaxRequest()).Returns(true /* or false */);
            // use this
            //request.SetupGet(x => x.Headers).Returns(
            //    new System.Net.WebHeaderCollection {
            //        {"X-Requested-With", "XMLHttpRequest"}
            //    });

            var context = new Mock<HttpContextBase>();
            context.SetupGet(x => x.Request).Returns(request.Object);

            controller.ControllerContext = new ControllerContext(context.Object, new RouteData(), controller);
        }
    }

}
