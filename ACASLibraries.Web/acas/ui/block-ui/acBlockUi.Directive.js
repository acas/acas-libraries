acas.module('acBlockUi', 'jquery', 'underscorejs', 'acas.ui.angular', 'acBlockUiManager', function () {
	acas.ui.angular.directive('acBlockUi', ['acBlockUiManager', function (acBlockUiManager) {
		/*
		This is a work in progress. The block ui style needs to be improved - it should only cover the element it's on, and it should
		not let the user scroll off of it

		How to use: 
			Pass a javascript object to ac-block-ui with properties:
				* urlPattern: Regex to match a request url against - the directive will block whenever a request
				  is sent to that url
				* httpMethods: string or array, CASE SENSITIVE (use caps) - optional parameter that filters which requests trigger a block
				* excludeHttpMethods: string or array, CASE SENSITIVE (use caps) - optional parameter that specifies requests that DO NOT 
				  trigger a block. Note that excludeHttpMethods overrides httpMethods if a method is in both.

			Ex: ac-block-ui="{urlPattern: 'api/resource', excludeHttpMethods: 'GET', httpMethods: ['PUT', 'POST', 'GET']}"
				This will block the element whenever a PUT or POST request is sent to a url that matches 'api/resource', but not GET
		*/
		return {
			restrict: 'A',
			replace: false,
			transclude: false,
			scope: {
				acBlockUi: '='
			},
			link: function (scope, element, attributes) {

				var config = _.extend({
					urlPattern: null,
					manualStartKey: null,
					httpMethods: [],
					excludeHttpMethods: []
				}, scope.acBlockUi)


				var block = acBlockUiManager.register({
					urlPattern: config.urlPattern,
					manualStartKey: config.manualStartKey,
					httpMethods: typeof(config.httpMethods) === 'object' ? config.httpMethods : [config.httpMethods],
					excludeHttpMethods: typeof (config.excludeHttpMethods) === 'object' ? config.excludeHttpMethods : [config.excludeHttpMethods],
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
			}
		}

	}])

})

