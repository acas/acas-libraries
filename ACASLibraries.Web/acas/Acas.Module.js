'use strict'

acas.module('acas.angular', 'angularjs', function () {
	var angularDependencies = [
		'acas.formatting.angular',
		'acas.utility.angular',
		'acas.ui.angular',
		'acas.reporting.angular'
	]

	//add acas.notifications.angular IF ui.bootstrap is loaded
	try {
		angular.module('ui.bootstrap')
		angularDependencies.push('acas.notifications.angular')
	}
	catch (ex) { }

	acas.angular = angular.module('acas.angular', angularDependencies)

	//setup the scope object
	acas.angular.run(['$rootScope', function ($rootScope) {
		$rootScope.acas = {}
	}])
	//allow CORS requests to hit the acas libraries cdn in addition to the application url
	acas.angular.config(['$sceDelegateProvider', function ($sceDelegateProvider) {
		$sceDelegateProvider.resourceUrlWhitelist([
			'self',   		
			acas.cdnBaseUrl + '**/*'
		])		
	}])
})


acas.module('acas.notifications.angular', 'acas.angular', 'ui.bootstrap', function () {
	acas.notifications.angular = angular.module('acas.notifications.angular', [
		'ui.bootstrap' //needed for notifications
	])
})
acas.module('acas.reporting.angular', 'acas.angular', function () {
	acas.reporting.angular = angular.module('acas.reporting.angular', [])
})
acas.module('acas.formatting.angular', 'acas.angular', function () {
	acas.formatting.angular = angular.module('acas.formatting.angular', [])
})
acas.module('acas.utility.angular', 'acas.angular', function () {
	acas.utility.angular = angular.module('acas.utility.angular', [])
})
acas.module('acas.ui.angular', 'acas.angular', function () {
	acas.ui.angular = angular.module('acas.ui.angular', [])
})
