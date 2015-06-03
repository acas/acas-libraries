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
					idProperty: 'id', // if optionFormat is not used, both this and nameProperty are used
					nameProperty: 'name', // if not passed in will be set to same as dataSearch if dataSearch is not a function, if dataSearch is array it will be first element
					selectionFormat: 'name',
					searchPath: '', //if searchPath is used, it will override data/dataSearch
					data: null, //array of objects or can be a function that returns an array - with large data sets a function should always be passed
					dataSearch: 'name', // property or properties to be searched, can be a function, a string (property name), or an array of strings to search on multiple properties
					optionFormat: null, //if not passed in, template uses nameProperty of item in watch instead of bind
					inputClass: 'form-control'
				}, scope.acTypeaheadOptions)
				scope.config = config //so that it can be accessed in the template   

				if (!scope.config.searchPath && !scope.config.data) {
					throw 'ERROR: acTypeahead must use either searchPath or both data and dataSearch. Please see acas libraries code.'
				}

				if (scope.config.optionFormat && typeof (scope.config.optionFormat) !== 'function') {
					scope.config.optionFormat = function(item) {return item[scope.config.optionFormat]}
				}

				// to support searching on multiple columns, use array by default, and if a string literal convert to array
				if (typeof (scope.config.dataSearch) === 'string') {
					scope.config.dataSearch = [scope.config.dataSearch]
				}

				// default nameProperty to be same as dataSearch if not explicitly set in options, if dataSearch is not function:
				if (scope.config.nameProperty === 'name' && typeof (scope.config.dataSearch) !== 'function' && scope.config.dataSearch[0] !== 'name') {
					scope.config.nameProperty = scope.config.dataSearch[0]
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
						var results = []
						if (typeof (scope.config.dataSearch) === 'function') {
							results = scope.config.dataSearch(scope.config.data, search)
						} else {
							// if data is coming from a function, invoke function, otherwise get data
							var data = typeof (scope.config.data) === 'function' ? scope.config.data() : scope.config.data
							//dataSearch should be a string array  - see above, if string literal is passed it's converted to single-element array, allows for searching on any number of properties
							var searchProperties = scope.config.dataSearch
							for (var i = 0; i < data.length; i++) {
								for (var j = 0; j < searchProperties.length; j++) {
									if (data[i][searchProperties[j]] && data[i][searchProperties[j]].toString().toLowerCase().indexOf(search.trim().toLowerCase()) !== -1) {
										results.push(data[i])
										break
									}
								}
							}
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
									scope.searchResults = data
									scope.activeItem = -1
									if (showResults) {
										scope.showBox = true
									}
								})
							})														
						}
						else {
							$timeout(function () {
								scope.searchResults = []
								if (showResults) {
									scope.showBox = true
								}
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
						var inputTemplate = '<input class="ac-typeahead {{config.inputClass}}" placeholder="Search..."  ng-model="display" ng-model-options="{ debounce: 300 }" ng-change="ngChange" ng-disabled="ngDisabled"/>'
						var dropdownTemplatePre = '<div ng-show="showBox" class="ac-typeahead-dropdown" tabindex="-1">'
						if (scope.config.optionFormat) {
							optionTemplateHtml = '<div class="ac-typeahead-dropdown-item" ng-class="{active: activeItem === $index}" ng-repeat="item in searchResults" ng-click="selectItem(item)" ng-bind-html="config.optionFormat(item)"></div>'
						}
						else {
							optionTemplateHtml = '<div class="ac-typeahead-dropdown-item" ng-class="{active: activeItem === $index}" ng-repeat="item in searchResults" ng-click="selectItem(item)" >{{item[config.nameProperty]}}</div>'
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
									scope.ngModel = value
									value = {}
								}
							})
						}
					)
				}
				// otherwise handled the old way, where value is id/name pair and model is just the id
				else {
					//watch the display - if it changes programmatically, update the value
					scope.$watch(function () { return scope.acDisplay },
						function () {
							value = {}
							value[scope.config.idProperty] = scope.ngModel
							value[scope.config.nameProperty] = scope.acDisplay

							search()
						}
					)

					//when the value changes, update both the display and the model
					scope.$watch(function () { return value },
						function () {
							$timeout(function () {
								scope.ngModel = value[scope.config.idProperty],
								scope.acDisplay = value[scope.config.nameProperty],
								scope.display = value[scope.config.nameProperty]					
							})
						}	
					)
				}

				//when selecting an item, update value
				scope.selectItem = function (item) {
					$timeout(function () {		
						value = item
						scope.showBox = false
						scope.searchResults = {}
					})
				}

				var setupInput = function () {
					//on blur, update the model back to value - if user typed but didn't select a new item to be placed in value, this resets the display
					var leave = function () {
						$timeout(function () {
							//when you blur, reset the model to value in the model							
							//but if there's only one item in searchResults, use that item
							//also, if it's empty, clear the model
							if (scope.searchResults.length === 1) {
								scope.selectItem(scope.searchResults[0])
							} else if (input.val().trim() === '') {
								scope.ngModel = null
								scope.display = null
							} else {
								scope.ngModel = value[scope.config.idProperty]
								scope.display = value[scope.config.nameProperty]									
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
									$timeout(function () { scope.showBox = true })
								}
								break
						}
						scope.$apply()
					})

					//search every keystroke
					input.on('keyup textInput', function (event) {
						if ((event.type === 'textInput' || !_.contains([40, 38, 13], event.which)) && (input && input.val().trim().length > 2)) {
							search(true)
						}
					})
					//show box on focus or click, click toggles it
					input.on('click', function () {
						$timeout(function () {
							scope.showBox = !scope.showBox
						})
					})

					input.on('focus', function () {
						$timeout(function () { //the timeout prevents this from firing before the click event if clicking on the box when not focused
							//this prevents it from opening and immediately shutting on focus/click collision
							scope.showBox = true
							
						}, 300)
					})
				}		
			}
		}
	}])
})
