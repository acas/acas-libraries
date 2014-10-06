acas.module('acReporting', 'acas.reporting.angular', 'jquery', 'jquery.blockUI', 'ui.bootstrap', function () {
	acas.reporting.angular.factory('acReporting', ['$modal', function ($modal) {
		/**
		Usage: you can call acReporting.downloadReport() from anywhere the service is available,
		or you can get the dialog to choose a format by calling showDownloadReportDialog()
		**/
		var utilities = {
			
		}

		var api = {

			downloadReport: function (queryString, format) {
				if (!acas.config.reporting.reportDownloadUrl) {
					throw 'Error: Cannot download report. The reporting service has not been configured. acas.config.reporting.reportDownloadUrl must point to a valid endpoint on the server.'
				}
				jQuery.growlUI("", "<div class='ac-reporting-dialog-file-format-" + format + "' " + "style='float:left;'></div>Please wait while your<br />report is generated...")
				document.location.href = acas.config.reporting.reportDownloadUrl + "?" + queryString + "&format=" + format
			},

			showDownloadReportDialog: function (reportTitle, queryString) {				
				$modal.open({
					templateUrl: acas.cdnBaseUrl + 'reporting/reporting.html',
					controller: 'acas-reporting-angular-controller',
					resolve: {
						params: function () {
							return {
								reportTitle: reportTitle,
								queryString: queryString								
							}
						}
					},
					background: 'static', // false,
					keyboard: false,
					windowClass: 'ac-reporting-dialog-window'
				})
			}
		}

		return api

	}])
})
