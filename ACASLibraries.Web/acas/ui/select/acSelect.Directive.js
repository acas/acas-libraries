'use strict';

acas.module('acSelect', 'acas.angular', 'select2', 'jquery', 'underscorejs', 'acValidation', function () {
	/*
	 * See below in the directive for argument usage. Wrap the item in a span or div, for some reason things go wrong when you don't.
	 * 
	 */
	acas.angular.directive('acSelect', ['$timeout', 'acValidation', function ($timeout, acValidation) {

		var defaultOptions = {
			allowSearch: true,
			allowEmpty: true,
			allowClear: false,
			bindOptionsOnce: false
		}

		return {
			restrict: 'E',
			scope: {
				acOptions: '=', //data array that contains the dropdown items
				acValue: '@', //name of property that contains the value
				acDisplay: '@', //name of property to be displayed
				acDisplayFunction: '=', //alternative to acDisplay, for the items in the dropdown - not currently supported if acBindOptionsOnce is true
				acDisplaySelected: '=', //alternative to acDisplay, for the selected item - not currently supported if acBindOptionsOnce is true
				acSelectOptions: '=', //object containing optional parameters, defaults to: { allowSearch: true, allowEmpty: true, bindOptionsOnce: false }
				acPlaceholder: '=', //deprecated: use the placeholder property inside acSelectOptions
				acChange: '=',
				ngModel: '=',
				acReadonly: '='
			},
			template: function (element, attributes) {				
				eval('var config = ' + attributes.acSelectOptions)				
				config = _.extend(_.clone(defaultOptions), config)

				var optionsHtml
				if (config.bindOptionsOnce) {					
					optionsHtml = '<option ng-repeat="element in ::acOptions" value="{{::element[acValue]}}" data-ac-display-selected="{{::acDisplaySelected(element)}}">{{::element[acDisplay]}}{{::acDisplayFunction(element)}}</option>'					
				}
				else {					
					optionsHtml = '<option ng-repeat="element in acOptions" value="{{element[acValue]}}" data-ac-display-selected="{{acDisplaySelected(element)}}">{{element[acDisplay]}}{{acDisplayFunction(element)}}</option>'
				}				
				if (config.allowEmpty || config.allowClear) {				
					optionsHtml = '<option></option>' + optionsHtml
				}

				return '<span' + (element.ngShow != null ? ' ng-show="ngShow"' : '') + '>' +
							'<select class="ac-select" style="width:100%;padding:0;"> '
									+ optionsHtml +
							' </select></span>';
			},
			link: function (scope, element, attributes) {
				var config = _.extend(_.clone(defaultOptions), scope.acSelectOptions)
				opts = {
					formatResult: function (x) {
						return x.element[0].innerHTML
					},
					formatSelection: function (x) {
						var formatted = jQuery(x.element).data('ac-display-selected')						
						if (formatted) {
							return formatted
						} else {
							return x.text
						}
					},
					minimumResultsForSearch: (!config.allowSearch ? -1 : undefined),
					allowClear: !!config.allowClear,
					placeholder: scope.acPlaceholder || config.placeholder
				}
				
				var select = element.children().children().select2(opts)

				
				scope.$watch(function () { return scope.acReadonly }, function () {
					select.select2("readonly", scope.acReadonly)
					//if it's readonly the placeholder shouldn't be there
					scope.placeholder = scope.acReadonly ? '' : scope.acPlaceholder 
					//input.select2("enable", !scope.acReadonly)
				})

				scope.$watch(function () { return scope.acOptions },
					function () {
						//wrap in timeout to let angular update the options in the select2 before attempting to reselect the correct model value
						$timeout(function () { select.select2('val', scope.ngModel) })
					}
				)

				select.on('change', function (e) {
					$timeout(function () {
						scope.$apply(function () {
							var elementFound = _.find(scope.acOptions, function (x) { return x[scope.acValue] == select.val() })
							if (elementFound !== undefined
								&& elementFound != null) {
								scope.ngModel = elementFound[scope.acValue]
							}
							else scope.ngModel = null
						})
					})
				})

				scope.$watch(function () { return scope.ngModel },
					function () {
						if (scope.ngModel) {
							//console.log('model changed')
							select.select2('val', scope.ngModel)
						}

					}
				)

				if (attributes.acValidationKey) { //validation only works if a validation key is provided
					var allowEmpty = config.allowEmpty // TODO: decouple the validation allowEmpty from the allowEmpty that controls whether a blank option appears
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

			}
		}
	}])
});

