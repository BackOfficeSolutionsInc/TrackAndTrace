console.info("Initializing RealTimeHub");
var RealTime = new function () {

	/*
	 * Wraps up the signalR hub. Allows us to join multiple hubs. 
	 * Also allows us to slowly initialize the listeners
	 */

	//Collect methods to run after we're done initializing
	this.afterInitialize = [];

	/*
	 * start the hub up.
	 */
	this.start = function () {
		var args = arguments;
		this.afterInitialize.push(function (s) { s.start.apply(args); });
	};

	/*
	 * join({
	 *	meetingIds: [],
	 *	surveys: [{ surveyContainerId:long?, surveyId:long? },...],
	 *  organizationIds: [],
	 *  vtoIds: []
	 * });
	 */
	this.join = function () {
		var args = arguments;
		this.afterInitialize.push(function (s) { s.join.apply(args); });
	};

	var starting = false;
	var started = false;
	var privateVariables = { joined: false };
	var startQueue = [];
	this.client = {};
	this.server = {};
	this.connectionId = null;
	this.id = null;

	var self = this;
	//Wait until we're actionally loaded
	waitUntil(function () {
		return typeof ($.connection) !== "undefined" && typeof ($.connection.realTimeHub) !== "undefined"; /*&& typeof ($.connection.hub.id) !== "undefined";*/
	}, function () {

		//Copy client methods in the the hub
		for (var i in self.client) {
			if (Object.prototype.hasOwnProperty.call(self.client, i)) {
				$.connection.realTimeHub.client[i] = self.client[i];
			}
		}
		self.client = $.connection.realTimeHub.client;

		//Copy server methods in the the hub
		for (var i in self.server) {
			if (Object.prototype.hasOwnProperty.call(self.server, i)) {
				self.hub.server[i] = self.server[i];
			}
		}
		self.server = $.connection.realTimeHub.server;

		var joinFallbackTimeout;
		//replace the start method.
		self.start = function (callback) {
			if (started) {
				console.warn("already started.");
				return;
			}
			starting = true;
			$.connection.hub.start(Constants.StartHubSettings).done(function () {
				console.log("Connected")
				self.connectionId = $.connection.hub.id;
				self.id = $.connection.hub.id;

				started = true;
				starting = false;
				for (var i = 0; i < startQueue.length; i++) {
					var q = startQueue[i];
					if (typeof (q) === "function") {
						q();
					} else {
						console.error("Expected function in queue. Found" + q);
					}
				}
				startQueue = [];
				if (typeof (callback) !== "undefined") {
					callback();
				}

				joinFallbackTimeout = setTimeout(function () {
					//ensure that we join the default hubs, even if we don't explictly call it.d
					//debugger;
					if (privateVariables.joined == false) {
						console.warn("Fallback: you didn't explicitly call RealTimeHub.join(). I'm calling it for you! ["+(+new Date())+"]");
						self.join({ fallback: true });
					}
				}, 120);
			});
		};


		self.join = function (options, successCallback, failCallback) {
			//deregister fallback.
			privateVariables.joined= true;
			clearTimeout(joinFallbackTimeout);

			var joinMessage = "Joining: ";
			if (options && options.fallback == true)
				joinMessage = "Joining via fallback: ";

			//create the join function
			var func = function () {
				options.connectionId = $.connection.hub.id;
				//debugger;
				console.log(joinMessage, options, "[" + (+new Date()) + "]");
				$.connection.realTimeHub.server.join(options).done(function (e) {
					if (e) {
						if (e.Error) {
							console.error("Error: RealTimeHub.join" + e.Message);
						} else {
							console.log("Success: RealTimeHub.join");
						}
					}
					if (typeof (successCallback) === "function") {
						successCallback(e);
					}
				}).fail(function (d) {
					console.error("Failed: RealTimeHub.join", d);
					if (typeof (failCallback) === "function") {
						failCallback(d);
					}
				});
			};
			//wait until ready, then call the join function.
			performAfterStart(func, true);
		}

		var performAfterStart = function (func, startIfUnstarted) {
			if (typeof (func) !== "function")
				console.warn("Expected a function. Found: " + q)

			if (typeof (startIfUnstarted) === "undefined")
				startIfUnstarted = true;

			if (startIfUnstarted && !started && !starting)
				this.start();

			if (!started) {
				startQueue.push(func);
			} else {
				func();
			}
		}

		self.afterStart = function (func) {
			performAfterStart(func, false);
		};

		//Perform all initialization functions..
		for (var i = 0; i < self.afterInitialize.length; i++) {
			self.afterInitialize[i](self);
		}
	}, function () {
		console.error("Failed to connect to hub.");
	});
};
RealTime.start();

