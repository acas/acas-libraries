'use strict';

acas.module('acas.utility', 'underscorejs', function () {		
	_.extend(acas.utility, {
			isIE9: function () {
				return document.all && !window.atob;
			},
			event:  function (eventType) {
				var event = {
					type: eventType,
					subscribers: [],
					bind: function (handler) {
						event.subscribers.push(handler);
					},
					unbind: function (handler) {
						event.subscribers.push(handler);
					},
					trigger: function (e) {
						var propagating = true;
						_.extend(e, {
							type: event.type,
							timestamp: new Date(),
							stopImmediatePropagation: function () {
								propagating = false;
							}
						})
						for (var x = 0; x < event.subscribers.length && propagating; x++) {
							event.subscribers[x](e);
						}
					}
				};

				return event;
			},
			formatting: {
				padZero: function (string, minLength) {
					//TODO test this function
					//Pads numbers with zeros on the left side to a minimum length (if necessary)
					string = string.toString();
					if (string.length >= minLength) {
						return string;
					}

					var zeroes = new Array(minLength).join("0");
					var string = zeroes + string;
					return string.substr(string.length - minLength, minLength);

				},
				formatDate: function (date) {
					//use this to format dates for display in the UI
					if (date === null || typeof (date) === 'undefined') {
						return '';
					}
					date = acas.utility.parser.toDate(date);
					var year = date.getFullYear();
					var month = date.getMonth();
					var day = date.getDate();
					return (month + 1).toString() + '/' + day.toString() + '/' + year.toString();
				},
				//please don't use this to format dates for the UI - use formatDate() instead. Use this if you need to format a date for some other use.
				formatDateFixedLength: function (date) {
					if (date === null) {
						return '';
					}
					date = acas.utility.parser.toDate(date);
					var year = date.getFullYear();
					var month = date.getMonth();
					var day = date.getDate();
					return formatting.padZero((month + 1), 2) + '/' + formatting.padZero(day, 2) + '/' + year;
				},

				formatTime: function (date) {
					if (date === null) {
						return '';
					}
					date = acas.utility.parser.toDate(date);
					var hours = date.getHours();
					var minutes = date.getMinutes();
					minutes = minutes < 10 ? '0' + minutes : minutes;
					var ampm = "AM";
					if (hours >= 12) {
						ampm = "PM";
						hours = hours - 12;
					}
					if (hours == 0) {
						hours = 12;
					}
					return hours + ':' + minutes + ' ' + ampm;
				},
				formatDateTime: function (date) {
					return this.formatDate(date) + ' ' + this.formatTime(date);
				},
				formatUsername: function (username) {
					if (username === null) {
						return '';
					}

					var slashIndex = username.lastIndexOf('\\');
					if (slashIndex > -1) {
						username = username.substring(slashIndex + 1);
					}
					var dotIndex = username.lastIndexOf('@');
					if (dotIndex > -1) {
						username = username.substring(dotIndex + 1);
					}
					username = username.replace('.', ' ');
					return username;

				},
				//takes a number and formats it - returns a string
				//will not work with a maxPrecision of zero
				formatNumber: function (value, minPrecision, maxPrecision, thousandsSeparator, negativeParenthesis, percent) {
					if (minPrecision === undefined || minPrecision === null) {
						minPrecision = acas.config.numericDisplayDefaults.minPrecision
					}
					if (maxPrecision === undefined || maxPrecision === null) {
						maxPrecision = acas.config.numericDisplayDefaults.maxPrecision
					}

					if (thousandsSeparator === undefined || thousandsSeparator === null) {
						thousandsSeparator = acas.config.numericDisplayDefaults.thousandsSeparator
					}
				
					if (negativeParenthesis === undefined || negativeParenthesis === null) {
						negativeParenthesis = acas.config.numericDisplayDefaults.negativeParenthesis
					}

					percent = !!percent

					//todo if value isn't a number, return null
					//address common issues: 
					if (typeof (value) === 'string' && value.indexOf(',') !== -1) { //remove commas from string
						value = value.split(',').join('')
					}
					if (typeof (value) === 'string' && value.indexOf('.') !== value.lastIndexOf('.')) { //more than one decimal point
						return null
					}
					
					value = parseFloat(value)
					if (isNaN(value)) {
						return null
					}
					
					//handle percent - multiply by 100 before doing all the rounding and stuff
					if (percent) {
						value = value * 100						
					}

					var negative = value < 0
					var stringValue = value.toString().substr(negative ? 1 : 0)
					var decimalIndex = stringValue.indexOf('.')
					var integer = decimalIndex === -1 ? stringValue : stringValue.substr(0, decimalIndex) || "0"
					var decimal = decimalIndex === -1 ? '' : stringValue.substr(decimalIndex + 1)
					var zeroes = "0000000000000000000000000000000000000000000000000000000000000000000000"

					//deal with precision. first the rounding
					if (decimal.length > maxPrecision) {
						if (decimal.charAt(maxPrecision) >= 5) {
							decimal = (parseInt(decimal.substr(0, maxPrecision)) + 1).toString()
							if (isNaN(decimal) || decimal.length > maxPrecision) {
								decimal = ''
								integer = (parseInt(integer) + 1).toString()
							}
						}
						else {
							decimal = decimal.substr(0, maxPrecision)
							if (isNaN(decimal)) {
								decimal = ''
							}
						}
						//after rounding, we could be left with just trailing zeroes. get rid of them
						while (decimal.charAt(decimal.length - 1) === "0") {
							decimal = decimal.substr(0, decimal.length - 1)
						}
					}

					//pad the zeroes to minPrecision. It's possible that this is hit even though we started longer than maxPrecision, after rounding and removing trailing zeroes
					if (decimal.length < minPrecision) {
						decimal = (decimal + zeroes).substr(0, minPrecision)
					}

					//put the commas in the integer
					if (thousandsSeparator) {
						var i = 0;
						while (i < integer.length) {
							i += 3;
							if (i < integer.length) {
								integer = integer.slice(0, integer.length - i) + ',' + integer.slice(integer.length - i)
								i += 1
							}
						}
					}

					//combine the pieces
					var result = integer + (decimal !== '' ? '.' + decimal : '')

					//handle percent
					if (percent) {
						result = result + '%'
					}

					//deal with negative formatting
					if (negative) {
						if (negativeParenthesis) {
							result = "(" + result + ")"
						}
						else {
							result = "-" + result
						}
					}
					return result;
				}
			},
			validation: {
				isValidDate: function (dateValue) {
					var date = new Date(dateValue);
					return (date instanceof Date && !isNaN(date.valueOf()))
				},

				//determines whether a single character is valid inside of a number. essentially just digits, decimal (.), and minus sign (-)
				isValidNumericCharacter: function (character) {
					var VALID_CHARACTERS_REGEX = /^[0-9-.]$/; //a single numeric character or - and .
					return VALID_CHARACTERS_REGEX.test(character)
				},

				//determines whether the value is a valid date
				isDate: function (dateValue) {
					if (_.isEmpty(dateValue)) {
						return false;
					} else {
						var date = new Date(dateValue);
						if (date instanceof Date && _.isNumber(date.valueOf())) {
							return true;
						} else {
							return false;
						}
					}
				},

				//determines whether a keypress event is a printable character. Useful for filtering out keypresses like tab, backspace, enter, etc.
				//incomplete list
				isPrintableCharacter: function (event) {
					return !event.ctrlKey && !_.contains([0, 8, 9, 13], event.which) //TODO add to this list			
				},

				//determines whether a value is a valid number or not. TODO - not tested very well
				isValidNumber: function (value) {
					var NUMBER_REGEXP = /^\s*(\-|\+)?(\d+|(\d*(\.\d*)))\s*$/;
					return NUMBER_REGEXP.test(value)
					//return !(_.isEmpty(numberValue) || _.isNaN(numberValue)) 				
				}
			},		
			array: {
				argumentsToArray: function (args, includeCalleeInArray) {
					if (args === undefined || args === null) {
						return args;
					}
					else {
						var output = new Array();
						for (var x = 0; args.length; x++) {
							output.push(args[x]);
						}
						if (includeCalleeInArray === true) {
							output.push(arguments.callee);
						}
						return output;
					}
				},

				sortByKey: function (array, key) {
					return array.sort(function (a, b) {
						var x = a[key]; var y = b[key];
						return ((x < y) ? -1 : ((x > y) ? 1 : 0));
					});
				},

				forceArray: function (object) {
					if (!_.isArray(object)) {
						object = [_.clone(object)];
					}
					return object;
				},

				objectToArray: function (input, orderBy) {
					//converts an object's non-function properties to elements in an array
					//the property names are lost
					//optionally orders the output by a given property of each property
					//this is useful if the properties of the object are objects themselves
					var out = [];
					for (i in input) {
						if (typeof (input[i]) !== 'function') {
							out.push(input[i]);
						}
					}
					if (orderBy) {
						out = out.sort(function (a, b) { return a[orderBy] > b[orderBy] })
					}
					return out;
				}
			},
			html: {
				getScrollBarWidth: function () {
					var scrollBarWidth;
					if (scrollBarWidth === undefined) {
						var inner = document.createElement('p');
						inner.style.width = "100%";
						inner.style.height = "200px";

						var outer = document.createElement('div');
						outer.style.position = "absolute";
						outer.style.top = "0px";
						outer.style.left = "0px";
						outer.style.visibility = "hidden";
						outer.style.width = "200px";
						outer.style.height = "150px";
						outer.style.overflow = "hidden";
						outer.appendChild(inner);

						document.body.appendChild(outer);
						var w1 = inner.offsetWidth;
						outer.style.overflow = 'scroll';
						var w2 = inner.offsetWidth;
						if (w1 == w2) w2 = outer.clientWidth;

						document.body.removeChild(outer);

						scrollBarWidth = (w1 - w2);
					}
					return scrollBarWidth;
				}
			},
			parser: new function() {
				var utilities = {
					//private function to convert the string .NET spits out to a proper javascript date
					dotNetDateToDate: function (dotNetDateString) {
						return new Date(parseInt(dotNetDateString.replace(/[^0-9]/ig, '')));
					}
				}
				var api = {
					//convert non-date objects to javascript dates
					toDate: function (date) {
						if (date === '') {
							date = null
						}
						if (date && !(date instanceof Date)) {
							if (date.match(/\/Date\([0-9]+\)\//)) {
								date = utilities.dotNetDateToDate(date);
							} else if (/^[0-9]{4}\-[0-9]{1,2}\-[0-9]{1,2}/.test(date)) {
								//xml date format
								var bits = date.split(/[-T:]/g);
								date = new Date(bits[0], bits[1] - 1, bits[2]);
								if (bits.length > 5) {
									date.setHours(bits[3], bits[4], bits[5]);
								}
							} else {
								date = new Date(date);
								if (!(date instanceof Date)) {
									date = null
								}
							}
						}
						return date;
					},
					toBoolean: function (value) {
						return (value != null && (value === true || value.toString().toLowerCase() == 'true' || value.toString().toLowerCase() == '1'));
					}
				}
				return api
			},
			navigationConfirmation: function (dirtyCheck, event) {
				//first parameter should resolve to whether or not the page is dirty, the second should have the navigation event if it exists
				//if the function is being called from window.beforeunload, don't pass an event in and the function will return the message
				var message = "You have unsaved changes. Are you sure you would like to discard these changes and leave this page?"
				if (dirtyCheck) {
					if (event) {
						if (!confirm(message)) {
							event.preventDefault();
						}
					} else {
						return dirtyCheck ? message : undefined;
					}
				}

			}
		})	
})
