using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;

namespace TractionTools.Tests {
	[TestClass]
	public class ExecuteTasks :BaseTest {
		[TestMethod]
		public async Task TestMultipleWebClients() {
			for (var j = 1; j <= 33; j += 8) {
				var sw = Stopwatch.StartNew();
				var tasks = new List<ScheduledTask>();
				for (int i = 0; i < j; i++) {
					DbCommit(s => {
						var task = new ScheduledTask() {
							Id = i,
							Fire = DateTime.UtcNow.AddSeconds(-10),
							Url = "https://example.com",
							TaskName = "Test"
						};
						s.Save(task);

						tasks.Add(task);
					});
				}
				await TaskAccessor.ExecuteTasks_Test(tasks, DateTime.UtcNow);
				Console.WriteLine(j+"\t"+sw.ElapsedMilliseconds);

			}
		}
	}
}
