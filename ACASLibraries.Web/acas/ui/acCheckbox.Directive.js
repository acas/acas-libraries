acas.module('acCheckbox', 'acas.ui.angular', function () {
	acas.ui.angular.directive('acCheckbox', function () {
		return {
			restrict: 'E',
			require: ['ngModel'],
			scope: {
				ngModel: '=',
				acReadonly: '=',
				ngDisabled: '=?'
			},
			replace: true,
			template: function () {
				return '<span><span ng-show="acReadonly" class="glyphicon" ng-class="ngModel ? \'glyphicon-ok\' : \' \'"></span><input ng-show="!acReadonly" type="checkbox" ng-model="ngModel" ng-disabled="ngDisabled"/></span>'
			}
		}
	})
})
