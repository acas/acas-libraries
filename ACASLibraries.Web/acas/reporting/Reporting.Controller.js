acas.module('acas.reporting.angular.controller', 'acas.reporting.angular', 'acReporting', 'ui.bootstrap', function () {
	acas.reporting.angular.controller('acas-reporting-angular-controller', ['$scope', '$modalInstance', 'params', 'acReporting',
	function ($scope, $modalInstance, params, acReporting) {
		/**Controller for the reporting service's modal **/
		$scope.acas.reporting = new function () {			
			var queryString = params.queryString
			var reportTitle = params.reportTitle
			var utilities = {}

			var api = {
				reportTitle: reportTitle,
				downloadReport: function (format) {
					acReporting.downloadReport(queryString, format)
					$modalInstance.close()
				}
			}

			return api
		}
	}])
})