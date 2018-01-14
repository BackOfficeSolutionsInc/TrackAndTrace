using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Utilities;
using System.Linq;
using RadialReview.Utilities.Synchronize;
using RadialReview;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using RadialReview.Models.Synchronize;
using System.Data;
using System.Diagnostics;
using System.Threading;
using TractionTools.Tests.TestUtils;
using RadialReview.Exceptions;

namespace TractionTools.Tests.Utilities {
    [TestClass]
    public class SyncTests : BaseTest {

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

          /*  public void EnsureAlwaysOrdered() {
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
            }*/
        }

        //[TestMethod]
        //[TestCategory("Sync")]
        //public void TryOutPermutator() {
        //    var ss = new SyncSystem(new DateTime(2017, 1, 15), TimeSpan.FromDays(.1));
        //    ss.AddServer("A", TimeSpan.FromSeconds(5));
        //    ss.SendRequest("1", TimeSpan.FromSeconds(.1), TimeSpan.FromSeconds(.1));
        //    ss.EnsureAlwaysOrdered();
        //}


        //[TestMethod]
        //[TestCategory("Sync")]
        //public void ConstantFulfilmentTime() {
        //    var ss = new SyncSystem(new DateTime(2017, 1, 15), TimeSpan.FromSeconds(40));
        //    ss.AddServer("A", TimeSpan.FromSeconds(5));
        //    ss.AddServer("B", TimeSpan.FromSeconds(-5));
        //    ss.AddServer("C", TimeSpan.FromSeconds(100));
        //    //ss.AddServer("D", TimeSpan.FromSeconds(-100));

        //    //Send several requests, constant FulfilmentTime
        //    ss.SendRequest("1", TimeSpan.FromSeconds(.1), TimeSpan.FromSeconds(.1));
        //    ss.SendRequest("2", TimeSpan.FromSeconds(.2), TimeSpan.FromSeconds(.1));
        //    ss.SendRequest("3", TimeSpan.FromSeconds(.3), TimeSpan.FromSeconds(.1));
        //    ss.SendRequest("4", TimeSpan.FromSeconds(.4), TimeSpan.FromSeconds(.1));

        //    ss.EnsureAlwaysOrdered();
        //}

        //[TestMethod]
        //[TestCategory("Sync")]
        //public void NonconstantFulfilmentTime() {
        //    var ss = new SyncSystem(new DateTime(2017, 1, 15), TimeSpan.FromSeconds(40));
        //    ss.AddServer("A", TimeSpan.FromSeconds(5));
        //    ss.AddServer("B", TimeSpan.FromSeconds(-5));
        //    ss.AddServer("C", TimeSpan.FromSeconds(100));
        //    ss.AddServer("D", TimeSpan.FromSeconds(-100));

        //    //Send several requests, constant FulfilmentTime
        //    ss.SendRequest("1", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(.1));
        //    ss.SendRequest("2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(50));
        //    ss.SendRequest("3", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(20));
        //    //ss.SendRequest("4", TimeSpan.FromSeconds(.4), TimeSpan.FromSeconds(.1));

        //    ss.EnsureAlwaysOrdered();
        //}

        [TestMethod]
        public void TestDualOrderedList() {

            var list1 = new[] { "A", "B" };
            var list2 = new[] { "C", "D" };

            var output = PermutationUtil.DualOrderdLists(list1, list2);

            Assert.AreEqual(6, output.Count());

            var flat = output.Select(x => string.Join("", x));

            Assert.AreEqual(6, flat.Count());
            Assert.IsTrue(flat.Contains("ABCD"));
            Assert.IsTrue(flat.Contains("CABD"));
            Assert.IsTrue(flat.Contains("ACBD"));
            Assert.IsTrue(flat.Contains("CDAB"));
            Assert.IsTrue(flat.Contains("ACDB"));
            Assert.IsTrue(flat.Contains("CADB"));
        }

        [TestMethod]
        public async Task TestEnsureAfter() {
            var org = await OrgUtil.CreateOrganization();

            org.Manager.SetClientRequestId("REQUEST_ID_1");
            org.Manager.SetClientTimeStamp(100);
            await SyncUtil.EnsureStrictlyAfter(org.Manager, SyncAction.UpdateVto(1), async s => { });

            try {
                org.Manager.SetClientTimeStamp(99);
                await SyncUtil.EnsureStrictlyAfter(org.Manager, SyncAction.UpdateVto(1), async s => { });
                Assert.Fail();
            } catch (SyncException) {
                //Pass
            } catch (Exception) {
                Assert.Fail();
            }

            org.Manager.SetClientTimeStamp(101);
            await SyncUtil.EnsureStrictlyAfter(org.Manager, SyncAction.UpdateVto(1), async s => { });

            try {
                //Same time stamp
                org.Manager.SetClientTimeStamp(101);
                await SyncUtil.EnsureStrictlyAfter(org.Manager, SyncAction.UpdateVto(1), async s => { });                
                Assert.Fail();
            } catch (SyncException) {
                //Pass
            } catch (Exception) {
                Assert.Fail();
            }

            org.Manager.SetClientRequestId("REQUEST_ID_2");
            org.Manager.SetClientTimeStamp(98);
            await SyncUtil.EnsureStrictlyAfter(org.Manager, SyncAction.UpdateVto(1), async s => { });
        }


        [TestMethod]
        public async Task DbLockingSmokeTest() {
            var runNum = 0;
            while (runNum < 1) {
                var message = new List<string>();
                var errors = new List<string>();
                var count = 0;

                Action timedOut = () => { };

                try {
                    int[] steps = null;
                    int[] currentStep = null;
                    string[] ordering = null;

                    var stepIterator = new Action(async () => {
                        lock (steps) {
                            var cs = currentStep[0];
                            if (cs >= ordering.Length)
                                return;
                            var o = ordering[cs];

                            if ("ABCD".Contains(o))
                                message.Add("\t" + o);
                            else
                                message.Add("\t\t" + o);

                            switch (o) {
                                case "A": steps[0] = 1; break;
                                case "B": steps[0] = 2; break;
                                case "C": steps[0] = 3; break;
                                case "D": steps[0] = 4; break;
                                case "E": steps[1] = 1; break;
                                case "F": steps[1] = 2; break;
                                case "G": steps[1] = 3; break;
                                case "H": steps[1] = 4; break;
                                default:
                                    break;
                            }
                            currentStep[0] += 1;
                        }
                        await Task.Delay(20);
                    });

                    var waitForStep = new Action<int, int>((requestId, stepId) => {
                        var tempTime = DateTime.UtcNow;
                        while (steps[requestId] < stepId) {
                            if (DateTime.UtcNow - tempTime > TimeSpan.FromMilliseconds(100)) {
                                message.Add("\t\t\tInfo: Step " + stepId + " out of requested order for request " + requestId + ".");
                                break;
                            }
                        }
                    });

                    var START_REQUEST = 1;
                    var OPEN_DB = 2;
                    var CLOSE_DB = 3;
                    var END_REQUEST = 4;

                    //var orderings = new List<string[]> { new string[] { "A", "E", "B", "F", "G", "C", "H", "D" } };
                    var orderings = PermutationUtil.DualOrderdLists(new[] { "A", "B", "C", "D" }, new[] { "E", "F", "G", "H" });

                    using (HibernateSession.ClearSessionFactory_Unsafe(Env.local_mysql)) {

                        foreach (var o in orderings) {
                            message.Add("STARTING - " + string.Join("", o));
                            ordering = o.ToArray();
                            steps = new[] { 0, 0 };
                            currentStep = new[] { 0 };

                            var semephoreOpen = new[] { false, false };


                            //   A ========= B ----------- C ======== D
                            //        E ======== F --- G ====== H
                            var syncLockId = "TEST_" + Guid.NewGuid();

                            //Remove session factory to ensure mysql db is used.
                            //var i = 0;
                            var requests = Enumerable.Range(0, 2).Select(i => new Action(async () => {
                                try {
                                    //Wait until request starts
                                    waitForStep(i, START_REQUEST);
                                    //Request is starting
                                    //Request has started
                                    stepIterator();
                                    //while (steps[i] < OPEN_DB) { }
                                    waitForStep(i, OPEN_DB);
                                    //Database opening
                                    var testHooks = new SyncUtil.TestHooks() {
                                        AfterLock = () => {
                                            if (semephoreOpen[(i + 1) % 2]) {
                                                errors.Add("Error: " + string.Join("", o));
                                                message.Add("\t\t\tDUEL SEMAPHORES ERROR");
                                            }
                                            message.Add("\t\t\tSEMAPHORE STARTED:" + i);
                                            semephoreOpen[i] = true;
                                        },
                                        BeforeUnlock = () => {
                                            message.Add("\t\t\tSEMAPHORE ENDING:" + i);
                                            semephoreOpen[i] = false;
                                        },
                                        //LOGGING ONLY
                                        BeforeLock = () => {
                                            message.Add("\t\t\t\t\t\t\t\tSemaphore Starting:" + i + " (order doesn't matter)");
                                        },
                                        AfterUnlock = () => {
                                            message.Add("\t\t\t\t\t\t\t\tSemaphore Ended:" + i + " (order doesn't matter)");
                                        }
                                    };
                                    await SyncUtil.Lock(syncLockId,null, async (s, lck) => {
                                        //Database is opened                   
                                        stepIterator();
                                        waitForStep(i, CLOSE_DB);

                                    }, testHooks);
                                    #region hide
                                    //SyncUtil.GenerateSyncLock(syncLockId);
                                    //using (var s = HibernateSession.GetCurrentSession()) {
                                    //    using (var tx = s.BeginTransaction()) {

                                    //        if (semephoreOpen[(i + 1) % 2]) {
                                    //            errors.Add("Error: " + string.Join("", o));
                                    //            message.Add("DUEL SEMAPHORES ERROR");
                                    //        }
                                    //        message.Add("\tSemaphore Start:" + i);
                                    //        semephoreOpen[i] = true;
                                    //        SyncUtil.Semaphore(s, syncLockId);

                                    //        //Database is opened                   
                                    //        stepIterator();
                                    //        waitForStep(i, CLOSE_DB);
                                    //        //var tempTime = DateTime.UtcNow;
                                    //        //while (steps[i] < CLOSE_DB) {
                                    //        //    if (DateTime.UtcNow - tempTime > TimeSpan.FromMilliseconds(100)) {
                                    //        //        message.Add("Db closed out of requested order.");
                                    //        //        break;
                                    //        //    }
                                    //        //}
                                    //        //Database closeing
                                    //        tx.Commit();
                                    //        s.Flush();
                                    //    }
                                    //}
                                    #endregion
                                    //Database is closed
                                    stepIterator();
                                    waitForStep(i, END_REQUEST);
                                    //while (steps[i] < END_REQUEST) { }
                                    //Request is ending
                                    //Request has ended
                                    stepIterator();
                                } catch (Exception e) {
                                    message.Add("\t\t\tERROR: " + e.Message);
                                }

                            })).ToList();
                            var cancel = new CancellationTokenSource();
                            cancel.Token.ThrowIfCancellationRequested();
                            await Task.WhenAny(Task.WhenAll(
                                Task.Run(() => stepIterator(), cancel.Token),
                                Task.Run(requests[0], cancel.Token),
                                Task.Run(requests[1], cancel.Token)
                                ), Task.Run(async () => {
                                    await Task.Delay(20000);
                                    if (!Debugger.IsAttached) {
                                        timedOut = () => Assert.Fail("Timed out");
                                        message.Add("TIMED OUT");
                                        throw new Exception("Timed out.");
                                        cancel.Cancel();
                                    } else {
                                        message.Add("TIMED OUT (Debugger)");
                                        timedOut = () => Assert.Inconclusive("Timed out");

                                    }
                                }, cancel.Token)
                            );
                            if (cancel.Token.IsCancellationRequested)
                                Assert.Fail();
                            count++;
                        }
                    }
                } finally {
                    Console.WriteLine("Errors");
                    Console.WriteLine(string.Join("\n", errors));
                    Console.WriteLine("Messages");
                    Console.WriteLine(string.Join("\n", message));
                    Assert.IsTrue(!errors.Any(), "There were " + errors.Count + " errors.\n" + string.Join("\n", errors));
                    timedOut();
                }
                runNum += 1;
                Console.WriteLine("Run" + runNum);
                Assert.AreEqual(0, errors.Count, "Run had errors");
                Assert.AreEqual(70, count);

            }
        }


        [TestMethod]
        public void TestDualOrderedListLong() {
            var list1 = new[] { "A", "B", "C", "D" };
            var list2 = new[] { "E", "F", "G", "H" };
            var output = PermutationUtil.DualOrderdLists(list1, list2);
            Assert.AreEqual(70, output.Count()); // 8! /24 /24
            /// 1/(4!) permutations of ABCD are in the order ABCD
            /// 1/(4!) permutations of EFGH are in the order EFGH
            foreach (var o in output) {
                Console.WriteLine("Assert.IsTrue(flat.Contains(\"" + string.Join("", o) + "\");");
            }


            var flat = output.Select(x => string.Join("", x));

            Assert.AreEqual(70, flat.Count());

            Assert.IsTrue(flat.Contains("ABCDEFGH"));
            Assert.IsTrue(flat.Contains("EABCDFGH"));
            Assert.IsTrue(flat.Contains("AEBCDFGH"));
            Assert.IsTrue(flat.Contains("ABECDFGH"));
            Assert.IsTrue(flat.Contains("ABCEDFGH"));
            Assert.IsTrue(flat.Contains("EFABCDGH"));
            Assert.IsTrue(flat.Contains("AEFBCDGH"));
            Assert.IsTrue(flat.Contains("EAFBCDGH"));
            Assert.IsTrue(flat.Contains("ABEFCDGH"));
            Assert.IsTrue(flat.Contains("EABFCDGH"));
            Assert.IsTrue(flat.Contains("AEBFCDGH"));
            Assert.IsTrue(flat.Contains("ABCEFDGH"));
            Assert.IsTrue(flat.Contains("EABCFDGH"));
            Assert.IsTrue(flat.Contains("AEBCFDGH"));
            Assert.IsTrue(flat.Contains("ABECFDGH"));
            Assert.IsTrue(flat.Contains("EFGABCDH"));
            Assert.IsTrue(flat.Contains("AEFGBCDH"));
            Assert.IsTrue(flat.Contains("EAFGBCDH"));
            Assert.IsTrue(flat.Contains("EFAGBCDH"));
            Assert.IsTrue(flat.Contains("ABEFGCDH"));
            Assert.IsTrue(flat.Contains("EABFGCDH"));
            Assert.IsTrue(flat.Contains("AEBFGCDH"));
            Assert.IsTrue(flat.Contains("EFABGCDH"));
            Assert.IsTrue(flat.Contains("AEFBGCDH"));
            Assert.IsTrue(flat.Contains("EAFBGCDH"));
            Assert.IsTrue(flat.Contains("ABCEFGDH"));
            Assert.IsTrue(flat.Contains("EABCFGDH"));
            Assert.IsTrue(flat.Contains("AEBCFGDH"));
            Assert.IsTrue(flat.Contains("ABECFGDH"));
            Assert.IsTrue(flat.Contains("EFABCGDH"));
            Assert.IsTrue(flat.Contains("AEFBCGDH"));
            Assert.IsTrue(flat.Contains("EAFBCGDH"));
            Assert.IsTrue(flat.Contains("ABEFCGDH"));
            Assert.IsTrue(flat.Contains("EABFCGDH"));
            Assert.IsTrue(flat.Contains("AEBFCGDH"));
            Assert.IsTrue(flat.Contains("EFGHABCD"));
            Assert.IsTrue(flat.Contains("AEFGHBCD"));
            Assert.IsTrue(flat.Contains("EAFGHBCD"));
            Assert.IsTrue(flat.Contains("EFAGHBCD"));
            Assert.IsTrue(flat.Contains("EFGAHBCD"));
            Assert.IsTrue(flat.Contains("ABEFGHCD"));
            Assert.IsTrue(flat.Contains("EABFGHCD"));
            Assert.IsTrue(flat.Contains("AEBFGHCD"));
            Assert.IsTrue(flat.Contains("EFABGHCD"));
            Assert.IsTrue(flat.Contains("AEFBGHCD"));
            Assert.IsTrue(flat.Contains("EAFBGHCD"));
            Assert.IsTrue(flat.Contains("EFGABHCD"));
            Assert.IsTrue(flat.Contains("AEFGBHCD"));
            Assert.IsTrue(flat.Contains("EAFGBHCD"));
            Assert.IsTrue(flat.Contains("EFAGBHCD"));
            Assert.IsTrue(flat.Contains("ABCEFGHD"));
            Assert.IsTrue(flat.Contains("EABCFGHD"));
            Assert.IsTrue(flat.Contains("AEBCFGHD"));
            Assert.IsTrue(flat.Contains("ABECFGHD"));
            Assert.IsTrue(flat.Contains("EFABCGHD"));
            Assert.IsTrue(flat.Contains("AEFBCGHD"));
            Assert.IsTrue(flat.Contains("EAFBCGHD"));
            Assert.IsTrue(flat.Contains("ABEFCGHD"));
            Assert.IsTrue(flat.Contains("EABFCGHD"));
            Assert.IsTrue(flat.Contains("AEBFCGHD"));
            Assert.IsTrue(flat.Contains("EFGABCHD"));
            Assert.IsTrue(flat.Contains("AEFGBCHD"));
            Assert.IsTrue(flat.Contains("EAFGBCHD"));
            Assert.IsTrue(flat.Contains("EFAGBCHD"));
            Assert.IsTrue(flat.Contains("ABEFGCHD"));
            Assert.IsTrue(flat.Contains("EABFGCHD"));
            Assert.IsTrue(flat.Contains("AEBFGCHD"));
            Assert.IsTrue(flat.Contains("EFABGCHD"));
            Assert.IsTrue(flat.Contains("AEFBGCHD"));
            Assert.IsTrue(flat.Contains("EAFBGCHD"));


        }
    }
}
