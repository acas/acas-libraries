/*
These are config settings used in acas libraries and intended to be overridden in consuming applications.
Some come with defaults, others must be implemented in the consuming app
*/
acas.config = {
	reporting : { reportDownloadUrl: ''},

	numericDisplayDefaults: {
		minPrecision: 2,
		maxPrecision: 10,
		thousandsSeparator: true,
		negativeParenthesis: true
	},

	notifications: {
		logClientNotificationEventUrl: '',
		reportToAddress: ''
	}
}