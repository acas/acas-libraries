/*
Directives such as ac-select and ac-validate register themselves with the validation service using a validation key that's unique to the section of the app that gets validated as a unit.
The validation service is then injected into your controller, and the validateAll(key) method is called before saving. It does two things: returns whether or not all items with that key
are valid, and activates validation for all those items (after which any change to the model should trigger the validator functions.

*/
acas.module('acValidation', 'acas.angular', 'underscorejs', function () {
	acas.angular.factory('acValidation', [function () {
		var validationService = new function () {
			var utilities = {
				validations: []
			}
			var api = {
				register: function (key, func) {

					utilities.validations.push({
						key: key,
						active: false,
						validate: function () {
							this.active = true
							return func()
						}
					})
					return utilities.validations[utilities.validations.length - 1]
				},

				remove: function (validator) {
					utilities.validations.splice(utilities.validations.indexOf(validator), 1)
				},

				validateAll: function (key) {
					var valid = true
					_.each(_.where(utilities.validations, { key: key }), function (item) {
						if (!item.validate()) {
							valid = false
						}
					})
					return valid
				},
				//we should use this html fragment wherever we use validation service, for consistency. The message can cater to the type of control using the service
				validationMessageHtml: function (message) {
					//message should be something like "Please enter a value" or "Please select a value"
					return '<span class="label label-danger" style="position:absolute;z-index: 999;margin-top:-15px;border-radius: .25em .25em 0 0;margin-left: 2px;opacity:.8">' + message + '</span>'
				}
			}

			return api
		}

		return validationService

	}])
})

