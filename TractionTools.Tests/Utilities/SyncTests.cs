using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Utilities;
using System.Linq;
using RadialReview.Utilities.Synchronize;
using RadialReview;

namespace TractionTools.Tests.Utilities {
	[TestClass]
	public class SyncTests {

		public class Request {
			public string Name { get; set; }
			public DateTime ClientTimestamp { get; set; }
			public TimeSpan FulfilmentTime { get; set; }
		}

		public class Server {
			public string Name { get; set; }
			public TimeSpan OffsetFromClient { get; set; }
		}

		public class SyncSystem {

			public TimeSpan BufferTime { get; set; }

			public DateTime ClientTime { get; set; }
			public SyncSystem(DateTime clientTime, TimeSpan bufferTime) {
				ClientTime = clientTime;
				Requests = new List<Request>();
				Servers = new List<Server>();
				BufferTime = bufferTime;
			}

			public List<Server> Servers { get; set; }
			public List<Request> Requests { get; set; }

			public Server AddServer(string name, TimeSpan offset) {
				var server = new Server() {
					Name = name,
					OffsetFromClient = offset
				};
				if (Servers.Any(x => x.Name == name))
					Assert.Fail("Duplicate server name detected");
				Servers.Add(server);
				return server;
			}

			public Request SendRequest(string name, TimeSpan offset, TimeSpan fulfilmentTime) {
				var request = new Request() {
					Name = name,
					ClientTimestamp = ClientTime.Add(offset),
					FulfilmentTime = fulfilmentTime
				};

				if (Requests.Any(x => x.Name == name))
					Assert.Fail("Duplicate request name detected");

				Requests.Add(request);
				return request;
			}



			public void ClearRequests() {
				Requests = new List<Request>();
			}


			private class Testable {
				public Testable() {
					OrderedActions = new List<Action<Request, Server>>();
				}

				public List<Action<Request, Server>> OrderedActions { get; set; }
			}

			private List<List<Server>> ServerSelections() {
				return _ServerSelectionsRecurse(Requests.Count, new List<Server>()).ToList();
			}

			private List<List<Server>> _ServerSelectionsRecurse(int remainingRequestCount, List<Server> builtList) {
				remainingRequestCount -= 1;
				var results = new List<List<Server>>();
				foreach (var server in Servers) {
					var current = builtList.ToList();
					current.Add(server);
					if (remainingRequestCount > 0)
						results.AddRange(_ServerSelectionsRecurse(remainingRequestCount, current));
					else
						results.Add(current);
				}
				return results;
			}

			public string MakeError(List<Request> requestOrder, List<Server> serverSelection) {

				var builder = "";
				for (var i = 0; i < requestOrder.Count; i++) {
					//Match request with server
					var request = requestOrder[i];
					var server = serverSelection[i];

					builder += request.Name + server.Name + " ";

				}
				return builder.Trim();
			}

			public void EnsureAlwaysOrdered() {
				var count = 0;
				var userId = 1;

				var requestsOrderedByClientSend = Requests.OrderBy(x => x.ClientTimestamp).Select(x => x.Name).ToList();


				//Server receives requests out of order
				var requestOrders = Requests.Permutate().ToList();
				var serverSelections = ServerSelections();

				if (!requestOrders.Any())
					Assert.Fail("No request orders. Did you register any requests?");
				if (!serverSelections.Any())
					Assert.Fail("No server selections. Did you register any servers?");

				//Try all the different orders that requests could be received...
				foreach (var ro in requestOrders) {
					var requestOrder = ro.ToList();
					//at all the different server end points
					foreach (var serverSelection in serverSelections) {
						userId += 1;
						var remainingRequests = requestsOrderedByClientSend.ToList();
						
						using (var s = HibernateSession.GetCurrentSession()) {
							using (var tx = s.BeginTransaction()) {
								//Make all requests
								for (var i = 0; i < requestOrder.Count; i++) {
									//Match request with server
									var request = requestOrder[i];
									var server = serverSelection[i];
									var serverTime = request.ClientTimestamp + request.FulfilmentTime + server.OffsetFromClient;

									var isAfter = SyncUtil.IsStrictlyAfter(s, "_action_name_", request.ClientTimestamp.ToJavascriptMilliseconds(), userId, serverTime, BufferTime);

									if (isAfter == false) {
										if (remainingRequests.Contains(request.Name))
											Assert.Fail("Request should have been allowed. " + MakeError(requestOrder, serverSelection));
									} else {
										if (!remainingRequests.Contains(request.Name))
											Assert.Fail("Request should not have been allowed. " + MakeError(requestOrder, serverSelection));
									}

									//remove requests that happened before me...
									var tempRemaining = new List<string>();
									for (var j = remainingRequests.Count - 1; j >= 0; j--) {
										if (remainingRequests[j] == request.Name)
											break;
										tempRemaining.Insert(0, remainingRequests[j]);
									}
									remainingRequests = tempRemaining;


								}
								tx.Commit();
								s.Flush();
								count += 1;

							}
						}
					}
				}

				Console.WriteLine("Number of combinations tested:" + count + "!");
			}
		}

		[TestMethod]
		[TestCategory("Sync")]
		public void TryOutPermutator() {
			var ss = new SyncSystem(new DateTime(2017, 1, 15), TimeSpan.FromDays(.1));
			ss.AddServer("A", TimeSpan.FromSeconds(5));
			ss.SendRequest("1", TimeSpan.FromSeconds(.1), TimeSpan.FromSeconds(.1));
			ss.EnsureAlwaysOrdered();
		}


		[TestMethod]
		[TestCategory("Sync")]
		public void ConstantFulfilmentTime() {
			var ss = new SyncSystem(new DateTime(2017, 1, 15), TimeSpan.FromSeconds(40));
			ss.AddServer("A", TimeSpan.FromSeconds(5));
			ss.AddServer("B", TimeSpan.FromSeconds(-5));
			ss.AddServer("C", TimeSpan.FromSeconds(100));
			//ss.AddServer("D", TimeSpan.FromSeconds(-100));

			//Send several requests, constant FulfilmentTime
			ss.SendRequest("1", TimeSpan.FromSeconds(.1), TimeSpan.FromSeconds(.1));
			ss.SendRequest("2", TimeSpan.FromSeconds(.2), TimeSpan.FromSeconds(.1));
			ss.SendRequest("3", TimeSpan.FromSeconds(.3), TimeSpan.FromSeconds(.1));
			ss.SendRequest("4", TimeSpan.FromSeconds(.4), TimeSpan.FromSeconds(.1));

			ss.EnsureAlwaysOrdered();
		}

		[TestMethod]
		[TestCategory("Sync")]
		public void NonconstantFulfilmentTime() {
			var ss = new SyncSystem(new DateTime(2017, 1, 15), TimeSpan.FromSeconds(40));
			ss.AddServer("A", TimeSpan.FromSeconds(5));
			ss.AddServer("B", TimeSpan.FromSeconds(-5));
			ss.AddServer("C", TimeSpan.FromSeconds(100));
			ss.AddServer("D", TimeSpan.FromSeconds(-100));

			//Send several requests, constant FulfilmentTime
			ss.SendRequest("1", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(.1));
			ss.SendRequest("2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(50));
			ss.SendRequest("3", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(20));
			//ss.SendRequest("4", TimeSpan.FromSeconds(.4), TimeSpan.FromSeconds(.1));

			ss.EnsureAlwaysOrdered();
		}
	}
}
