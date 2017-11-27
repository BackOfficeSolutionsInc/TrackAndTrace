angular.module('updaterModule', []).factory('updater', ["$http",function ($http) {

	function applyUpdate(data, status) {
        convertDates(data);
        this._preExtend(data, status);
        baseExtend(this.scope.model, [data], true);
        this._postExtend(data, status);
        this._preDelete(data, status);
        removeDeleted(this.scope.model);
        this._postDelete(data, status);
        this._preResolve(data, status);
        removeDeleted(data);
        resolveRef(this.scope.model, data);
        this._postResolve(this.scope.model, data, status);
    }

    function removeDeleted(model) {
        _removeDeleted(model, {});
    }

    function search(find, model, arr, str) {
        if (typeof (str) === "undefined") {
            find = find.toLowerCase();
            str = "";
        }

        for (var i in model) {
            if (arrayHasOwnIndex(model, i)) {
                var builder = str + "." + i;
                if ((model[i] + "").toLowerCase() == find) {
                    console.log(builder)
                    arr.push(model);
                }
                if (typeof (model[i]) === "object" && i != "parent")
                    search(find, model[i], arr, builder);
            }
        }

    }

    function _continueIfUnseen(item, seen, onUnseen) {

        if (item) {
            if (item.Key) {
                if (!(item.Key in seen)) {
                    seen[item.Key] = true;
                    onUnseen();
                } else {
                }
            } else {
                onUnseen();
            }
        } else {
            onUnseen();
        }

    }

    function _removeDeleted(model, seen) {
        for (var key in model) {
            if (arrayHasOwnIndex(model, key)) {
                if (model[key] == "`delete`")
                    model[key] = null;
                else if (typeof (model[key]) == 'object') {

                    var value = model[key];
                    _continueIfUnseen(value, seen, function () {
                        _removeDeleted(value, seen);
                    });
                }
            }
        }
    }

    function baseExtend(dst, objs, deep, after, lookupFix) {
        var h = dst.$$hashKey;

        //Check if the lookup has the base object
        if (typeof (after) === "undefined") {
            if ("Key" in dst) {
                //throw "Do not box update with AngularUpdate, instead pass the updated object in directly.";
                var lookthrough = objs;
                if (!angular.isArray(objs)) {
                    lookthrough = [objs];
                }
                for (var k in objs) {
                    if (arrayHasOwnIndex(objs, k)) {
                        var o = objs[k];
                        if ("Lookup" in o) {
                            if (dst["Key"] in o["Lookup"]) {
                                var baseObj = o["Lookup"][dst["Key"]];
                                baseExtend(objs[k], [baseObj], deep, true, true);
                                o["Lookup"][dst["Key"]] = null;
                            }
                        }
                    }
                }
            }
        }

        for (var i = 0, ii = objs.length; i < ii; ++i) {
            var obj = objs[i];
            if (!angular.isObject(obj) && !angular.isFunction(obj))
            	continue;
        	try{
        		if (typeof (obj["UT"]) !== "undefined") {//new
        			if (typeof (dst["UT"]) !== "undefined") {//old
        				if (obj["UT"] < dst["UT"]) {
        					console.info("update skipped:" + (+obj["UT"]) + "<" + (+dst["UT"]));
        					continue;
        				}
        			}
        		}
        		if (typeof (dst["UT"]) !== "undefined" && (typeof (obj["UT"]) === "undefined")) {
        			dst["UT"] = null;
        		}
        	} catch (e) {
        		console.error("UT Error",e);        		
        	}

            var keys = Object.keys(obj);
            for (var j = 0, jj = keys.length; j < jj; j++) {
                var key = keys[j];
                var src = obj[key];
                if (deep && angular.isObject(src)) {
                	


                    if (src.AngularList) {
                        //Special AngularList Object
                        if (typeof (lookupFix) !== "undefined" && lookupFix == true) {
                            //skip this step if we're fixing the lookup
                            dst[key] = src;
                        } else {
                            if (src.UpdateMethod == "Add") {
                                if (typeof (dst[key]) === "undefined") {
                                    dst[key] = [];
                                }
                                dst[key] = dst[key].concat(src.AngularList);
                            } else if (src.UpdateMethod == "ReplaceAll") {
                                dst[key] = src.AngularList;
                            } else if (src.UpdateMethod == "ReplaceIfNewer") {
                                var keysList = [];
                                for (var entry in dst[key]) {
                                    if (arrayHasOwnIndex(dst[key], entry)) {
                                        keysList.push(dst[key][entry]["Key"]);
                                    }
                                }
                                for (var entry in src.AngularList) { //Foreach element in src
                                    if (arrayHasOwnIndex(src.AngularList, entry)) {
                                        var loc = keysList.indexOf(src.AngularList[entry]["Key"]);
                                        if (loc != -1) {
                                            dst[key][loc] = src.AngularList[entry];
                                        } else {
                                            if (typeof (dst[key]) === "undefined") {
                                                dst[key] = [];
                                            }
                                            dst[key].push(src.AngularList[entry]);
                                            keysList.push(src.AngularList[entry]["Key"]);
                                        }
                                    }
                                }
                            } else if (src.UpdateMethod == "Remove") {
                                var keysList = [];
                                for (var entry in dst[key]) {
                                    if (arrayHasOwnIndex(dst[key], entry)) {
                                        keysList.push(dst[key][entry]["Key"]);
                                    }
                                }

                                for (var entry in src.AngularList) {
                                    if (arrayHasOwnIndex(src.AngularList, entry)) {
                                    	var entryKey = src.AngularList[entry]["Key"];
                                    	while (true) {
                                    		var index = keysList.indexOf(entryKey);
                                    		if (index != -1) {
                                    			dst[key].splice(index, 1);
                                    			keysList.splice(index, 1);
                                    		} else {
                                    			break;
                                    		}
                                    	}
                                    }
                                }
                            } else {
                                console.error("UpdateMethod unknown:" + src.UpdateMethod);
                            }
                        }
                    } else {
                        if (!angular.isObject(dst[key]))
                            dst[key] = angular.isArray(src) ? [] : {};

                        if (angular.isArray(dst[key])) {
                            if (src.length > 0 && "Key" in src[0]) {
                                var keysList = [];
                                for (var e in dst[key]) {
                                    if (arrayHasOwnIndex(dst[key], e)) {
                                        keysList.push(dst[key][e]["Key"]);
                                    }
                                }
                                for (var e in src) { //Foreach element in src
                                    if (arrayHasOwnIndex(src, e)) {
                                        var loc = keysList.indexOf(src[e]["Key"]);
                                        if (loc != -1) {
                                            dst[key][loc] = src[e];
                                        } else {
                                            dst[key].push(src[e]);
                                            keysList.push(src[e]["Key"]);
                                        }
                                    }
                                }
                            } else {
                                dst[key] = dst[key].concat(src);
                            }
                        } else {
                            if ((typeof (src.Key) !== "undefined" || key == "Lookup") && dst[key].Key == src.Key)
                                baseExtend(dst[key], [src], true, true);
                            else
                                dst[key] = src;
                        }
                    }
                } else {
                    dst[key] = src;
                }
            }
        }
        if (h) {
            dst.$$hashKey = h;
        } else {
            delete dst.$$hashKey;
        }
        return dst;
    }



    //When sending data TO the server
    function convertDatesForServer(obj) {
        _convertDatesForServer(obj, []);
    }

    function _convertDatesForServer(obj, seen) {
        if (typeof (obj) === "undefined" || obj == null)
            return obj;
        for (var key in obj) {
            if (arrayHasOwnIndex(obj, key)) {
                var value = obj[key];
                var type = typeof (value);
                if (obj[key] == null) {
                    //Do nothing
                } else if (obj[key].getDate !== undefined) {
                	obj[key] = Time.toServerTime(obj[key]);//new Date(obj[key].getTime() + tzoffset() * 60 * 1000);
                } else if (type == 'object') {
                    _continueIfUnseen(value, seen, function () {
                        _convertDatesForServer(value, seen);
                    });
                }
            }
        }
    }

    //When parsing data FROM the server
    function convertDates(obj) {
        var arr = {};
        _convertDates(obj, arr);
    }

    function _convertDates(obj, seen) {
        var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
        //var dateRegex2 = /^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$/;
        var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
        for (var key in obj) {
            if (arrayHasOwnIndex(obj, key)) {
                var value = obj[key];
                var type = typeof (value);

                if (obj[key] == null) {
                    //Do nothing
                } else {
                    var isDate = parseJsonDate(value);

                    if (isDate != false) {
                        obj[key] = isDate;
                    } else if (type == 'object') {
                        _convertDates(value, seen);
                    }
                }
            }
        }
    }

    function clearAndApply(data, status) {

        console.log("updater",this.scope);
        this.scope.model = {};
        this.applyUpdate(data, status);
    }

    function resolveRef(model, update) {
        var firstWarn = true;
        function populate(m, u, topLevel) {
            if (m == null)
                return u;

            if (typeof (topLevel) === "undefined")
                topLevel = true;

            if (Array.isArray(u) || (u && u.AngularList)) {
                var out = [];
                var keysList = [];
                for (var e in m) {
                    if (arrayHasOwnIndex(m, e)) {
                        keysList.push(m[e]["Key"]);
                    }
                }

                if (u.AngularList && (u.AngularList.length == 0 || u.AngularList[0].Key) && (m.length == 0 || m[0].Key)) {
                    for (var k in u.AngularList) { //Foreach element in src
                        if (arrayHasOwnIndex(u.AngularList, k)) {
                            var loc = keysList.indexOf(u.AngularList[k]["Key"]);
                            if (loc != -1) {
                                m[loc] = populate(m[loc], u.AngularList[k], false);
                            } else {
                                if (u.UpdateMethod != "Remove") {
                                    m.push(populate(null, u.AngularList[k], false));
                                    keysList.push(u.AngularList[k]["Key"]);
                                }
                            }
                        }
                    }
                } else if ((u.length == 0 || u[0].Key) && (m.length == 0 || m[0].Key)) {
                    for (var k in u) { //Foreach element in src
                        if (arrayHasOwnIndex(u, k)) {
                            var loc = keysList.indexOf(u[k]["Key"]);
                            if (loc != -1) {
                                try {
                                    m[loc] = populate(m[loc], u[k], false);
                                } catch (e) {
                                    debugger;
                                }
                            } else {
                                m.push(populate(null, u[k], false));
                                keysList.push(u[k]["Key"]);
                            }
                        }
                    }
                } else {
                    for (var k in u) {
                        if (arrayHasOwnIndex(u, k)) {
                            populate(m[k], u[k], false);
                        }
                    }
                    if (firstWarn) {
                        console.warn("List items should have keys.");
                        firstWarn = false;
                    }
                }

                return m;
            }
            else if (u && u._P) {
                return model.Lookup[m.Key];
            } else if (angular.isObject(u)) {
                for (var k in u) {
                    if (arrayHasOwnIndex(u, k)) {
                        if (m[k] !== null && m[k].AngularList) {
                            //model had an angular list (probably from Lookup merge with Main container)
                            //Fix m[k]
                            if (m[k].UpdateMethod == "Remove") {
                                m[k] = [];
                            } else if (m[k].UpdateMethod == "Replace" || m[k].UpdateMethod == "ReplaceIfNewer" || m[k].UpdateMethod == "Add") {
                                m[k] = m[k].AngularList;
                            } else {
                                console.error("Unknown update type:" + m[k].UpdateMethod);
                            }
                        }
                        m[k] = populate(m[k], u[k], false);
                    }
                }
                return m;
            } else {
                return u;
            }
        }

        return populate(model, update);
    }


    function constructSendUpdate(useUpdateUrl, options) {
        options = options || {};
        onSuccess = options.onSuccess || function () { };
        onError = options.onError || showAngularError;

        if (typeof (onError) === "string") {
            var errorMsg = onError;
            onError = function () { showAlert(errorMsg); }
        }
        var updateUrl = useUpdateUrl;
        if (typeof (updateUrl) === "undefined") {
            console.warn("Aborting. URL must be either a string or function to construct a SendUpdate method.");
            return function () {
                showAlert("Error. Updated data will not save.");
            };
        }

        return function (self, args) {
            var dat = angular.copy(self);
            //var _clientTimestamp = Time.getTimestamp();
            this.updater.convertDatesForServer(dat, Time.tzoffset());
            var builder = "";
            args = args || {};

            if (!("connectionId" in args))
            	args["connectionId"] = this.hub.connection.id;//$scope.connectionId;

            for (var i in args) {
                if (arrayHasOwnIndex(args, i)) {
                    if (typeof (args[i]) !== "string")
                        console.warn("sendUpdate argument (" + i + ") did not resolve to a string.");
                    builder += "&" + i + "=" + args[i];
                }
            }
            var url = null;
            if (typeof (updateUrl) === "function") {
                url = updateUrl(self);
            }
            if (typeof (url) !== "string") {
                console.error("Aborting sendUpdate. Url did not resolve to a string.");
                return;
            }
            url = Time.addTimestamp(url) + builder;
            $http.post(url, dat).then(onSuccess, onError);
        };
    }

    function updaterFactory($scope, hub) {
        var o = {
            scope: $scope,
            preExtend: function (data, status) {    /*console.log("preExtend update:", data); */ },
            postExtend: function (data, status) {   /*console.log("postExtend update");       */ },
            preDelete: function (data, status) { },
            postDelete: function (data, status) { },
            preResolve: function (data, status) { },
            postResolve: function (data, status) { },
            convertDates: convertDates,
            convertDatesForServer: convertDatesForServer,
            constructSendUpdate: constructSendUpdate,
            //tzoffset: tzoffset,
            //addTimestamp : addTimestamp,
        };

        o.applyUpdate = function (d, s) { applyUpdate.call(o, d, s); };
        o.clearAndApply = function (d, s) { clearAndApply.call(o, d, s); };
        o._preExtend = function (d, s) { o.preExtend && o.preExtend.call(o, d, s); };
        o._postExtend = function (d, s) { o.postExtend && o.postExtend.call(o, d, s); };
        o._preDelete = function (d, s) { o.preDelete && o.preDelete.call(o, d, s); };
        o._postDelete = function (d, s) { o.postDelete && o.postDelete.call(o, d, s); };
        o._preResolve = function (d, s) { o.preResolve && o.preResolve.call(o, d, s); };
        o._postResolve = function (m, d, s) { o.postResolve && o.postResolve.call(o, m, d, s); };
        if (typeof (hub) !== "undefined") {
            hub.on('update', o.applyUpdate);
        }
        return o;
    }

    return updaterFactory;
}]);
