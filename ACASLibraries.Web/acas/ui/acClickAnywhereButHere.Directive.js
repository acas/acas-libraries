acas.module('acClickAnywhereButHere', 'acas.ui.angular', function () {
	acas.ui.angular.directive('acClickAnywhereButHere', ['$document', function ($document) {
		//put on an element to evaluate an expression when anywhere else in the document is clicked
		//Usage: 
		//-----ac-click-anywhere-but-here="someScopeVariable = true"
		//-----ac-click-anywhere-but-here="callThisFunction(arg1, arg2)"
		//concept from: http://stackoverflow.com/questions/12931369/click-everywhere-but-here-event
		return {
			restrict: 'A',
			scope: {
				acClickAnywhereButHere: '&'
			},
			link: function (scope, element, attr, ctrl) {
				var handler = function (event) {
					if (!element[0].contains(event.target)) {
						scope.$apply(function () {
							scope.acClickAnywhereButHere()
						})
					}
				};

				$document.on('click', handler)
				scope.$on('$destroy', function () {
					$document.off('click', handler)
				})
			}
		}
	}])
})