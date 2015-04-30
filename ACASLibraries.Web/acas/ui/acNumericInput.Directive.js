
'use strict';
/***********************
 * Required parameter: 
 *      ac-value (a reference to a model value that will be bound)
 * Supports optional parameters: 
 *		ac-comments (no default - a reference to a model value that should appear in the comments popover)
 *		ac-readonly (no default - a reference to a model value. when it evaluates to true, the input will be readonly)
 *		ac-numeric-input-options - pass in a reference to a javascript object with option settings
 *			datatype: 'int', 'decimal'. default is 'decimal'
 *			value validation: minValue, maxValue, scale, precision: these control what can be typed in the field and saved to the database. defaults are dependent on the datatype, see the code
 *			display formatting: minDisplayPrecision, maxDisplayPrecision, percent, negativeStyle - these control how the value is displayed when the input is not focused. When the input is focused,
 *				the raw value is shown. defaults are 2, 2, false, null respectively
 *			styling: inputClass (class to be applied to non-readonly inputs), readonlyClass (applied to readonly inputs), datagridElement (when true, styling is applied to integrate the input into a datagrid)
 *		ng-blur: function call - ng-blur="boo()" --this will be executed AFTER the model is updated
 * 
 * We do not use ngModel to bind the input directly to the data because that introduces lag when the user is typing. Instead, we update the model
 * only on blur. This also gives us better control over the validation
 * 
 * Does not support ngShow
 */

acas.module('acNumericInput', 'acas.ui.angular', 'jquery', 'underscorejs', 'acValidation', function () {
	acas.ui.angular.directive('acNumericInput', ['acValidation', '$compile', function (acValidation, $compile) {
		return {
			restrict: 'E',			
			scope: {
				acValue: '=',
				ngBlur: '&',
				acComments: '=',
				acReadonly: '=',
				acNumericInputOptions: '=',
				acDisabled: '=?'				
			},
			//no template here. the template is set dynamically in the link function
			link: function (scope, el, attr, ctrl) {
				var input //to be populated when it's not readonly
				var options

				options = {
					datatype: 'decimal',
					//min, max, scale and precision control what can actually be typed in the field. might contain business logic, but defaults based on the datatype
					//to what that datatype in the db typically accepts.
					//min and max are primarily useful for ints, and scale and precision for decimals.
					//defaults will be set later based on datatype
					minValue: null,
					maxValue: null,
					//precision and scale are used here the way MSSQL uses them: http://technet.microsoft.com/en-us/library/ms187746.aspx
					//if precision is the same as scale, use int because this won't work
					precision: null,
					scale: null,


					//display precision affects how the values are displayed when focus is not in the input
					//when focused, the input always shows the actual value. defaults are 2,2
					//precision here refers to the number of digits AFTER the decimal 
					minDisplayPrecision: 2,
					maxDisplayPrecision: 2,

					//other formatting options
					inputClass: (scope.acNumericInputOptions && scope.acNumericInputOptions.datagridElement) ? '' : 'form-control',
					readonlyClass: '',
					negativeStyle: '',
					percent: false,
					datagridElement: false
				}

				_.extend(
					options,
					scope.acNumericInputOptions
				)

				//default options based on datatype
				switch (options.datatype) {
					case 'int':
						//int overrides any passed in precision
						options.minDisplayPrecision = 0
						options.maxDisplayPrecision = 0
						//based on sql server size for int
						options.minValue = options.minValue !== null ? options.minValue : Math.pow(-2, 31)
						options.maxValue = options.maxValue !== null ? options.maxValue : Math.pow(2, 31)
						options.scale = Infinity
						options.precision = Infinity
						break;
					case 'decimal':
						options.minValue = options.minValue !== null ? options.minValue : -Infinity //todo correct value
						options.maxValue = options.maxValue !== null ? options.maxValue : Infinity //todo correct value								
						options.precision = options.precision !== null ? options.precision : 25
						options.scale = options.scale !== null ? options.scale : 17 // length of max value
				}
							
				var generateTemplate = function (readonly, element) {
					var template
					if (readonly) {
						template = '<span style="float:right;text-align:right"></span>'
					}
					else {
						template = '<input style="text-align:right;"/>'
					}
					jQuery(template).insertBefore(element)
					var newElement = element.parent().children()[0]
					//assign the class attribute from the original element to the new one					
					var classes = (attr.class || '') + ' ' + (readonly ? options.readonlyClass || '' : options.inputClass || '')
					if (classes.trim() !== '') {
						newElement.classList.add(classes.trim())
					}
					$compile(newElement)(scope)
					element.remove()
					element = jQuery(newElement)
			
					return element
				}
				
				var format = function (value) {
					if (options.negativeStyle) {
						if (value < 0) {
							el[0].classList.add(options.negativeStyle);
						} else {
							el[0].classList.remove(options.negativeStyle);
						}
					}
					//thousandsSeparator and negative parenthesis are always true for now. we can make them options if necessary
					return acas.utility.formatting.formatNumber(value, options.minDisplayPrecision, options.maxDisplayPrecision, true, true, options.percent)
				}

				var validation = {
					isAboveMin: function (value) {
						return value >= parseFloat(options.minValue)
					},
					isBelowMax: function (value) {
						return value <= parseFloat(options.maxValue)
					},
					// sql precision/scale:
					// - precision: Total number of digits in the number
					// - scale: Number of digits allowed on the RHS of the decimal point
					// 123.45 has precision 5, scale 2
					isWithinPrecision: function (value) {
						return value.toString().split('.')[0].replace('-', '').length <= (options.precision - options.scale) || Infinity
					},
					isWithinScale: function (value) {
						return value.toString().indexOf('.') === -1 || value.toString().split('.')[1].length <= options.scale
					},
					isValidForDataType: function (value) {
						//for now this only contains a check for integers, decimals don't have datatype specific checks
						return (options.datatype === "int" && Math.floor(value) == value)//allow coercion intentionally: if value could be converted to the same value as the floor of itself, it's valid
							|| (options.datatype === "decimal")
					}

				}
				
				var setupInput = function () {
					//the core logic for the numeric input happens here
					var oldValue;

					input = el
					
					input.bind('keypress', function (event) {

						var character = String.fromCharCode(event.which || event.keyCode);

						if (!acas.utility.validation.isPrintableCharacter(event)) {
							//this might have been an arrow key, enter, etc. Don't prevent default						
							if (character === 13) {//the ENTER key needs to return false in some browsers or it causes problems
								return false
							}
							return //everything else needs to return - or else things like backspace wouldn't work
						}

						if (!acas.utility.validation.isValidNumericCharacter(character)) {
							//not an allowed character. cancel the event and we're done
							event.preventDefault()
							return
						}

						//TODO - check that it's all valid or could be valid with more keypresses	
						if (options.minValue === 0 && character === "-") {
							event.preventDefault()
							return
						}

						if (options.datatype === "int" && character === ".") {
							event.preventDefault()
							return
						}

					})

					input.bind('focus', function () {
						//grab the value from the model and display it as is (no formatting)
						input.val(scope.acValue)
						oldValue = scope.acValue
					})

					input.bind('blur', function () {
						//check that the entire number is valid
						var value = input.val()
						if ((acas.utility.validation.isValidNumber(value)
								&& validation.isAboveMin(value) && validation.isBelowMax(value)
								&& validation.isWithinPrecision(value) && validation.isWithinScale(value)
								&& validation.isValidForDataType(value))
								|| value === '') {

							var parsed = value === '' ? null : parseFloat(value) //TODO improve the way empty values are handled.
							if (parsed !== oldValue) { //the value changed, stick it in the model						
								scope.$apply(function () { scope.acValue = parsed })
							} else {
								//if case there was no change in value, the digest cycle wouldn't do anything. But we unformatted the value for editing, so we need to format it again.						
								input.val(format(value))
							}
						} else {
							input.val(format(oldValue)) //reset to what's in the model (original value)
						}
						scope.$apply(function () {
							scope.ngBlur()
						})
						
					})
					
					if (options.datagridElement) {
						//if it's in a td, clicking anywhere on the td will focus the input,
						//and focusing on the input will style the td. This gives the illusion of the entire td being the input box.
						//if it's not in a td, this part is ignored
						var td = jQuery(input).closest("td")[0]
						if (td) {
							input.bind('focus', function () {
								td.classList.add('ac-datagrid-cell-focused')
							})


							input.bind('blur', function () {
								td.classList.remove('ac-datagrid-cell-focused')
							})

							jQuery(td).click(function () {
								//before focusing make sure that the click in the td wasn't on the input already or on comments, if they exist
								if ((!commentsBox || !commentsTextBox || document.activeElement !== commentsTextBox[0]) && document.activeElement !== input[0]) {
									input.focus()
								}
							})
						}
					}

					//TODO - remove the tight coupling between comments and ac-numeric-input, use the ac-comments directive
					//Also, note that comments currently requires the the input be inside a TD
					//How will comments interact with acReadonly?
					if (attr.acComments) {
						var commentsBox
						var commentsTextBox
						var commentsIcon

						var autoShowOnInputFocus

						var setup = function () {
							//attach the glyph, opacity light if no comments, opacity dark if comments					
							var html = '<button class=" ac-comments-box-comment-icon" tabindex="-1"><span class="glyphicon glyphicon-comment" /></button>'
							commentsIcon = jQuery(jQuery(input.parent()).prepend(html).children(":first"))
							commentsIcon.click(function () {
								if (commentsBox) {
									hideComments()
								} else {
									showCommentsIcon()
									showComments(true)
								}

							})

							autoShowOnInputFocus = true
						}



						var hideComments = function (retainInputFocus, autoShowStatus) {
							if (!retainInputFocus) {
								td.classList.remove('ac-datagrid-cell-focused')
							}
							autoShowOnInputFocus = autoShowStatus
							scope.$apply(function () {
								if (commentsBox) {
									commentsBox.remove()
								}
								commentsBox = null
								scope.acComments = commentsTextBox.val().trim() || null
							})
						}

						var showComments = function (focus) {
							var comments = scope.acComments
							var leftSide = window.innerWidth - input.parents()[1].offsetLeft - input[0].offsetWidth < 450	//not sure 450 is the correct number, but it works for now on the forecasts screen								

							var html = '<span class="ac-comments-box ' + (leftSide ? 'left' : 'right') + '"><textarea tabindex = "-1" rows="4" columns="20">' + (comments ? comments : '') + ' </textarea><button tabindex="-1"><span class="glyphicon glyphicon-remove"></span></button></span>'


							if (leftSide) {
								commentsBox = jQuery(jQuery(input.parent()).prepend(html).children()[0])
							} else {
								commentsBox = jQuery(jQuery(input.parent()).append(html).children()[2])
							}
							commentsTextBox = commentsBox.children(":first")

							commentsTextBox.focus(function () {
								td.classList.add('ac-datagrid-cell-focused')
							})

							commentsTextBox.blur(function () {
								setTimeout(function () {//this timeout is to allow checking for clicks on the commentsIcon before hiding comments
									if (document.activeElement !== commentsIcon[0]) {
										var retainInputFocus = document.activeElement === input[0]
										var autoShowStatus = document.activeElement !== input[0]
										hideComments(retainInputFocus, autoShowStatus)
										hideCommentsIcon()
									}

								}, 5)


							})

							if (focus) {
								commentsTextBox.focus()
							}

						}

						var showCommentsIcon = function () {
							commentsIcon[0].classList.add('ac-comment-icon-show')
						}

						var hideCommentsIcon = function () {
							commentsIcon[0].classList.remove('ac-comment-icon-show')
						}

						setup()

						input.bind('focus', function () {

							setTimeout(function () { //this timeout is to prevent sliding comment icons and double comment boxes
								showCommentsIcon()
								if (scope.acComments && autoShowOnInputFocus) {
									showComments(false)
									autoShowOnInputFocus = false
								}
							}, 5)

						})



						input.bind('blur', function () {
							setTimeout(function () {
								if (!(commentsIcon[0] === document.activeElement || (commentsTextBox && commentsTextBox[0] === document.activeElement))) {
									if (commentsTextBox) {
										hideComments()
									}
									hideCommentsIcon()
									autoShowOnInputFocus = true
								}
							}, 5)

						})
					}

					//TODO the validation warning doesn't style properly when datagridElement is true
					if (options.validationKey) {
						var warningBox
						var html = validationService.validationMessageHtml('Please enter a value')
						var validate = function () {
							if (!input.val()) {
								input[0].classList.add('ac-warning-border')
								if (!warningBox) {
									warningBox = jQuery(jQuery(input.parents()[0]).prepend(html).children()[0]);
								}

								return false
							} else {
								input[0].classList.remove('ac-warning-border')
								if (warningBox) {
									warningBox.remove()
									warningBox = null
								}
								return true
							}
						}

						var validator = validationService.register(options.validationKey, validate)

						scope.$watch(function () { return input.val() }, function () {
							if (validator.active) {
								validator.validate()
							}
						})

						scope.$on('$destroy', function () {
							validationService.remove(validator)
						})
					}

					//initial disabled state, in case scope.acDisabled is already stabilized before the input is setup
					setDisabledState()
				}

				var setElementValue = function () {
					if (scope.acReadonly) {
						el.html(format(scope.acValue))
					} else {
						if (input) { //if input is still undefined, don't bother setting it
							input.val(format(scope.acValue))
						}
						
					}
				}

				var setDisabledState = function () {
					if (scope.acDisabled) {
						input.attr('disabled', 'disabled')
					} else {
						input.removeAttr('disabled')
					}
				}

				var init = function () {
					el = generateTemplate(!!scope.acReadonly, el)
					if (!scope.acReadonly) {
						//it's not readonly, there are stuff that need to be setup on the new input field
						//events and whatnot
						setupInput()					
					}
					//the new element's value must be populated from the model
					setElementValue()
				}				

				scope.$watch(function () { return scope.acReadonly },
					function (newValue, oldValue) {
						if (!!newValue !== !!oldValue) {
							init()
						}
					}
				)

				//setup disabled functionality
				scope.$watch(function() { return scope.acDisabled },
					function (newValue, oldValue) {
						if (!!newValue !== !!oldValue) {
							setDisabledState()
						}					
					})
				
				//watch the value and update the readonly span or input
				scope.$watch(function () { return scope.acValue },
					function () {
						setElementValue()							
					}
				)

				init()
			}
		}
	}])
})