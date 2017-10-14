using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities.Hooks;
using System.Linq;
using RadialReview.App_Start;
using RadialReview.Hooks;
using RadialReview;

namespace TractionTools.Tests.Codebase {
	[TestClass]
	public class AllHooksRegistered {
		[TestMethod]
		[TestCategory("Codebase")]
		public void EnsureHooksAreRegistered() {
			var type = typeof(IHook);
			var expectedTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(p => type.IsAssignableFrom(p) && p.IsClass);

			HookConfig.RegisterHooks();
			var allRegistered = HooksRegistry.GetHooks<IHook>();

			var expectedNames = expectedTypes.Select(x => x.FullName).ToList();
			var registeredNames = allRegistered.Select(x => x.GetType().FullName).ToList();

			SetUtility.AssertEqual(expectedNames, registeredNames);

		}
	}
}
