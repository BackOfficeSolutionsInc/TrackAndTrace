angular.module("modalTemplates", []).directive("showModal", function() {
	return{
		restrict: "A",
		scope: {
			showModal: "@showModal",
			pull:"@pull",
			push:"@push"
		},
		link: function(scope, element, attrs) {
			function openModal() {
				alert("modal: "+scope.showModal);
			}
			element.on('click', openModal);
		}
	};
});