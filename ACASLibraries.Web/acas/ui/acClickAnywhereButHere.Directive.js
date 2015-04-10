acas.module('acDatepicker', 'acas.utility', 'acas.ui.angular', function () {
	acas.ui.angular.directive('acClickAnywhereButHere', ['$document', function ($document) {
		//put on an element to evaluate an expression when anywhere else in the document is clicked
		//Usage: 
		//-----ds-click-anywhere-but-here="someScopeVariable = true"
		//-----ds-click-anywhere-but-here="callThisFunction(arg1, arg2)"
		//concept from: http://stackoverflow.com/questions/12931369/click-everywhere-but-here-event
		return {
			restrict: 'A',
			scope: {
				dsClickAnywhereButHere: '&'
			},
			link: function (scope, element, attr, ctrl) {
				var handler = function (event) {
					if (!element[0].contains(event.target)) {
						scope.$apply(function () {
							scope.dsClickAnywhereButHere()
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