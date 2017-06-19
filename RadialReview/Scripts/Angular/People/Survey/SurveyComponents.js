//SurveyContainer Controller
angular.module('people', ['helpers']).component('surveyContainer', {
    templateUrl: function () { return '/Content/AngularTemplates/People/surveyContainer.html'; },
    bindings: {
        "surveyContainerId": "<?",
        surveyId: "<?",
    },
    controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
        var ctrl = this;
        $scope.functions = {};
        $log.log("survComp:", $scope);
        var r = radial($scope, {
            hubName: "PeopleHub",
            hubJoinMethod: "Join",
            hubJoinArgs: [ctrl.surveyContainerId, ctrl.surveyId],
            sendUpdateUrl: function (self) { return "/People/Survey/Update?type=" + self.Type; },
            loadDataUrl: "/People/Survey/Data?surveyId=" + ctrl.surveyId + "&surveyContainerId=" + ctrl.surveyContainerId,
            loadDataOptions: {
                success: function (data) {
                    $log.log("survComp2:", $scope);
                    r.updater.clearAndApply(data);
                }
            }
        });


    }]
}).component('survey', {
    templateUrl: function () { return '/Content/AngularTemplates/People/survey.html'; },
    bindings: {
        survey: "<"
    },
    controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
        $scope.model = this.survey;
    }]
}).component('surveySection', {
    templateUrl: function () { return '/Content/AngularTemplates/People/surveySection.html'; },
    bindings: {
        section: "<"
    },
    controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
        $scope.model = this.section;
    }]
}).component('surveyItemContainer', {
    templateUrl: function () { return '/Content/AngularTemplates/People/surveyItemContainer.html'; },
    bindings: {
        itemContainer: "<"
    },
    controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
        $scope.model = this.itemContainer;
    }]
}).component('surveyItem', {
    template : '<div ng-include="getActualTemplateContent()"></div>',

    //templateUrl:["$element", "$attrs",  function ($element,$attrs) {
    //    debugger;
    //    return '/Content/AngularTemplates/People/Items/' + $attrs.type + '-item.html';
    //}],
    bindings: {
        item: "<",
        format: "<",
        type: "<",
        response: "<?"
    },
    controller: ["$scope", "$element", "$attrs", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
        $scope.item = this.item;
        $scope.type = this.type;
        $scope.format = this.format;
        $scope.response = this.response;

        $scope.classes = this.format.classes || {
            responses: "btn-group",
            response: "",
            responselabel:"btn btn-primary"

        };

        //$scope.type = thj
        //var type = this.type;
        $scope.getActualTemplateContent = function () {
            debugger;
            return '/Content/AngularTemplates/People/Items/' +$scope.format.TemplateModifier+"/"+ $scope.format.ItemType + '-item.html';
        };
    }]
});
//Section Controller

//ItemContainer Controller