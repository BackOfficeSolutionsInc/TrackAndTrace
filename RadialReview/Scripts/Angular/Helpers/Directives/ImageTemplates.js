angular.module('imageTemplates', []).directive("profileImage", function() {
  return {
    restrict: "E",
	scope: {
		user:"="
	},
    template: "<span class='picture-container' title='{{user.Name}}'>"+
		"<span ng-if='user.ImageUrl!=\"/i/userplaceholder\"' class='picture' style='background: url({{user.ImageUrl}}) no-repeat center center;'></span>"+
		"<span ng-if='user.ImageUrl==\"/i/userplaceholder\"' class='picture' style=''>{{user.Initials}}</span>"+
		"</span>"
  };
});