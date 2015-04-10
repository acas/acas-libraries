'use strict';

acas.module('acas.utility', 'underscorejs', function () {

	var utilityApi = {
		isIE9: function () {
			return document.all && !window.atob;
		},
		event: function (eventType) {
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
		date: {
			addDays: function (date, days) {
				return new Date(acas.utility.parser.toDate(date).getTime() + (86400 * 1000 * days))
			}
		},
		data: {
			// Gets the value of referential data and translates it to a specified display value
			// id: The lookup id
			// dataModel: The data model in which the lookup will be performed. Must be an array of object
			// idColumnName: The column in the referential data table that the ID parameter will
			//               refer to during the lookup
			// displayColumnName: The display value of the record pertaining to the id value in
			//                    an ID column
			getReferentialDataValue: function (id, dataModel, idColumnName, displayColumnName) {
				var value = _.find(dataModel, function (item) {
					return id == item[idColumnName];
				});
				if (value === undefined || value === null) return ''
				else return value[displayColumnName]
			}
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
			formatAlphanumericDate: function (date) {
				var getMonthAbbreviation = function (monthNumber) {
					switch (monthNumber) {
						case 1: return "Jan"
						case 2: return "Feb"
						case 3: return "Mar"
						case 4: return "Apr"
						case 5: return "May"
						case 6: return "Jun"
						case 7: return "Jul"
						case 8: return "Aug"
						case 9: return "Sep"
						case 10: return "Oct"
						case 11: return "Nov"
						case 12: return "Dec"
						default: return "Unk"
					}
					return "Unk"
				}
				var dateBuilder = function (dateString) {
					if (dateString !== undefined
						&& dateString != null
						&& dateString != '') {
						var dateParts = acas.utility.formatting.formatDate(dateString).split("/")
						if (dateParts.length === 3) {
							var month = getMonthAbbreviation(parseInt(dateParts[0]))
							return month + " " + dateParts[1] + ", " + dateParts[2]
						}
					}
					return null
				}
				return dateBuilder(date)
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
				var usernameParts = username.split('.');
				for (var x = 0; x < usernameParts.length; x++) {
					if (usernameParts[x].length > 1) {
						usernameParts[x] = usernameParts[x].substr(0, 1).toUpperCase() + usernameParts[x].substr(1);
					}
					else {
						usernameParts[x] = usernameParts[x].toUpperCase();
					}
				}
				//username = username.replace('.', ' ');
				return usernameParts.join(' ');

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
				if (negative) {
					value = value * -1
				}
				var rounded = +(Math.round(value + ("e+" + maxPrecision)) + ("e-" + maxPrecision))
				var stringValue = rounded.toString()
				var decimalIndex = stringValue.indexOf('.')
				var decimal = decimalIndex === -1 ? '' : stringValue.substr(decimalIndex + 1)
				var integer = decimalIndex === -1 ? stringValue : stringValue.substr(0, decimalIndex) || "0"
				var zeroes = "0000000000000000000000000000000000000000000000000000000000000000000000"

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
				if (dateValue == null) return false
				var date = new Date(dateValue);
				return (date instanceof Date && !isNaN(date.valueOf()))
			},

			//determines whether a single character is valid inside of a number. essentially just digits, decimal (.), and minus sign (-)
			isValidNumericCharacter: function (character) {
				if (character == null) return false
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
				return !event.ctrlKey && !_.contains([0, 8, 9
					, 13 //enter key
					, 37, 38, 39, 40	//arrow keys					
				], event.which) //TODO add to this list			
			},

			//determines whether a value is a valid number or not. TODO - not tested very well
			isValidNumber: function (value) {
				if (value == null) return false
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
				for (var i in input) {
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
		text: {
			setCharAt: function (string, index, char) {
				if (index > string.length - 1) return str;
				return string.substr(0, index) + char + string.substr(index + 1);
			}
		},
		parser: new function () {
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
				},
				toAngularJSQueryString: function (value, includeNullParameters) {
					includeNullParameters = typeof includeNullParameters !== undefined ? includeNullParameters : false
					var buffer = []
					_.each(value, function (value, key) {
						_.each(acas.utility.array.forceArray(value), function (v) {
							if (includeNullParameters || v) {
								buffer.push(encodeURIComponent(key) + '=' + (v ? v.toString() : ''))
							}
						})
					})
					return buffer.join('&')
				}
			}
			return api
		},
		periods: {
			//private function to convert monthId to the quarterId it occurs in
			monthIdToQuarterId: function (monthId) {
				return monthId.toString().slice(0, 4) * 10 + (Math.floor((monthId.toString().slice(4) - .1) / 3) + 1)
			},

			addMonths: function (monthId, increment) {
				if (monthId && increment) {
					if (monthId.toString().length !== 6) {
						return;
					} else {
						var year = Math.floor(monthId / 100);
						var month = monthId % 100;
						month += increment;
						while (month < 1 || month > 12) {
							if (month > 12) {
								month = month - 12;
								year += 1;
							}
							else if (month < 1) {
								month = month + 12;
								year -= 1;
							}
						}
						return year * 100 + month;
					}
				} else {
					return monthId;
				}
			},

			addQuarters: function (quarterId, increment) {
				if (quarterId && increment) {
					var correspondingMonth = quarterId.toString().slice(0, 4) * 100 + quarterId.toString().slice(4) * 3
					var newMonth = this.addMonths(correspondingMonth, increment * 3)
					return this.monthIdToQuarterId(newMonth)
				} else {
					return quarterId;
				}
			},

			displayMonth: function (monthId) {
				if (!monthId || monthId.toString().length !== 6) {
					return;
				} else {
					var year = Math.floor(monthId / 100);
					var month = monthId % 100;
					return year + "-" + formatting.padZero(month, 2);
				}
			},

			displayQuarter: function (quarterId) {
				if (!quarterId || quarterId.toString().length !== 5) {
					return;
				} else {
					return 'Q' + quarterId.toString().substr(4) + ' ' + quarterId.toString().slice(0, 4)
				}
			},

			currentMonth: function () {
				var today = new Date();
				return today.getFullYear() * 100 + today.getMonth() + 1; //getMonth is zero indexed
			},

			currentQuarter: function () {
				return this.monthIdToQuarterId(this.currentMonth())
			}
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
	}

	_.extend(acas.utility, utilityApi)
})
