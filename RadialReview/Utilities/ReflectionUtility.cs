using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
namespace RadialReview.Utilities {
    public static class ReflectionUtility {

        public static string MethodName<T>(this Expression<T> expression)
        {
            if (expression.Body is UnaryExpression) {

                var unaryExpression = (UnaryExpression)expression.Body;
                var methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
                var methodCallObject = (ConstantExpression)methodCallExpression.Object;
                var methodInfo = (MethodInfo)methodCallObject.Value;
                return methodInfo.Name;
            } else if (expression.Body is MethodCallExpression) {
                var mcExp = (MethodCallExpression)expression.Body;
                return mcExp.Method.Name;
            }
            throw new Exception("Unhandled:" + expression.Body.GetType().Name);
        }

    }
}
