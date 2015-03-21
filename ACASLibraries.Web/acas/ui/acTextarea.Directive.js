acas.module('acTextarea', 'acas.ui.angular', function () {
	acas.ui.angular.directive('acTextarea', [function () {
		return {
			restrict: 'E',
			replace: true,
			require: 'ngModel',
			template: '<textarea ng-model="acModel" class="{{acInputClass}}"></textarea>',
			scope: {
				acModel: '=',
				acInputClass: '@'
			},
			link: function (scope, element, attributes, model) {
				element.attr('maxlength', attributes.maxlength)				

				var originalHeight = element[0].style.height
				var resizeTextarea = function () {
					element[0].style.height = '0'; //this forces us to rely on the element's min-height
					element[0].style.height = element[0].scrollHeight + 3 + 'px'					
				}
				
				element.on('change', resizeTextarea)
				element.on('keydown', resizeTextarea)
				scope.$watch(function () { return scope.acModel }, function () {
					resizeTextarea()					
				})

				scope.$watch(function () { return element[0].scrollHeight }, function (newValue, oldValue) {					
					if (oldValue === 0) resizeTextarea()
				})				
			}
		}
	}])
})
