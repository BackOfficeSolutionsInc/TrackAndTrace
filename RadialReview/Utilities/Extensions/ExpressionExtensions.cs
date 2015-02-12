﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.UI.WebControls.Expressions;

namespace RadialReview.Utilities.Extensions
{
	public static class ExpressionExtensions
	{
		public static Expression<Func<TInput, object>> AddBox<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression)
		{
			// Add the boxing operation, but get a weakly typed expression
			Expression converted = Expression.Convert(expression.Body, typeof(object));
			// Use Expression.Lambda to get back to strong typing
			return Expression.Lambda<Func<TInput, object>>(converted, expression.Parameters);
		}

		public static String GetMvcName<T>(this Expression<Func<T, object>> selector)
		{
			var p = ((UnaryExpression) selector.Body).Operand.ToString();
			return p.Substring(p.IndexOf(".") + 1);
		}
	}
}