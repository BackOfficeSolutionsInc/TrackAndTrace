using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Amazon.SimpleDB.Model;
using System.Collections.Generic;

namespace RadialReview.Models.Angular.Base {
	public class BaseStringAngular : IAngularItemString {
		public string Id { get; set; }
		public string Type {
			get { return GetType().Name; }
		}
		public BaseStringAngular() { }
		public BaseStringAngular(string id) {
			Id = id;
		}
		public string Key { get { return this.GetKey(); } }
		public bool CreateOnly { get; set; }
		public bool Hide { get; set; }
		public Dictionary<string, object> _ExtraProperties { get; set; }

		public object GetAngularId() {
			return Id;
		}

		public string GetAngularType() {
			return Type;
		}
	}


	public class BaseAngular : IAngularItem {
		public long Id { get; set; }
		public string Type {
			get { return GetType().Name; }
		}

		public BaseAngular() {}
		public BaseAngular(long id) {
			Id = id;
			
		}
		
		public string Key { get { return this.GetKey(); } }
		public bool CreateOnly { get; set; }
		public bool Hide { get; set; }
		public Dictionary<string, object> _ExtraProperties { get; set; }

		public object GetAngularId() {
			return Id;
		}

		public string GetAngularType() {
			return Type;
		}
	}

	public class Removed {

		//public static T Create<T>(T instance)
		//{
		//    return (T)new Removed<T>(instance).GetTransparentProxy();
		//}

		public static long Long() {
			return long.MaxValue - 1;
		}
		public static T From<T>() where T : IAngularItem, new() {
			var obj = (T)Activator.CreateInstance<T>();
			obj.Id = Long();// Long();
							//obj.Deleted = true;
			return obj;
		}
		public static T FromAngularString<T>() where T : IAngularItemString, new() {
			var obj = (T)Activator.CreateInstance<T>();
			obj.Id = String();// Long();
							//obj.Deleted = true;
			return obj;
		}
		public static DateTime Date() {
			return DateTime.MaxValue - TimeSpan.FromSeconds(1);
		}
		public static decimal Decimal() {
			return decimal.MaxValue + decimal.MinusOne;
		}
		public static string String() {
			return DELETED_KEY;
		}

		public const string DELETED_KEY = "`delete`";
	}
	//public class Removed<T> : RealProxy, IAngularItem
	//{

	//	private readonly T _instance;

	//	public Removed(T instance)
	//		: base(typeof(T))
	//	{
	//		_instance = instance;
	//	}

	//	public static T Create(T instance)
	//	{
	//		return (T)new Removed<T>(instance).GetTransparentProxy();
	//	}

	//	public override IMessage Invoke(IMessage msg)
	//	{
	//		var methodCall = (IMethodCallMessage)msg;
	//		var method = (MethodInfo)methodCall.MethodBase;

	//		try
	//		{
	//			Console.WriteLine("Before invoke: " + method.Name);
	//			var result = method.Invoke(_instance, methodCall.InArgs);
	//			Console.WriteLine("After invoke: " + method.Name);
	//			return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
	//		}
	//		catch (Exception e)
	//		{
	//			Console.WriteLine("Exception: " + e);
	//			if (e is TargetInvocationException && e.InnerException != null)
	//			{
	//				return new ReturnMessage(e.InnerException, msg as IMethodCallMessage);
	//			}

	//			return new ReturnMessage(e, msg as IMethodCallMessage);
	//		}
	//	}

	//       public long Id { get { return 0; } set { } }

	//	public string Type{get { return "Removed"; }}



	//       public bool Hide { get; set; }
	//   }
}