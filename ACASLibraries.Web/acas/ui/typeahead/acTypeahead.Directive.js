'use strict';

acas.module('acTypeahead', 'acas.ui.angular', 'select2', 'underscorejs', function () {

	acas.ui.angular.directive('acTypeahead', ['$timeout', '$window', '$compile', '$http', function ($timeout, $window, $compile, $http) {


		return {
			restrict: 'E',
			require: ['ngModel'],
			scope: {
				acSearchPath: '@',
				ngModel: '=',
				acTypeaheadOptions: '=', //{idProperty: 'id'}								
				acReadonly: '=',
				acDisplay: '=',
				acDisabled: '=',
				acInputClass: '@' //any class/styling you would like to add to the control
			},
			replace: true,
			link: function (scope, element) {
				var config = _.extend({
					idProperty: 'id',
					selectionFormat: 'name',
					optionFormat: function (item) { return item.name },
					inputClass: 'form-control'
				}, scope.acTypeaheadOptions)
				scope.config = config //so that it can be accessed in the template

				scope.showBox = false
				scope.activeItem = -1
				var input
				var dropdown

				var count = 0
				var value
				var search = function (showResults) {
					//perform the search, update scope.searchResults					
					count++
					var current = count
					$timeout(function () {
						if (count !== current) return //don't run too many search if the user is typing fast, only the last should be run
						if (input && input.val().trim().length > 2) {
							$http({ method: 'GET', url: scope.acSearchPath, params: { query: input.val().trim() } })
								.success(function (data) {
									$timeout(function () {
										scope.$apply(function () {
											scope.searchResults = data
											scope.activeItem = -1
											if (showResults) {
												scope.showBox = true
											}
										})
									})
								})

						}
						else {
							$timeout(function () {
								scope.$apply(function () {
									scope.searchResults = []
									if (showResults) {
										scope.showBox = true
									}
								})
							})
						}

					}, 250)
				}

				var generateTemplate = function () {
					var html
					if (scope.acReadonly) {
						element.html('<span>{{display}}</>')
					}
					else {
						var inputTemplate = '<input class="ac-typeahead {{config.inputClass}}" placeholder="Search..."  ng-model="display" ng-disabled="acDisabled"/>'
						var dropdownTemplatePre = '<div ng-show="showBox" class="ac-typeahead-dropdown" tabindex="-1">'
						var optionTemplateHtml = '<div class="ac-typeahead-dropdown-item" ng-class="{active: activeItem === $index}" ng-repeat="item in searchResults" ng-click="selectItem(item)" ng-bind-html="config.optionFormat(item)"></div>'
						var noResultsTemplate = '<div class="ac-typeahead-dropdown-item no-results" ng-show="!searchResults.length">No Results Found</div>'
						var html = '<div class="ac-typeahead">' + inputTemplate + dropdownTemplatePre + optionTemplateHtml + noResultsTemplate + '</div></div>'
						element.html(html)
						input = element.children().children(":first")
						dropdown = element.children().children(":last")
						setupInput()
					}					
					$compile(element.contents())(scope)
				}


				scope.$watch(function () { return scope.acReadonly }, 
					function () {
						generateTemplate()
					}					
				)
				
				//watch the display - if it changes programmatically, update the value
				scope.$watch(function () { return scope.acDisplay },
					function () {
						value = {
							id: scope.ngModel,
							name: scope.acDisplay
						}
						search()
					}
				)

				//when the value changes, update both the display and the model
				scope.$watch(function () { return value },
					function () {
						$timeout(function () {
							scope.$apply(function () {
								scope.ngModel = value.id,
								scope.acDisplay = value.name,
								scope.display = value.name
							})
						})
					}
				)

				//when selecting an item, update value
				scope.selectItem = function (item) {
					$timeout(function () {
						scope.$apply(function () {
							value = item
							scope.showBox = false
						})
					})
				}

				var setupInput = function () {
					//on blur, update the model back to value - if user typed but didn't select a new item to be placed in value, this resets the display
					var leave = function () {
						scope.$apply(function () {
							//when you blur, reset the model to value in the model							
							//but if there's only one item in searchResults, use that item
							//also, if it's empty, clear the model
							if (scope.searchResults.length === 1) {
								scope.selectItem(scope.searchResults[0])
							} else if (input.val().trim() === '') {
								scope.ngModel = null
								scope.display = null								
							} else {
								scope.ngModel = value.id //todo
								scope.display = value.name //todo									
							}
							scope.showBox = false
						})
					}

					input.on('blur', function () {
						$timeout(function () {
							if (document.activeElement !== dropdown[0]) {
								leave()
							}							
						}, 100)
					})

					dropdown.on('blur', function () {
						$timeout(function () {
							if (document.activeElement !== input[0]) {
								leave()
							}
						}, 100)
					})

					//support keyboard navigation
					input.on('keydown', function (event) {
						switch (event.which) {
							case 40:
								scope.activeItem++
								break
							case 38:
								scope.activeItem--
								break
							case 13:
								if (scope.showBox && scope.searchResults.length > scope.activeItem && scope.activeItem !== -1) {
									scope.selectItem(scope.searchResults[scope.activeItem])
								} else if (!scope.showBox) {
									scope.$apply(function () { scope.showBox = true })
								}
								break
						}
						scope.$apply()
					})

					//search every keystroke
					input.on('keyup textInput', function (event) {
						if (event.type === 'textInput' || !_.contains([40, 38, 13], event.which)) {
							search(true)
						}
					})
					//show box on focus or click, click toggles it
					input.on('click', function () {
						scope.$apply(function () {
							scope.showBox = !scope.showBox
						})
					})

					input.on('focus', function () {
						$timeout(function () { //the timeout prevents this from firing before the click event if clicking on the box when not focused
							//this prevents it from opening and immediately shutting on focus/click collision
							scope.$apply(function () {
								scope.showBox = true
							})
						}, 300)
					})
				}		
			}
		}
	}])
})
