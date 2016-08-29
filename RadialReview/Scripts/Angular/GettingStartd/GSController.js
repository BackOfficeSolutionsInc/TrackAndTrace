
var app = angular.module('GSApp', ['ngMessages', 'ngMaterial', 'md-steppers', 'lfNgMdFileInput']);
app.config(['$mdThemingProvider', function ($mdThemingProvider) {
    var customPrimary = {
        '50': '#837870',
        '100': '#756c64',
        '200': '#675f58',
        '300': '#59524c',
        '400': '#4c4641',
        '500': '#3E3935',
        '600': '#302c29',
        '700': '#22201d',
        '800': '#151312',
        '900': '#070606',
        'A100': '#8f857d',
        'A200': '#9b928a',
        'A400': '#a79f98',
        'A700': '#000000', 'contrastDefaultColor': 'light',
    };
    $mdThemingProvider.definePalette('customPrimary', customPrimary);

    var customAccent = {
        '50': '#703308',
        '100': '#883d0a',
        '200': '#9f480c',
        '300': '#b7530d',
        '400': '#cf5e0f',
        '500': '#e76811',
        '600': '#f1853a',
        '700': '#f29352',
        '800': '#f4a269',
        '900': '#f6b181',
        'A100': '#f1853a',
        'A200': '#EF7622',
        'A400': '#e76811',
        'A700': '#f8c099', 'contrastDefaultColor': 'light',
    };
    $mdThemingProvider.definePalette('customAccent', customAccent);

    var customWarn = {
        '50': '#f0b9b8',
        '100': '#eba5a3',
        '200': '#e7908e',
        '300': '#e27c79',
        '400': '#de6764',
        '500': '#d9534f',
        '600': '#d43e3a',
        '700': '#c9302c',
        '800': '#b42b27',
        '900': '#a02622',
        'A100': '#f4cecd',
        'A200': '#f9e2e2',
        'A400': '#fdf7f7',
        'A700': '#8b211e',
        'contrastDefaultColor': 'light',
    };
    $mdThemingProvider.definePalette('customWarn', customWarn);

    var customBackground = {
        '50': '#ffffff',
        '100': '#fcfcfc',
        '200': '#f0efef',
        '300': '#e3e2e2',
        '400': '#d7d5d5',
        '500': '#cac8c8',
        '600': '#bdbbbb',
        '700': '#b1aeae',
        '800': '#a4a1a1',
        '900': '#989494',
        'A100': '#ffffff',
        'A200': '#ffffff',
        'A400': '#ffffff',
        'A700': '#8b8787'
    };
    $mdThemingProvider.definePalette('customBackground', customBackground);

    $mdThemingProvider.theme('default')
        .primaryPalette('customPrimary')
        .accentPalette('customAccent')
        .warnPalette('customWarn')
        .backgroundPalette('customBackground');
    //.light();
}]);

app.controller('GSCtrl', ["$scope", "$q", "$timeout", "$http", function ($scope, $q, $timeout, $http) {

	setInterval(function () {
		$(".lf-ng-md-file-input-frame img").addClass("fixBug");
	}, 500);

    var vm = this;

   // $scope.profilePicture = {};

    vm.selectedStep = 0;
    vm.stepProgress = 1;
    vm.maxStep = 4;
    vm.showBusyText = false;
    var defaultStepData = [
            { step: 1, disable: false, completed: false, optional: false, data: { page: "Personal" } },
            { step: 2, disable: false, completed: false, optional: false, data: { page: "Organization" } },
            { step: 3, disable: false, completed: false, optional: false, data: { page: "Login" } },
            { step: 4, disable: false, completed: false, optional: false, data: { page: "Payment" } },
    ];
    $http.get('/getstarted/data').then(function (res) {
        if (typeof(res) !== "undefined") {
            vm.stepData = res.data;
            if (typeof (res.data[2]) !== "undefined" && typeof (res.data[2].data) !== "undefined" && typeof (res.data[2].data.profileUrl) !== "undefined" && res.data[2].data.profileUrl!=null) {
                $scope.profilePicture = [{
                    lfDataUrl: res.data[2].data.profileUrl,
                    lfFileName: "Profile Picture",
                    lfFile: null
                }];
            } 
        } else {
            vm.stepData = defaultStepData;
        }
    }, function () {
        vm.stepData = defaultStepData;
    });



    vm.eosdurations = [{ value: 1, text: "12+ months" }, { value: .5, text: "4 to 12 months" }, { value: .333, text: "4 months or less" }, { value: -1, text: "Just getting started" }, { value: -2, text: "What's EOS?" }]

    vm.enableNextStep = function nextStep() {
        //do not exceed into max step
        if (vm.selectedStep >= vm.maxStep) {
            return;
        }
        //do not increment vm.stepProgress when submitting from previously completed step
        if (vm.selectedStep === vm.stepProgress - 1) {
            vm.stepProgress = vm.stepProgress + 1;
        }
        vm.selectedStep = vm.selectedStep + 1;
    }
    vm.months = [];
    for (var i = 1; i <= 12; i++)
        vm.months.push({ text: "" + i, value: i });
    vm.years = [];
    var start = new Date().getFullYear();
    for (var i = 0; i <= 22; i++)
        vm.years.push({ text: "" + (start + i), value: (start + i) });

    vm.moveToPreviousStep = function moveToPreviousStep() {
        if (vm.selectedStep > 0) {
            vm.selectedStep = vm.selectedStep - 1;
        }
    }

    vm.cmaxStep = vm.selectedStep;

    vm.submitCurrentStep = function submitCurrentStep(stepData, isSkip) {
        //var deferred = $q.defer();

       // vm.cmaxStep = Math.max(vm.cmaxStep, vm.selectedStep);

        //$("video").addClass("semitrans");

        clearAlerts();
        vm.showBusyText = true;
        console.log('On before submit');

        var toSend = stepData;
      //  if (!stepData.completed && !isSkip) {
            var page = stepData.page;
            var config = {};
            //Special case to upload picture
            if (typeof (page) === "string" && page.toLowerCase() == "login") {
                if ($scope.profilePicture && Array === $scope.profilePicture.constructor && $scope.profilePicture.length > 0 && $scope.profilePicture[0].lfFile!=null) {
                    stepData.file = $scope.profilePicture[0].lfFile;
                    var formData = new FormData();
                    angular.forEach(stepData, function (v, k) {
                        formData.append(k, v);
                    });
                    config = {
                        transformRequest: angular.identity,
                        headers: { 'Content-Type': undefined }
                    }
                    toSend = formData;
                }
            }

            $http.post('/GetStarted/' + page, toSend, config).then(function (response) {
                vm.showBusyText = false;
                console.log('On submit success');
                //deferred.resolve({ status: 200, statusText: 'success', data: {} });
                //move to next step when success
                stepData.completed = true;
                vm.enableNextStep();
                if (typeof (page) === "string" && page.toLowerCase() == "login") {

                }

            }, function (response) {
                clearAlerts();
                if (typeof (response.data) != "undefined")
                    showJsonAlert(response.data);
                else
                    showAlert("An error occurred. " + response.statusText + " (" + response.status + ")");
                vm.showBusyText = false;
            });

        //} else {
        //    vm.showBusyText = false;
        //    vm.enableNextStep();
        //}
    }

}]);
