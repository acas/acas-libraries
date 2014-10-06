'use strict'

acas.module('acDataCache', 'acas.utility.angular', 'underscorejs', function () {
	acas.utility.angular.factory('acDataCache', ['$q', '$http', function ($q, $http) {
		return new function () {

			var dataCache = {}

			var utilities = {
				dataInCache: function (url) {
					return dataCache[url] !== undefined
				},

				storeData: function (url, data) {
					dataCache[url] = data
				},

				retrieveData: function (url) {
					return dataCache[url]
				}
			}

			var api = {
				getData: function (url) {
					var deferred = $q.defer()
					if (utilities.dataInCache(url)) {
						deferred.resolve(utilities.retrieveData(url))
					}
					else {
						$http({ method: 'GET', url: url })
							.success(function (data) {
								utilities.storeData(url, data)
								deferred.resolve(data)
							})
							.error(function (data) {
								deferred.reject(data)
							})
					}
					return deferred.promise
				}
			}

			return api

		}
	}])

})