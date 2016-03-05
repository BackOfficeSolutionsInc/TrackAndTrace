using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
	public enum Env
	{
		invalid,
		local_sqlite,
		local_mysql,
		production,
        local_test_sqlite
	}
}