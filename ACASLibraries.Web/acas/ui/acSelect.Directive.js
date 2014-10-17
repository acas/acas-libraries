'use strict';

acas.module('acSelect', 'select2', 'jquery', 'acValidation', function () {
	acas.directive('acSelect', ['$timeout', 'acValidation', function ($timeout, acValidation) {
		//init global settings if not aleady defined
		if (!acas.internal) {
			acas.internal = {}
		}
		if (!acas.internal.acSelect) {
			acas.internal.acSelect = null;
		}

		return {
			restrict: 'E',
			scope: {
				ngModel: '=', //does this really work?
				ngChange: '=',
				ngShow: '=', //does this work?

				acOptions: '=',
				acValue: '@',
				acDisplayFunction: '=', //not currently supported if acBindOptionsOnce is true
				acDisplay: '@',
				acChange: '=',

				acBindOptionsOnce: '@',
				acAllowEmpty: '@' //default will be false
			},
			replace: true,
			transclude: false,
			template: function (element, attributes) {
				var optionsHtml
				if (attributes.acBindOptionsOnce) {
					if (attributes.acDisplayFunction) {
						console.warn('acSelect does not support acDisplayFunction when acBindOptionsOnce is true. acDisplayFunction will be ignored.')
					}
					optionsHtml = '<option bindonce ng-repeat="element in acOptions" bo-value="element[acValue]" bo-text="element[acDisplay]"></option>'
				}
				else {
					optionsHtml = '<option ng-repeat="element in acOptions" value="{{element[acValue]}}">{{element[acDisplay]}}{{acDisplayFunction(element)}}</option>'
				}

				var allowEmpty = attributes.acAllowEmpty !== "false" //default to true
				if (allowEmpty) {
					optionsHtml = '<option value=""> </option>' + optionsHtml
				}

				return '<span' + (element.ngShow != null ? ' ng-show="ngShow"' : '') + '><select ui-select2 class="form-control" style="width:100%;padding:0;" ng-model="ngModel"  ng-change="acSelectChange()"> ' + optionsHtml + ' </select></span>';
			},
			link: function (scope, element, attributes, ngModelController) {
				var $select = jQuery(element.find("select2-chosen")[0]);

				if (attributes.acValidationKey) { //validation only works if a validation key is provided
					var allowEmpty = attributes.acAllowEmpty === "true" //default to false. TODO: decouple the validation allowEmpty from the allowEmpty that controls whether a blank option appears
					var warningBox


					var validate = function () {
						var val = jQuery(jQuery(element).children()[1]).val()
						var html = validationService.validationMessageHtml('Please enter a value')
						if (!allowEmpty && (val === null || val.trim() === "")) {
							element.children(":first")[0].classList.add('ac-warning-border')
							if (!warningBox) {
								warningBox = jQuery(jQuery(element.parents()[0]).prepend(html).children()[0]);
							}

							return false
						} else {
							element.children(":first")[0].classList.remove('ac-warning-border')
							if (warningBox) {
								warningBox.remove()
								warningBox = null
							}
							return true
						}
					}

					var validator = validationService.register(attributes.acValidationKey, validate)

					scope.$watch(
						function () { return jQuery(jQuery(element).children()[1]).val() },
						function () {
							if (validator.active) {
								validator.validate()
							}
						}
					)

					scope.$on('$destroy', function () {
						validationService.remove(validator)
					})

				}

				$select.on('show', function (ev) {
					if (acas.internal.acSelect != null && acas.internal.acSelect != $select) {
						ev.preventDefault();
						ev.stopImmediatePropagation();
						return false;
					}
					acas.internal.acSelect = $select;
				})

				if (attributes.acChange) {
					scope.$watch(
						function () { return jQuery(jQuery(element).children()[1]).val() },
						function (newValue, oldValue, scope) {
							if (newValue !== oldValue) {
								scope.acChange(newValue, oldValue, scope)
							}
						})
				}

				scope.acSelectChange = function () {
					//I don't think this block does anything. Verify before removing.
					if (scope.ngModel && ngModelController) {
						$timeout(function () {
							ngModelController.setViewValue(element.val());
						});
					}

					if (scope.ngChange) {
						scope.ngChange();
					}

				}
			}
		}
	}])
});

