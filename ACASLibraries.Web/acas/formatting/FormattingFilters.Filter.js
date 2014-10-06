acas.module('formattingFilters', 'acas.formatting.angular', 'acas.utility', function () {

	acas.formatting.angular
		//acFormatMoney uses default settings for format number except the precision is {2, 2}
		.filter('acFormatMoney', function () {
			return function (value) {
				return acas.utility.formatting.formatNumber(value, 2, 2)
			}
		})
		//acFormatInteger uses default settings for format number except the precision is {0, 0}
		.filter('acFormatInteger', function () {
			return function (value) {
				return acas.utility.formatting.formatNumber(value, 0, 0)
			}
		})
		//acFormatNumber is a pass through to the formatNumber function, with defaults, except it allows 
		//overriding the minPrecision and maxPrecision
		.filter('acFormatNumber', function () {
			return function (value, args) {
				args = args || {}
				var minPrecision = args.minPrecision !== undefined ? args.minPrecision : acas.config.numericDisplayDefaults.minPrecision
				var maxPrecision = args.maxPrecision !== undefined ? args.maxPrecision : acas.config.numericDisplayDefaults.maxPrecision
				return acas.utility.formatting.formatNumber(value, minPrecision, maxPrecision)

			}
		})
		//acFormatPercent is a pass through to formatNumber with percent = true, all other parameters use defaults
		//it allows overriding minPrecision and maxPrecision, otherwise it's the standard defaults
		.filter('acFormatPercent', function () {
			return function (value, args) {
				args = args || {}
				return acas.utility.formatting.formatNumber(value,
					(args.minPrecision !== undefined ? args.minPrecision : acas.config.numericDisplayDefaults.minPrecision),
					(args.maxPrecision !== undefined ? args.maxPrecision : acas.config.numericDisplayDefaults.maxPrecision),
					acas.config.numericDisplayDefaults.thousandsSeparator,
					acas.config.numericDisplayDefaults.negativeParenthesis,
					true)
			}
		})		
		.filter('acFormatYesNo', function () {
			// converts to 'Yes' or 'No', returns empty string if null, undefined, empty string
			// we could try to tighten the performance of this in typical use case, perhaps. and simplify the code
			// this isn't a utility function pass-through because it's not the kind of thing we should be doing except with angular filters
			return function (value) {
				//null/undefined and empty strings 
				if (value === null || value === undefined || (typeof (value) === 'string' && value.trim() === '')) {
					return ''
				}
				//interpret '0' and the like as numbers
				var parsed = parseFloat(value)
				if (!isNaN(parsed)) {
					value = parsed
				}
				return (value ? 'Yes' : 'No')
			}
		})
		//format a date. If it's a .NET date or not a date, convert it to a JS date first.
		.filter('acFormatDate', function () {
			return function (value) {
				if (value != null) {
					return acas.utility.formatting.formatDate(value);
				} else {
					return "";
				}
			}
		})
		.filter('acFormatTime', function () {
			return function (value) {
				if (value != null) {
					return acas.utility.formatting.formatTime(value);
				} else {
					return "";
				}
			}
		})
		.filter('acFormatDateTime', function () {
			return function (value) {
				if (value != null) {
					return acas.utility.formatting.formatDateTime(value);
				} else {
					return "";
				}
			}
		})
		.filter('acFormatUsername', function () {
			return function (value) {
				if (value != null) {
					return acas.utility.formatting.formatUsername(value);
				} else {
					return "";
				}
			}
		});
})
