acas.module('acDatepicker', 'acas.utility', 'acas.ui.angular', 'jquery.ui', function () {
	acas.ui.angular.directive('acDatepicker', function () {
		return {
			restrict: 'E',
			scope: {
				acValue: '=',
				acInputClass: '@',
				acHideClearButton: '@'
			},
			replace: true,
			template: '<span><input ng-readonly="true" style="cursor: pointer; background-color: white; display: inline;" class="form-control {{acInputClass}}" type="text" />' +
					   '<span ng-hide="{{acHideClearButton}}" style="cursor: pointer; display: inline-block; vertical-align: middle;" class="ui-icon ui-icon-closethick" ng-click="clearValue()"></span>' +
					   '</span>',
			link: function (scope, element) {
				var input = element.children().first()
				input.datepicker({
					dateFormat: 'mm/dd/yy',
					onSelect: function (selection) {
						scope.$apply(function () {
							scope.acValue = acas.utility.parser.toDate(selection)
						});
					}
				});

				scope.$watch(function () { return scope.acValue }, function () {
					input.val(acas.utility.formatting.formatDate(scope.acValue))
				})

				scope.clearValue = function () {
					scope.acValue = null
				}
			}
		}
	})
})
