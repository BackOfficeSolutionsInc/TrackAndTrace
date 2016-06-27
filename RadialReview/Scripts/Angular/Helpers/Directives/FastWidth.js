angular.module('fastWidth', []);//.directive("gridTile", ["ngIfDirective", function (ngIfDirective) {
//    var ngIf = ngIfDirective[0];
//    return {
//        //multiElement: true,
//        transclude: true,
//        priority: ngIf.priority,
//        terminal: ngIf.terminal,
//        restrict: "C",
//        scope: {
//        },
//        template: "<ng-transclude></ng-transclude>",
//        link: function (scope, element, attr, ctrl, transclude) {
//            //scope.width1 = element.hasClass("width_1");
//            var tsclude = transclude;
//            scope.transclude=transclude;
//            scope.exe = false;
//            scope.$watch(function () { return element.attr('class'); },
//                function (newValue, oldValue) {
//                    var newWidth = element.hasClass("width_1");
//                    if (newWidth != scope.width1)
//                        console.log("width1 " + newWidth);
//                    scope.width1 = element.hasClass("width_1");
//                   // if (!scope.exe && newValue && oldValue && newValue.indexOf("loaded") != -1 && oldValue.indexOf("loaded") == -1) {
//                        element.find(".visible-width-1").each(function () {
//                            var e = angular.element(this);
//                            var s = e.scope();
//                            //e.attr.ngIf = function () {
//                            //    return false;//scope.width1;
//                            //};
//                            //ngIf.link.apply(ngIf, [e.scope(), e, e.attr, ctrl, function () {
//                            //    return e.html();
//                            //}]);
                          
//                            //childScope, previousElements;

//                            if (scope.width1) {
//                                if (!s.childScope) {
//                                    transclude(function (clone, newScope) {
//                                        s.childScope = newScope;
//                                        clone[clone.length++] = $compile.$$createComment('end ngIf', $attr.ngIf);
//                                        // Note: We only need the first/last node of the cloned nodes.
//                                        // However, we need to keep the reference to the jqlite wrapper as it might be changed later
//                                        // by a directive with templateUrl when its template arrives.
//                                        block = {
//                                            clone: clone
//                                        };
//                                        $animate.enter(clone, e.parent(), e);
//                                    });
//                                }
//                            } else {
//                                if (s.previousElements) {
//                                    s.previousElements.remove();
//                                    s.previousElements = null;
//                                }
//                                if (s.childScope) {
//                                    s.childScope.$destroy();
//                                    s.childScope = null;
//                                }
//                                if (s.block) {
//                                    s.previousElements = getBlockNodes(s.block.clone);
//                                    $animate.leave(s.previousElements).then(function () {
//                                        s.previousElements = null;
//                                    });
//                                    s.block = null;
//                                }
//                            }

//                            //$(this).show();
//                            //resumeWatchersScope(angular.element(this).scope());
//                        });
//                        element.find(".hidden-width-1").each(function () {
//                            var e = angular.element(this);
//                            var s = e.scope();
//                            if (!scope.width1) {
//                                if (!s.childScope) {
//                                    transclude(function (clone, newScope) {
//                                        s.childScope = newScope;
//                                        clone[clone.length++] = $compile.$$createComment('end ngIf', $attr.ngIf);
//                                        // Note: We only need the first/last node of the cloned nodes.
//                                        // However, we need to keep the reference to the jqlite wrapper as it might be changed later
//                                        // by a directive with templateUrl when its template arrives.
//                                        block = {
//                                            clone: clone
//                                        };
//                                        $animate.enter(clone, e.parent(), e);
//                                    });
//                                }
//                            } else {
//                                if (s.previousElements) {
//                                    s.previousElements.remove();
//                                    s.previousElements = null;
//                                }
//                                if (s.childScope) {
//                                    s.childScope.$destroy();
//                                    s.childScope = null;
//                                }
//                                if (s.block) {
//                                    s.previousElements = getBlockNodes(s.block.clone);
//                                    $animate.leave(s.previousElements).then(function () {
//                                        s.previousElements = null;
//                                    });
//                                    s.block = null;
//                                }
//                            }
//                            //e.attr.ngIf = function () {
//                            //    return true;//!scope.width1;
//                            //};
//                            //ngIf.link.apply(ngIf, [e.scope(), e, e.attr, ctrl, function () {
//                            //    return e.html();
//                            //}]);

//                            //$(this).hide();
//                            //suspendWatchersScope(angular.element(this).scope());
//                        });
//                    //    scope.exe = true;
//                    //}
//                });


//            //scope.$watch(function () { return element.attr('class'); }, function (newValue, oldValue) {
//            //    var newWidth = element.hasClass("width_1");



//            //    if (newWidth != scope.width1) {
//            //       // debugger;
//            //        scope.width1 = newWidth;
//            //        if (newWidth) {


//            //        } else {
//            //            element.find(".visible-width-1").each(function () {
//            //                var e = angular.element(this);
//            //                e.attr.ngIf = function () {
//            //                    return false;
//            //                };
//            //                ngIf.link.apply(ngIf, [e.scope(), e, e.attr, ctrl, tsclude]);
//            //            });
//            //            element.find(".hidden-width-1").each(function () {
//            //                var e = angular.element(this);
//            //                e.attr.ngIf = function () {
//            //                    return true;
//            //                };
//            //                ngIf.link.apply(ngIf, [e.scope(), e, e.attr, ctrl, tsclude]);
//            //            });

//            //        }
//            //    }
//            //});
//            //var watchers = {
//            //    suspended: false
//            //};

//            //function suspendFromRoot() {
//            //    if (!watchers.suspended) {
//            //        $timeout(function () {
//            //            suspendWatchers();
//            //            watchers.suspended = true;
//            //        })
//            //    }
//            //}

//            //function refreshSuspensionFromRoot() {
//            //    if (watchers.suspended) {
//            //        $timeout(function () {
//            //            suspendWatchers();
//            //        })
//            //    }
//            //}

//            //function resumeFromRoot() {
//            //    if (watchers.suspended) {
//            //        $timeout(function () {
//            //            resumeWatchers();
//            //            watchers.suspended = false;
//            //        })
//            //    }
//            //}
//            //function suspendWatchersScope(scp) {
//            //    iterateSiblings(scp, suspendScopeWatchers);
//            //    iterateChildren(scp, suspendScopeWatchers);
//            //};

//            //function resumeWatchersScope(scp) {
//            //    iterateSiblings(scp, resumeScopeWatchers);
//            //    iterateChildren(scp, resumeScopeWatchers);
//            //};

//            //function suspendWatchers() {
//            //    suspendWatchersScope(scope);
//            //};

//            //function resumeWatchers() {
//            //    resumeWatchersScope(scope)
//            //};

//            //var mockScopeWatch = function (scopeId) {
//            //    return function (watchExp, listener, objectEquality, prettyPrintExpression) {
//            //        watchers[scopeId].unshift({
//            //            fn: angular.isFunction(listener) ? listener : angular.noop,
//            //            last: void 0,
//            //            get: $parse(watchExp),
//            //            exp: prettyPrintExpression || watchExp,
//            //            eq: !!objectEquality
//            //        })
//            //    }
//            //}

//            //function suspendScopeWatchers(scope) {
//            //    if (!watchers[scope.$id]) {
//            //        watchers[scope.$id] = scope.$$watchers || [];
//            //        scope.$$watchers = [];
//            //        scope.$watch = mockScopeWatch(scope.$id)
//            //    }
//            //}

//            //function resumeScopeWatchers(scope) {
//            //    if (watchers[scope.$id]) {
//            //        scope.$$watchers = watchers[scope.$id];
//            //        if (scope.hasOwnProperty('$watch')) delete scope.$watch;
//            //        watchers[scope.$id] = false
//            //    }
//            //}

//            //function iterateSiblings(scope, operationOnScope) {
//            //    while (!!(scope = scope.$$nextSibling)) {
//            //        if ((scope.width1))
//            //            break;

//            //        operationOnScope(scope);
//            //        iterateChildren(scope, operationOnScope);
//            //    }
//            //}

//            //function iterateChildren(scope, operationOnScope) {
//            //    while (!!(scope = scope.$$childHead)) {
//            //        if ((scope.width1))
//            //            break;

//            //        operationOnScope(scope);
//            //        iterateSiblings(scope, operationOnScope);
//            //    }
//            //}


//        }
//        //controller: ["$scope", "$element", "$attrs", function (scope, element, attrs) {
//        //    scope.iterateSiblings = function (scope, operationOnScope) {
//        //        while (!!(scope = scope.$$nextSibling)) {
//        //            operationOnScope(scope);
//        //            iterateChildren(scope, operationOnScope);
//        //        }
//        //    }

//        //    scope.iterateChildren = function (scope, operationOnScope) {
//        //        while (!!(scope = scope.$$childHead)) {
//        //            operationOnScope(scope);
//        //            iterateSiblings(scope, operationOnScope);
//        //        }
//        //    }
//        //}]
//    }
//}]);