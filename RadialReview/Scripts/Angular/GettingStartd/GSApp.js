var l10App = angular.module('GSApp', ['helpers', 'rockstate', 'buttonbar', 'xeditable',"puElasticInput"]);


//l10App.config(['$locationProvider', function ($locationProvider) {
//    $locationProvider.html5Mode({
//        enabled: true,
//        requireBase: false
//    });
//}]);

l10App.config(['fcsaNumberConfigProvider', function(fcsaNumberConfigProvider) {
    fcsaNumberConfigProvider.setDefaultOptions({
        "preventInvalidInput": true
    });
}]);

l10App.config(['$mdThemingProvider', function ($mdThemingProvider) {
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