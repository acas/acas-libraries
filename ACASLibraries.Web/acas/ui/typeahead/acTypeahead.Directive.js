'use strict'; 

acas.module('acTypeahead', 'acas.ui.angular', 'underscorejs', function () {
	acas.ui.angular.directive('acTypeahead', ['$timeout', '$window', '$compile', '$http', '$q', function ($timeout, $window, $compile, $http, $q) {
		return {
			restrict: 'E',
			scope: {				
				ngModel: '=',
				acTypeaheadOptions: '=', //see config object below for defaults
				acReadonly: '=',
				acDisplay: '=',
				ngDisabled: '=',
				ngChange: '&',
			},
			replace: true,
			link: function (scope, element) {
				var config = _.extend({
					idProperty: 'id', // if optionFormat is not used, both this and displayProperty are used
					displayProperty: 'name', // if not passed in will be set to same as dataSearch if dataSearch is not a function
					selectionFormat: 'name',
					searchPath: '', //if searchPath is used, it will override data/dataSearch
					data: null, //array of objects
					dataSearch: 'name', //either function or name of property
					optionFormat: null, //if not passed in, template uses displayProperty of item in watch instead of bind
					inputClass: 'form-control'
				}, scope.acTypeaheadOptions)
				scope.config = config //so that it can be accessed in the template

				if (!scope.config.searchPath && !scope.config.data) {
					throw 'ERROR: acTypeahead must use either searchPath or both data and dataSearch. Please see acas libraries code.'
				}

				if (scope.config.optionFormat && typeof (scope.config.optionFormat) !== 'function') {
					scope.config.optionFormat = function(item) {return item[scope.config.optionFormat]}
				}

				// default displayProperty to be same as dataSearch if not explicitly set in options, if dataSearch is not function:
				if (scope.config.displayProperty === 'name' && typeof (scope.config.dataSearch) !== 'function' && scope.config.dataSearch !== 'name') {
					scope.config.displayProperty = scope.config.dataSearch
				}

				scope.showBox = false
				scope.activeItem = -1
				var input
				var dropdown

				var count = 0
				var value

				var getData = function (search) {
					var deferred = $q.defer()
					if (scope.config.searchPath) {
						$http({ method: 'GET', url: scope.config.searchPath, params: { query: search } })
							.success(function (data) {
								deferred.resolve(data)
							})
	
					} else {
						var results
						if (typeof (scope.config.dataSearch) === 'function') {
							results = scope.config.dataSearch(scope.config.data, search)
						} else { //should be a string
							results = _.filter(scope.config.data, function (x) {
							
							return x[scope.config.dataSearch].toString().toLowerCase().indexOf(search.trim().toLowerCase()) !== -1
							})
						}

						deferred.resolve(results)
					}
					return deferred.promise
					
				}
				
				var search = function (showResults) {
					//perform the search, update scope.searchResults					
					count++
					var current = count
					$timeout(function () {
						if (count !== current) return //don't run too many search if the user is typing fast, only the last should be run
						if (input && input.val().trim().length > 2) {
							getData(input.val().trim()).then(function (data) {
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
						var optionTemplateHtml
						var inputTemplate = '<input class="ac-typeahead {{config.inputClass}}" placeholder="Search..."  ng-model="display" ng-change="ngChange" ng-disabled="ngDisabled"/>'
						var dropdownTemplatePre = '<div ng-show="showBox" class="ac-typeahead-dropdown" tabindex="-1">'
						if (scope.config.optionFormat) {
							optionTemplateHtml = '<div class="ac-typeahead-dropdown-item" ng-class="{active: activeItem === $index}" ng-repeat="item in searchResults" ng-click="selectItem(item)" ng-bind-html="config.optionFormat(item)"></div>'
						}
						else {
							optionTemplateHtml = '<div class="ac-typeahead-dropdown-item" ng-class="{active: activeItem === $index}" ng-repeat="item in searchResults" ng-click="selectItem(item)" >{{item[config.displayProperty]}}</div>'
						}
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

				// For selecting an item, need completely separate execution path at watch definition for objects,
				// otherwise other processes may nullify ngModel and the null non-object gets assigned an int.
				// When ngmodel is an object the directive needs to treat both value and model as objects
				// - allows two way binding and object changes can be handled with a watch in the controller
				if (scope.ngModel && typeof (scope.ngModel) === 'object') {
					scope.$watch(function () { return scope.acDisplay },
						function () {
							value = {}
							search()
						}
					)
					// when value changes and object is not empty, assign it to model
					scope.$watch(function () { return value },
						function () {
							$timeout(function () {
								if (Object.keys(value).length > 0) {
									scope.$apply(function () {
										scope.ngModel = value
										value = {}
									})
								}
							})
							
						}
					)
				}
				// otherwise handled the old way, where value is id/name pair and model is just the id
				else if (scope.ngModel) {
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
									scope.ngModel = value[scope.config.idProperty],
									scope.acDisplay = value.name,
									scope.display = value.name
								})
							})
						}	
					)
				}

				//when selecting an item, update value
				scope.selectItem = function (item) {
					$timeout(function () {
						scope.$apply(function () {
							value = item
							scope.showBox = false
							scope.searchResults = {}
						})
						console.log(value)
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
