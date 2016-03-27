angular.module('updaterModule', []).factory('updater', [function () {


    function removeDeleted(model) {
        for (var key in model) {
            if (model[key] == "`delete`")
                model[key] = null;
            if (typeof (model[key]) == 'object')
                removeDeleted(model[key]);
        }
    }

    function baseExtend(dst, objs, deep, after) {
        var h = dst.$$hashKey;

        //Check if the lookup has the base object
        if (typeof(after)==="undefined") {
            if ("Key" in dst) {
                //throw "Do not box update with AngularUpdate, instead pass the updated object in directly.";
                var lookthrough= objs;
                if (!angular.isArray(objs)){
                    lookthrough= [objs];
                }
                for(var k in objs){
                    var o= objs[k];
                    if ("Lookup" in o) {
                        if (dst["Key"] in o["Lookup"]) {
                            var baseObj = o["Lookup"][dst["Key"]];
                            baseExtend(objs[k], [baseObj], deep, true);
                            o["Lookup"][dst["Key"]] = null;
                        }
                    }
                }
            }
        }


        for (var i = 0, ii = objs.length; i < ii; ++i) {
            var obj = objs[i];
            if (!angular.isObject(obj) && !angular.isFunction(obj)) continue;
            var keys = Object.keys(obj);
            for (var j = 0, jj = keys.length; j < jj; j++) {
                var key = keys[j];
                var src = obj[key];
                if (deep && angular.isObject(src)) {
                    if (src.AngularList) {
                        //Special AngularList Object
                        if (src.UpdateMethod == "Add") {
                            dst[key] = dst[key].concat(src.AngularList);
                        } else if (src.UpdateMethod == "ReplaceAll") {
                            dst[key] = src.AngularList;
                        } else if (src.UpdateMethod == "ReplaceIfNewer") {
                            debugger;


                            var keysList = [];
                            for (var e in dst[key]) {
                                keysList.push(dst[key][e]["Key"]);
                            }
                            for (var e in src.AngularList) { //Foreach element in src
                                var loc = keysList.indexOf(src.AngularList[e]["Key"]);
                                if (loc != -1) {
                                    dst[key][loc] = src.AngularList[e];
                                } else {
                                    dst[key].push(src.AngularList[e]);
                                    keysList.push(src.AngularList[e]["Key"]);
                                }
                            }


                        } else {
                            console.error("UpdateMethod unknown:"+src.UpdateMethod);
                        }
                    } else {
                        if (!angular.isObject(dst[key]))
                            dst[key] = angular.isArray(src) ? [] : {};

                        if (angular.isArray(dst[key])) {
                            if (src.length > 0 && "Key" in src[0]) {
                                var keysList = [];
                                for(var e in dst[key]){
                                    keysList.push(dst[key][e]["Key"]);
                                }
                                for (var e in src) { //Foreach element in src
                                    var loc = keysList.indexOf(src[e]["Key"]);
                                    if (loc!=-1) {
                                        dst[key][loc] = src[e];
                                    } else {
                                        dst[key].push(src[e]);
                                        keysList.push(src[e]["Key"]);
                                    }
                                }
                            }else{
                                dst[key] = dst[key].concat(src);
                            }
                        } else {
                            if (dst[key].Key == src.Key)
                                baseExtend(dst[key], [src], true,true);
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

    function convertDates(obj) {
        var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
        //var dateRegex2 = /^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$/;
        var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
        for (var key in obj) {
            var value = obj[key];
            var type = typeof (value);
            if (obj[key] == null) {
                //Do nothing
            } else if (type == 'string' && dateRegex1.test(value)) {
                obj[key] = new Date(parseInt(value.substr(6)));
            } else if (type == 'string' && dateRegex2.test(value)) {
                obj[key] = new Date(obj[key]);
            } else if (obj[key].getDate !== undefined) {
                obj[key] = new Date(obj[key].getTime() /*- obj[key].getTimezoneOffset() * 60000*/);
            } else if (type == 'object') {
                convertDates(value);
            }
        }
    }

    function clearAndApply(data, status) {
        this.scope.model = {};
        this.applyUpdate(data, status);
    }

    function applyUpdate(data, status) {
        this._preExtend(data, status);
        baseExtend(this.scope.model, [data], true);
        this._postExtend(data, status);
        convertDates(this.scope.model);
        this._preDelete(data, status);
        removeDeleted(this.scope.model);
        this._postDelete(data, status);
    }

    function updaterFactory($scope,hub) {
        var o = {
            scope:$scope,
            preExtend: function (data, status) {console.log("preExtend update:", data);},
            postExtend:function (data, status) {console.log("postExtend update");},
            preDelete: function (data, status) { },
            postDelete: function (data, status) { },
            convertDates: convertDates,
        };

        o.applyUpdate   = function (d, s) { applyUpdate.call(o, d, s); };
        o.clearAndApply = function (d, s) { clearAndApply.call(o, d, s); };
        o._preExtend    = function (d, s) { o.preExtend && o.preExtend.call(o, d, s); };
        o._postExtend   = function (d, s) { o.postExtend && o.postExtend.call(o, d, s); };
        o._preDelete    = function (d, s) { o.preDelete && o.preDelete.call(o, d, s); };
        o._postDelete   = function (d, s) { o.postDelete && o.postDelete.call(o, d, s); };

        hub.on('update', o.applyUpdate);
        return o;
    }

    return updaterFactory;
}]);
