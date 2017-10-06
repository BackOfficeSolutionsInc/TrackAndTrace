using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using System.Web.Http.Description;

#region DO NOT EDIT, V0
namespace RadialReview.Api.V0
{
	[RoutePrefix("api/v0")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class AdminController : BaseApiController
	{

		// GET: api/Scores/5
		[Route("app/stats")]
		[HttpGet]
		public ApplicationAccessor.AppStat Stats()
		{
			if (!(GetUser().IsRadialAdmin || GetUser().User.IsRadialAdmin))
				throw new PermissionsException();

			return ApplicationAccessor.Stats();

		}
	}
}
#endregion