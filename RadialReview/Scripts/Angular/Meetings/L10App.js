var l10App = angular.module('L10App', ['helpers', 'rockstate', 'buttonbar', 'xeditable',"puElasticInput"]);

l10App.run(['editableOptions', function (editableOptions) {
    editableOptions.theme = 'bs3';

}]);