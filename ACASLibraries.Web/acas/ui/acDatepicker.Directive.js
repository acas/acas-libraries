acas.module('acDatepicker', 'acas.utility', 'acas.ui.angular', 'jquery.ui', function () {
	acas.ui.angular.directive('acDatepicker', [ function () {
		// date format is fixed for now
		var dateFormat = 'mm/dd/yy'

		// editor scaffolding
		var editor = jQuery('<div/>')
				.append(jQuery('<input/>')
					.css('border', '0')
					.css('background', 'transparent')
					.css('outline', 'none')
					.css('width', '100%')
					.css('height', '100%')
					.css('line-height', '100%')
					.css('padding-left', '5px')
					.css('padding-right', '5px')
					.attr('ng-model', 'acValue')
					// important!
					.attr('ng-model-options', '{ updateOn: \'blur\'}')
					.attr('ng-disabled', 'acDisabled'))
					.css('display', 'inline-block')
					.css('height', '100%')
					.css('vertical-align', 'top')
					.css('width', '84.95%')

		// remove button scaffolding
		var removeButton = jQuery('<div/>')
					.css('width', '15%')
					.css('height', '100%')
					.css('display', 'inline-block')
					.css('height', '100%')
					.css('text-align', 'center')
					.css('border-left', '1px solid lightgrey')
					.attr('ng-click', 'clearValue()')
					.attr('onmouseover', 'this.style.backgroundColor = "lightgrey";')
					.attr('onmouseout', 'this.style.backgroundColor = "white";')
					.append(jQuery('<div/>')
						.addClass('glyphicon')
						.addClass('glyphicon-remove')
						.css('vertical-align', 'middle')
						.css('top', '0'))

		// directive template
		var template = jQuery('<div/>')
			.append(jQuery('<div/>')
				.css('float', 'clear')
				.append(editor)
				.append(removeButton)).html()
		jQuery(template).attr('tabindex', '1')

		// SQL Server date ranges
		var minDate = new Date(1753, 1, 1)
		var maxDate = new Date(9999, 12, 31)

		return {
			restrict: 'E',
			scope: {
				acValue: '=',
				acInputClass: '@',
				acHideClearButton: '=?',
				acDisabled: '=?',
				acDatepickerOptions: '@'
			},
			replace: true,
			template: template,
			link: function (scope, element, attrs, model) {

				// aliases are nice
				var jDatePicker = jQuery(element)
				var datePicker = element

				// clear datepicker value
				scope.clearValue = function () {
					scope.acValue = null
				}

				// show or hide the 'x' button
				scope.toggleHideClearButton = function (on) {
					var removeSection = datePicker.children().last()
					var inputSection = datePicker.children().first()
					if (on) {
						removeSection.css('display', 'none')
						inputSection.css('width', '99.95%')
					} else {
						removeSection.css('display', 'inline-block')
						inputSection.css('width', '84.95%')
					}
				}

				// toggle disabled field
				scope.toggleDisabled = function (on) {
					// disabled should hide clear button (should be dynamic)
					if (on) {
						scope.toggleHideClearButton(true)
						jDatePicker.css('background-color', '#eeeeee')
						scope.acDisabled = true
					} else {
						if (scope.acHideClearButton == true) {
							scope.toggleHideClearButton(true)
						} else {
							scope.toggleHideClearButton(false)
						}
						jDatePicker.css('background-color', 'white')
						scope.acDisabled = false
					}
				}

				// check for a valid SQL date
				scope.isValidDate = function (dateString) {
					if (typeof (dateString) == "string") {
						if (Date.parse(dateString).valueOf() >= minDate.valueOf()
							&& Date.parse(dateString).valueOf() <= maxDate.valueOf()) {
							return true
						}
					}
					return false
				}
	
				// check for a valid partial date
				scope.isDatePartial = function(dateString) {
					if (typeof dateString == "string") {
						var splitDate = dateString.split("/")
						var result = ""
						for (var i = 0; i < splitDate.length; i++) {
							if (splitDate[i] === "") {
								splitDate.splice(i, 1)
							}
						}
						if (splitDate.length === 2) {
							return true
						}
					}
					return false
				}

				// autocomplete a valid partial date with the current year
				scope.completeDatePartial = function(dateString) {
					if (typeof (dateString) == "string") {
						var splitDate = dateString.split("/")
						var result = ""
						for (var i = 0; i < splitDate.length; i++) {
							if (splitDate[i] === "") {
								splitDate.splice(i, 1)
							}
						}
						if (splitDate.length === 2) {
							return splitDate[0] + "/" + splitDate[1] + "/" + (new Date()).getFullYear()
						} else {
							return null
						}
					}
				}

				// set defaults if disabled properties not specified
				if (!scope.acDisabled) scope.acDisabled = false
				if (!scope.acHideClearButton) scope.acHideClearButton = false

				// set a default value, if a falsy one exists
				if (!scope.acValue) scope.acValue = null
				scope.acValue = acas.utility.formatting.formatDate(scope.acValue)
				if (scope.acValue === "") scope.acValue = null

				// toggle these settings at link time, for first time display
				scope.toggleHideClearButton(scope.acHideClearButton)
				scope.toggleDisabled(scope.acDisabled)

				// get input options
				// account for no options provide barebones defaults
				var options = angular.fromJson(scope.acDatepickerOptions)
				options = jQuery.extend({
					height: 24,
					width: 130,
					bootstrap: true,
					numberOfMonths: 2
				}, options)
				jDatePicker.css('border', '1px solid #cccccc')
					.css('height', options.height)
					.css('width', options.width)

				// apply input class
				jDatePicker.addClass(scope.acInputClass)

				// manually apply line height as a formatting concern 
				// (see MDN/W3C http://stackoverflow.com/questions/22719315/vertical-align-glyphicon-in-bootstrap-3)
				var removeButtonContainer = datePicker.children().last().children()
				removeButtonContainer.css('line-height', jDatePicker.css('height'))

				// display none on button hide, set the input width (should be dynamic)
				scope.$watch('acDisabled', function (newValue, oldValue) {
					if (oldValue != newValue) {
						if (newValue == true) {
							scope.toggleDisabled(true)
						} else {
							scope.toggleDisabled(false)
						}
					}
				})

				// display none on button hide, set the input width (should be dynamic)
				scope.$watch('acHideClearButton', function (newValue, oldValue) {
					if (oldValue != newValue) {
						if (newValue == true) {
							scope.toggleHideClearButton(true)
						} else {
							scope.toggleHideClearButton(false)
						}
					}
				})

				// check the existing date to prevent invalid bounds
				if (Date.parse(scope.acValue).valueOf() < minDate.valueOf()) {
					scope.acValue = minDate
				} else if (Date.parse(scope.acValue).valueOf() > maxDate.valueOf()) {
					scope.acValue = maxDate
				}

				// get input
				var input = element.children().first().children().first()
				var jInput = jQuery(input)

				// bootstrap look and feel
				if (options.bootstrap) {
					var oldBoxShadow = 'inset 0 1px 1px rgba(0, 0, 0, 0.075)'
					removeButtonContainer.css('box-shadow', oldBoxShadow)
					jDatePicker.css('transition', 'border-color ease-in-out 0.15s, box-shadow ease-in-out 0.15s')
						.css('box-shadow', oldBoxShadow)
					var oldBorderColor = jDatePicker.css('border-color')
					jInput.focus(function () {
						jDatePicker.css('border-color', '#66afe9')
							.css('box-shadow', 'inset 0 1px 1px rgba(0, 0, 0, 0.075), 0 0 8px rgba(102, 175, 233, 0.6)')
					})
					jInput.blur(function () {
						jDatePicker.css('border-color', oldBorderColor)
							.css('box-shadow', oldBoxShadow)
					})
				}

				// capture datepicker input
				input.datepicker({
					dateFormat: dateFormat,
					numberOfMonths: [1, options.numberOfMonths],
					minDate: minDate,
					maxDate: maxDate,
					// unfortunately, this only prevent alphanumeric characters from being entered
					constrainInput: true,
					onSelect: function (selection) {
						scope.$apply(function () {
							scope.acValue = acas.utility.formatting.formatDate(selection)
						});
					},
				});

				// capture input from the text field
				scope.$watch(function () { return scope.acValue }, function (newValue, oldValue) {
					if (oldValue != newValue) {
						if (scope.isValidDate(String(newValue)) || newValue == null || newValue == "") {
							if (newValue == "") {
								// keep values consistent, should be null when empty, not empty string
								newValue = null
								scope.acValue = null
							} else if (scope.isDatePartial(newValue)) {
								newValue = scope.completeDatePartial(newValue)
							}
							input.val(acas.utility.formatting.formatDate(newValue));
						} else {
							scope.acValue = acas.utility.formatting.formatDate(oldValue)
							input.val(acas.utility.formatting.formatDate(oldValue))
						}
					}
				})
			}
		}
	}])
})