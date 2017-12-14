
//window.Ellapsed = 0;

//angular.module('transformModule', [])
//	.filter('transform', function () {
//	    function populate(item, scope, topLevel) {
//	        if (typeof (topLevel) === "undefined")
//	            topLevel = true;


//	        //if (scope.$$phase == "$digest")
//	        //    scope.$$phase = { transform: {} };

//	        if (Array.isArray(item)) {
//	            var out = [];
//	            for (var k in item) {
//	                out.push(populate(item[k], scope, false));
//	            }
//	            return out;
//	        }

//	        //if (item && item.Key) {
//	        //    if (scope.$$phase.transform[item.Key]) {	               
//	        //        return scope.$$phase.transform[item.Key];
//	        //    }
//	        //}
//	        //var o;
//	        else if (item && item._P) {
//	            var out = scope.model.Lookup[item.Key];

//	            for (var k in out) {
//	                out[k] = populate(out[k], scope, false);
//	            }
//                return out;
//	        } else if (item && topLevel) {
//	            for (var k in item) {
//	                item[k] = populate(item[k], scope, false);
//	            }
//	            return item;
//	        } else {
//	            return item;
//	        }
//	        //if (item && item.Key) {
//	        //    scope.$$phase.transform[item.Key] = o;
//	        //}
//	       // return o;
//	    }
//	    var func = function (items, scope) {
//            //console.log("transform")
//	        //var t = +new Date();
//	        return items;
//	        var p = populate(items, scope);

//	        //window.Ellapsed += (+new Date()) - t;
//	        return p;
//	    };

//	    return func;
//	})
//	.config(['$httpProvider', function ($httpProvider) {
//	    var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
//	    var dateRegex2 = /^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$/;

//	    var convertDates = function (obj) {
//	        for (var key in obj) {
//	            var value = obj[key];
//	            var type = typeof (value);
//	            if (type == 'string' && dateRegex1.test(value)) {
//	                obj[key] = new Date(parseInt(value.substr(6)));
//	            } else if (type == 'string' && dateRegex2.test(value)) {
//	                obj[key] = new Date(obj[key]);
//	            } else if (obj === "`delete`") {
//	                obj[key] = null;
//	            } else if (type == 'object') {
//	                convertDates(value);
//	            }
//	        }
//	    };

//	    /*function populate(item, lookup) {
//			if (Array.isArray(item)) {
//				var out = [];
//				for (var k in item) {
//					out.push(populate(item[k], lookup));
//				}
//				return out;
//			} else if (item && item._P) {
//				var out = lookup[item.Key];
//				for (var k in out) {
//					out[k] = populate(out[k], lookup);
//				}
//				return out;
//			} else {
//				return item;
//			}
//		}*/

//	    $httpProvider.defaults.transformResponse.push(function (responseData) {
//	        convertDates(responseData);
//	        /*if (responseData.Lookup) {
//				populate(responseData, responseData.Lookup);
//			}*/
//	        return responseData;
//	    });

//	}]);
