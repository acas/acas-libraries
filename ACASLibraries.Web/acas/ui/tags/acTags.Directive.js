'use strict';

acas.module('acTags', 'acas.ui.angular', 'select2', 'underscorejs', function () {
	/*
	* acTags is essentially a multi-select dropdown displayed like tags. It currently does not support selecting items not in the options list,
	* but that will be added (hence the name 'tags').
	* 
	* Example:
	*	<ac-tags ac-options="someArrayOfObjects" ng-model="selectedItemsEndUpHere" />
	* 
	* Required attributes:
	*	- ac-options: array of objects to populate dropdown
	*	- ng-model: array of objects with selected items
	* 
	* Optional attributes:
	*	- ac-tags-options: an object containing any or all of the following properties
	*		- idProperty: string (default: 'id') - name of property on each object that serves as unique identifier
	*		- optionFormat: string|function (default: 'name') - for the dropdown items: either name of property on each object that should be displayed, or function returning a properly escaped html string
	*		- selectionFormat: string|function (default: 'name')  - for the selected items: either name of property on each object that should be displayed, or function returning a properly escaped html string
	*		- selectionClass: string|function - a class, or function that returns a class (the selected item is passed as the only parameter to the function). 
	*				The class will be applied to the li tags for the selected items. If selectionClass is used, selectionFormat must not be a function (otherwise just set the class in the selectionFormat function)
	*		- requireUserDescription: function taking a single option object and returning whether or not to prompt the user for a description string to attach to the option object and the model object
	*		- userDescriptionProperty: string, no default - required if requireUserDescription might return true. The name of the property to assign the value the user puts in the description prompt
	*		- matcher: string|function - defaults to optionFormat if optionFormat is a string, else 'name'. If string, user input searches case-insensitively on the property with that name
	*				If it's a function, it should return true for matches. It is supplied arguments: term (the user input), text (idk what this is), opt (the option to search for the term in)
	*	- acReadonly: bool (default: false) - when true, a read only view is displayed
	*/
	acas.ui.angular.directive('acTags', ['$timeout', '$window', function ($timeout, $window) {
		return {
			restrict: 'E',
			require: ['ngModel'],
			scope: {
				acOptions: '=', //an array of objects, must contain 'name' and 'id'
				ngModel: '=', //an array of objects, same as acOptions
				acTagsOptions: '=', //{idProperty: 'id'}
				acAllowNewOption: '=', //TODO
				acConfirmAddition: '=', //TODO
				acReadonly: '=',
				acDisabled: '=',
				acInputClass: '@' //any class/styling you would like to add to the control
			},
			replace: true,
			template: function () {
				return '<input class="ac-tags {{acInputClass}}" style="width:100%" type="hidden"/>'
			},
			link: function (scope, element) {
				var config = _.extend({
					idProperty: 'id',
					selectionFormat: 'name',
					optionFormat: 'name'					
				}, scope.acTagsOptions)

				//the matcher determines how searching works in the dropdown
				if (!config.matcher) {
					//no matcher was specified, setup a default:
					//case insensitive contains match on the optionFormat if it's a property, otherwise 'name'
					var prop = typeof (config.optionFormat) === 'string' ? config.optionFormat : 'name'
					config.matcher = function (term, text, opt) {
						return opt[prop].toLowerCase().indexOf(term.toLowerCase()) !== -1
					}
				}
				else if (typeof (config.matcher) === 'string') {
					//a matcher was specified in the directive options, but it's a string, not a function
					//do a case insensitive contains match on the property with that name
					config.matcher = function (term, text, opt) {
						return opt[config.matcher].toLowerCase().indexOf(term.toLowerCase()) !== -1
					}
				}
				
				var input = element
				//input.select2({ tags: function () { return scope.acSuggestions }, tokenSeparators: [","] })				
				input.select2({
					data: function () {
						return { results: scope.acOptions }
					},
					multiple: true,
					id: config.idProperty,
					matcher: config.matcher,
					formatResult: typeof (config.optionFormat) === 'function' ? config.optionFormat : function (item) { return item[config.optionFormat] },
					formatSelection: typeof (config.selectionFormat) === 'function' ? config.selectionFormat :
						function (item, container) {
							var selectionClass = (typeof (config.selectionClass) === 'function' ? config.selectionClass(item) : (config.selectionClass || ''))
							if (selectionClass) {
								if (typeof selectionClass === 'object') { //array of strings
									for (var i = 0; i < selectionClass.length; i++) {
										container.parent()[0].classList.add(selectionClass[i])
									}
								} else {
									container.parent()[0].classList.add(selectionClass)
								}

							}
							return item[config.optionFormat]
						}
				})

				scope.$watch(function () { return scope.acReadonly }, function () {
					input.select2("readonly", scope.acReadonly)				
				})

				scope.$watch(function () {return scope.acDisabled}, function () {
					input.select2("enable", !scope.acDisabled)
				})

				var copyPropertiesToOptions = function (model, options) {
					for (var i = 0; i < model.length; i++) {
						var option = _.find(options, function (x) { return x[config.idProperty] === model[i][config.idProperty] })
						if (option) {
							_.extend(option, model[i])
						}						
					}
				}
				//programmatic changes to the model should update the input with the correct ids
				//need to get properties of the model objects over to the input options objects, too
				scope.$watch(function () { return scope.ngModel },
					function (newValue, oldValue) {
						//the following check says that if the model's already been loaded, disregard whether or not the value has changed.
						//this allows us to run this code the first time to initialize the value, but not until after the model's been loaded
						//perhaps there's a better way to accomplish this that'll generate less processing waste						
						if (newValue !== oldValue || scope.ngModel) { 
							copyPropertiesToOptions(scope.ngModel, scope.acOptions)
							input.select2('val', _.pluck(scope.ngModel, config.idProperty))
						}						
					}
					, true
				)
			
				//if the options change, remove any unavailable selections from the model and reset the input val to the model
				//this will also allow the input to pick up items that were in the model but were previously not in the options				
				scope.$watch(function () { return scope.acOptions },
					function (newValue, oldValue) {
						if (newValue !== oldValue) {
							scope.ngModel = _.reject(scope.ngModel, function (x) { return _.pluck(scope.acOptions, config.idProperty).indexOf(x[config.idProperty]) === -1 })
							copyPropertiesToOptions(scope.ngModel, scope.acOptions)
							input.select2('val', _.pluck(scope.ngModel, config.idProperty))
						}						
					}
					, true
				)

				//manual change of the input should update the model
				//if necessary, add a property to both the options and the model
				//it needs to be on the options for display
				input.change(function (event) {
					$timeout(function () {
						scope.$apply(function () {							
							var values = input.val().split(',')
							if (config.requireUserDescription) {
								if (event.added && config.requireUserDescription(event.added)) {
									event.added[config.userDescriptionProperty] = $window.prompt('Please enter a description:', '')
									if (!event.added[config.userDescriptionProperty]) {
										//remove the last item from the select
										values = values.slice(0, values.length - 1)
										input.select2('val', values) 
									}
								}
								else if (event.removed && config.requireUserDescription(event.removed)) {
									event.removed[config.userDescriptionProperty] = undefined
								}
							}							
							scope.ngModel = _.filter(scope.acOptions, function (x) { return _.filter(values, function (y) { return y == x[config.idProperty] }).length })
						})
					})

				})
			}
		}
	}])
})
