acas.module('acBlockUi', 'jquery', 'acas.ui.angular', 'acBlockUiManager', function () {
	acas.ui.angular.directive('acBlockUi', ['acBlockUiManager', function (acBlockUiManager) {
		/*
		This is a work in progress. The block ui style needs to be improved - it should only cover the element it's on, and it should
		not let the user scroll off of it
		*/
		return {
			restrict: 'A',
			replace: false,
			transclude: false,
			scope: {
				acBlockUi: '='
			},
			link: function (scope, element, attributes) {

				var block = acBlockUiManager.register({
					urlPattern: attributes.acUrlPattern,
					manualStartKey: attributes.acManualStartKey,
					element: element,
					blocking: false
				})

				var blockElement
				var applyBlock = function (on) {
					if (on) {
						var html = "<div class = 'ac-block-ui'><span class='ac-block-ui-message'>Please Wait...</span></div>"
						blockElement = jQuery(jQuery(element.parent()).prepend(html).children()[0])

					} else {
						if (blockElement) {
							blockElement.remove()
							blockElement = null
						}
					}

				}

				scope.$watch(
					function () { return block.blocking },
					function (newValue) {
						applyBlock(newValue)
					}
				)

				scope.$watch(scope.acBlockUi,
					function (newValue) {
						block.blocking = newValue
					}
				)
			}
		}

	}])

})

