using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace RadialReview {
	public static class ExpressionExtensions {
		public static Expression<Func<TInput, object>> AddBox<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression) {
			// Add the boxing operation, but get a weakly typed expression
			Expression converted = Expression.Convert(expression.Body, typeof(object));
			// Use Expression.Lambda to get back to strong typing
			return Expression.Lambda<Func<TInput, object>>(converted, expression.Parameters);
		}

		public static String GetMvcName<T>(this Expression<Func<T, object>> selector) {
			/*var s = selector.Body;
			var name = new List<string>();
			while (true){
				if (s is UnaryExpression){
					name.Add(((UnaryExpression)s).Operand.;
				}
			}*/
			string p;
			if (selector.Body is UnaryExpression) {
				p = ((UnaryExpression)selector.Body).Operand.ToString();
				return p.Substring(p.IndexOf(".") + 1);
			}
			if (selector.Body is MemberExpression) {
				//p = ((MemberExpression)selector.Body).Member.Name.ToString();
				p = ((MemberExpression)selector.Body).ToString();
				return p.Substring(p.IndexOf(".") + 1);
			}
			throw new Exception("Unhandled");
		}

		public static Type GetMemberType(this LambdaExpression memberSelector) {

			return memberSelector.Body.Type;

			//Func<Expression, Type> nameSelector = null;  //recursive func
			//nameSelector = e => //or move the entire thing to a separate recursive method
			//{
			//	switch (e.NodeType) {
			//		//case ExpressionType.Parameter:
			//		//	return ((ParameterExpression)e).Me;
			//		case ExpressionType.MemberAccess:
			//			return ((MemberExpression)e).Member.ReflectedType;
			//		//case ExpressionType.Call:
			//		//	return ((MethodCallExpression)e).Method.Name;
			//		//case ExpressionType.Convert:
			//		//case ExpressionType.ConvertChecked:
			//		//	return nameSelector(((UnaryExpression)e).Operand);
			//		//case ExpressionType.Invoke:
			//		//	return nameSelector(((InvocationExpression)e).Expression);
			//		//case ExpressionType.ArrayLength:
			//		//	return "Length";
			//		default:
			//			throw new Exception("not a proper member selector");
			//	}
			//};
			//return nameSelector(memberSelector.Body);
		}

		//involves recursion
		public static string GetMemberName(this LambdaExpression memberSelector) {
			Func<Expression, string> nameSelector = null;  //recursive func
			nameSelector = e => //or move the entire thing to a separate recursive method
			{
				switch (e.NodeType) {
					case ExpressionType.Parameter:
						return ((ParameterExpression)e).Name;
					case ExpressionType.MemberAccess:
						return ((MemberExpression)e).Member.Name;
					case ExpressionType.Call:
						return ((MethodCallExpression)e).Method.Name;
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
						return nameSelector(((UnaryExpression)e).Operand);
					case ExpressionType.Invoke:
						return nameSelector(((InvocationExpression)e).Expression);
					case ExpressionType.ArrayLength:
						return "Length";
					default:
						throw new Exception("not a proper member selector");
				}
			};

			return nameSelector(memberSelector.Body);
		}


		public static string GetPropertyDisplayName(this LambdaExpression propertyExpression) {
			var memberInfo = GetPropertyInformation(propertyExpression.Body);
			if (memberInfo == null) {
				throw new ArgumentException("No property reference expression was found.", "propertyExpression");
			}

			var attr = GetAttribute<DisplayAttribute>(memberInfo, false);
			if (attr == null || attr.GetName()==null) {
				return SplitCamelCase(memberInfo.Name);
			}
			return attr.GetName();
		}

		public static string GetPropertyDisplayPrompt(this LambdaExpression propertyExpression) {
			var memberInfo = GetPropertyInformation(propertyExpression.Body);
			if (memberInfo == null) {
				throw new ArgumentException("No property reference expression was found.", "propertyExpression");
			}
			var attr = GetAttribute<DisplayAttribute>(memberInfo, false);
			if (attr == null) {
				return "";
			}
			return attr.GetPrompt();
		}

		public static string GetPropertyDisplayDescription(this LambdaExpression propertyExpression) {
			var memberInfo = GetPropertyInformation(propertyExpression.Body);
			if (memberInfo == null) {
				throw new ArgumentException("No property reference expression was found.", "propertyExpression");
			}
			var attr = GetAttribute<DisplayAttribute>(memberInfo, false);
			if (attr == null) {
				return "";
			}
			return attr.GetDescription();
		}

		#region helpers
		public static string SplitCamelCase(string input) {
			return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
		}

		public static T GetAttribute<T>(MemberInfo member, bool isRequired) where T : Attribute {
			var attribute = member.GetCustomAttributes(typeof(T), false).SingleOrDefault();

			if (attribute == null && isRequired) {
				throw new ArgumentException(
					string.Format(CultureInfo.InvariantCulture, "The {0} attribute must be defined on member {1}", typeof(T).Name, member.Name));
			}

			return (T)attribute;
		}

		public static MemberInfo GetPropertyInformation(Expression propertyExpression) {
			//Debug.Assert(propertyExpression != null, "propertyExpression != null");
			MemberExpression memberExpr = propertyExpression as MemberExpression;
			if (memberExpr == null) {
				UnaryExpression unaryExpr = propertyExpression as UnaryExpression;
				if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Convert) {
					memberExpr = unaryExpr.Operand as MemberExpression;
				}
			}

			if (memberExpr != null && memberExpr.Member.MemberType == MemberTypes.Property) {
				return memberExpr.Member;
			}

			return null;
		}
		#endregion

	}
}