acas.module('acBlockUiManager', 'underscorejs', 'acas.ui.angular', function () {
	acas.ui.angular.factory('acBlockUiManager', function () {

		return new function () {
			var blocks = []

			var api = {
				start: function (url, method) {
					_.each(_.filter(blocks, function (x) {
						return url.match(new RegExp(x.urlPattern))
							&& x.excludeHttpMethods.indexOf(method) === -1
							&& (x.httpMethods.length === 0 || x.httpMethods.indexOf(method) !== -1)
					}), function (block) {
						block.blocking = true
					})
				},

				stop: function (url) {
					_.each(_.filter(blocks, function (x) { return url.match(new RegExp(x.urlPattern)) }), function (block) {
						block.blocking = false
					})
				},

				register: function (data) {
					var newBlock = {
						urlPattern: data.urlPattern,
						httpMethods: data.httpMethods,
						excludeHttpMethods : data.excludeHttpMethods,
						blocking: data.blocking
					}
					blocks.push(newBlock)
					return newBlock
				}
			}

			return api
		}
	})

	acas.ui.angular.config(['$httpProvider', '$provide', function ($httpProvider, $provide) {
		$provide.factory('acBlockUi-httpInterceptor', ['$q', 'acBlockUiManager', function ($q, acBlockUIManager) {
			return {
				'request': function (request) {					
					acBlockUIManager.start(request.url, request.method)
					return request
				},
				'response': function (response) {
					if (response) {
						var url = (response.config ? response.config.url : response.url)
						acBlockUIManager.stop(url)
					}
					return response
				},
				'responseError': function (rejection) {
					acBlockUIManager.stop(rejection.config.url)
					return $q.reject(rejection)
				},
				'requestError': function (rejection) {
					acBlockUIManager.stop(rejection.config.url)
					return $q.reject(rejection)
				}
			}
		}])
		$httpProvider.interceptors.push('acBlockUi-httpInterceptor')
	}])

})
