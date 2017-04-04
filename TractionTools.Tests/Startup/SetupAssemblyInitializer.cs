using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TractionTools.Tests.Startup {
	[TestClass]
	public class SetupAssemblyInitializer {

		[AssemblyInitialize]
		public static void AssemblyInit(TestContext context) {
			// Initalization code goes here
		}
	}
}
