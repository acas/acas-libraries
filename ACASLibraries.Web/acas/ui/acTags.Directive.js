'use strict';

acas.module('acTags', 'select2', function () {
	acas.ui.angular.directive('acTags', ['$timeout', function ($timeout) {
		return {
			restrict: 'E',
			scope: {
				acSuggestions: '=',
				acValues: '=',
				acConfirmAddition: '=',
				acReadonly: '='
			},
			replace: true,
			template: function () {
				return '<input class="ac-tags" />'
			},
			link: function (scope, element) {
				var input = element
				input.select2({ tags: function () { return scope.acSuggestions }, tokenSeparators: [","] })

				scope.$watch(function () { scope.acReadonly }, function () {
					input.select2("readonly", scope.acReadonly)
					//input.select2("enable", !scope.acReadonly)
				})

				scope.$watch(function () { return scope.acValues },
					function () {
						if (!_.isEqual(_.reject(input.val().split(','), function (x) { return x.trim() === "" }), scope.acValues)) {
							input.val(scope.acValues && scope.acValues.join ? scope.acValues.join(',') : []).trigger("change")
						}
					}
				)
				input.change(function (event) {
					if (!_.isEqual(_.reject(input.val().split(','), function (x) { return x.trim() === "" }), scope.acValues)) { //check that there's an actual change in values
						$timeout(function () {
							scope.$apply(function () {
								var values = _.reject(input.val().split(','), function (x) { return x.trim() === "" })

								//we may need to add a new value to the suggestions, and potentially confirm addition. 
								//if there are no values, skip this part
								var value
								var alreadyExists = false
								if (values.length !== 0) {
									value = values[values.length - 1] //if there's a new value, it'll be last in the list. this value might not be new at all, or it might already exist. Either way is fine
									for (var i = 0; i < scope.acSuggestions.length; i++) {
										if (scope.acSuggestions[i] === value) {
											alreadyExists = true
										}
									}

									if (!alreadyExists) {
										scope.acSuggestions.push(value)
									}
								}



								//if a acConfirmAddition function was supplied with the directive, and this is a new value, confirm addition before modifying the model.
								//otherwise, just modify the model.
								if (value && !alreadyExists && scope.acConfirmAddition) {
									scope.acConfirmAddition(value).then(function (result) {
										$timeout(function () {
											scope.$apply(function () {
												if (!result) {
													values.pop()
													scope.acSuggestions.pop()
												}
												scope.acValues = values
											})
										})
									})

								}
								else {
									scope.acValues = values
								}
							})
						})
					}
				})
			}
		}
	}])
})
