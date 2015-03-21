acas.module('acas.notifications', 'jquery', 'jquery.noty', 'underscorejs', function () {
	_.extend(acas.notifications,
		new function () {

			//configure noty notifications
			jQuery.noty.defaults.layout = 'topRight';
			jQuery.noty.defaults.timeout = 3 * 1000; //5 seconds

			//declare notification event
			var notificationEvent = new acas.utility.event('acas-notification-event');
			var notificationEventCount = 0;

			var detectDuplicateHttpErrors = true;
			var lastHttpError = null;

			//returns an event object for the given eventType and data
			function getEventData(eventType, data) {
				var e = {
					index: notificationEventCount++,
					eventCategory: eventType.split('-')[0],
					eventType: eventType,
					data: data,
					message: getEventMessage(eventType, data)
				}

				if (arguments.length > 2) {
					e.other = [];
					for (var x = 2; x < arguments.length; x++) {
						e.other.push(arguments[x]);
					}
				}

				return e;
			}

			//sends event data to server to be logged
			function sendNotificationToServer(e) {
				if (acas.config.notifications.logClientNotificationEventUrl) {

					var baseUrl = jQuery('base').prop('href');
					jQuery.ajax({
						url: (baseUrl != null && baseUrl.length > 0 ? baseUrl : '/') + acas.config.notifications.logClientNotificationEventUrl,
						type: 'POST',
						data: {
							notificationEvent: JSON.stringify(e)
						},
						success: function () {
							if (typeof (console) == 'object') console.log('Notification logged to server');
						},
						error: function (jqXHR, textStatus, errorThrown) {
							if (typeof (console) == 'object') console.error('Failed sending notification to server: ' + (errorThrown != null ? errorThrown : 'no error thrown'));
						}
					});
				}
				else {
					if (typeof (console) == 'object') console.warn('Server-side logging for notifications is not set up.');
				}
			}

			//processes incoming data and builds message dynamically for certain event types
			function getEventMessage(eventType, data) {
				if (data != null) {
					switch (eventType) {
						case 'error-exception':
							if (data.message != null) {
								return data.message;
							} else {
								return data.toString();
							}
							break;
						case 'error-http':
							if (data.status != null && data.config.method != null && data.config.url != null) {
								return data.status.toString() + ' exception on ' + data.config.method + ' from ' + data.config.url;
							}
							else {
								return data.toString();
							}
							break;
						default:
							if (data.message != null) {
								return data.message;
							} else {
								return data.toString();
							}
					}
				} else {
					return (eventType != null ? eventType + ' event' : 'Unspecified event type') + ' missing event data';
				}
			}

			//returns an array of arguments except the first <shiftAmount> items
			function shiftArguments(args, shiftAmount) {
				var output = [];
				if (args.length > shiftAmount) {
					for (var x = shiftAmount + 1; x < args.length; x++) {
						output.push(args[x]);
					}
				}
				return output;
			}

			function isDuplicateHttpError(e1, e2) {
				return e1 && e1.data && e1.data.config
				 && e2 && e2.data && e2.data.config
				 && e1.data.status == e2.data.status
				 && e1.data.config.method == e2.data.config.method
				 && e1.data.config.url == e2.data.config.url;
			}

			//generates the event object and fires the event; also determines whether event data should be sent to server to be logged
			function fireEvent(type, data, args) {
				var newArgs = [type, data];
				var remainingArgs = shiftArguments(args, 1);
				var e;
				for (var x = 0; x < remainingArgs.length; x++) {
					newArgs.push(remainingArgs[x]);
				}
				try {
					e = getEventData.apply(this, newArgs);
					if (e.eventType == 'error-http') {
						if (detectDuplicateHttpErrors && lastHttpError && isDuplicateHttpError(lastHttpError.error, e) && (new Date() - lastHttpError.timestamp) < 1000) {
							//this error is a duplicate of the last error, ignore it
							return;
						}
						lastHttpError = { timestamp: new Date(), error: e };
					}
					if (e.eventCategory == 'error' && e.eventType != 'error-http') {
						sendNotificationToServer(e);
					}
					notificationEvent.trigger(e);
				} catch (ex) {
					if (typeof (console) == 'object') console.error(ex);
				}
				return e;
			}

			//creates notifications for unhandled exceptions (this function should be registered elsewhere via document.onerror)
			function unhandledExceptionHandler(message, url, lineNumber) {
				var data = {
					message: message,
					url: url,
					lineNumber: lineNumber
				};
				if (typeof (console) == 'object') console.error(data);
				fireEvent('error-unhandled-exception', data, arguments);
			};

			//register handled to receive unhandled exceptions
			window.onerror = unhandledExceptionHandler;



			//publically accessible api
			//Notes about event notification functions:
			//- All functions (error, exception, success, etc) return the event object and log the arguments to the console
			//- Events created through the error handling event functions (error, exception, etc) are given an event category = 'error' and are sent to the server for logging
			var api = {

				//notification event; provides external binding capabilities
				onNotification: notificationEvent,

				//creates notifications for general error messages
				error: function (message) {
					if (typeof (console) == 'object') console.error(arguments);
					return fireEvent('error-message', message, arguments);
				},

				//creates notifications for exception objects
				exception: function (ex) {
					if (typeof (console) == 'object') console.error(arguments);
					return fireEvent('error-exception', ex, arguments);
				},

				//creates notifications for angular ajax errors
				httpError: function (rejection) {
					if (typeof (console) == 'object') console.error(arguments);
					return fireEvent('error-http', rejection, arguments);
				},

				//creates notifications for warning messages
				warning: function (message) {
					if (typeof (console) == 'object') console.warn(arguments);
					return fireEvent('warning-message', message, arguments);
				},

				//creates notifications for success messages
				success: function (message) {
					if (typeof (console) == 'object') console.info(arguments);
					return fireEvent('success-message', message, arguments);
				},

				//creates notifications for information messages
				info: function (message) {
					if (typeof (console) == 'object') console.info(arguments);
					return fireEvent('information-message', message, arguments);
				}
			};

			return api;
		}
	)
})
