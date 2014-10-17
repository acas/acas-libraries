acas.module('acas.notifications.angular.controller', 'acas.notifications.angular', 'jquery', 'underscorejs', function () {
	/*
	This config must be setup in the project for all the pieces of the notifications module to work properly.
	*/
	
	acas.notifications.angular.controller('acas-notifications-angular-controller', ['$scope', '$timeout', '$modal', function ($scope, $timeout, $modal) {
		$scope.acas.notifications = new function () {
			function notificationEventHandler(e) {
				$timeout(function () {
					if (e.eventCategory == 'error') {
						$scope.acas.notifications.hasErrors = true;
					}
					$scope.acas.notifications.history.push(e);
					if (api.maxRecentNotifications < $scope.acas.notifications.recent.unshift(e)) {
						$scope.acas.notifications.recent.splice(-1, 1);
					}

					//show pop-up notification
					var notyType;
					switch (e.eventCategory) {
						case 'error':
						case 'warning':
						case 'success':
						case 'information':
							notyType = e.eventCategory;
							break;
						default:
							notyType = 'information';
					};

					var notyText = '<span class="glyphicon ' + api.getNotificationEventIcon(e) + '"></span> ' + api.getNotificationEventTitle(e);
					if (e.pageId) {
						notyText += '<div class="wp-header-notification-pageName">' + e.pageName + '</div>';
					}

					noty({
						type: notyType,
						text: '<div>' + notyText + '</div>',
						callback: {
							onCloseClick: function () {
								api.showNotificationDetails(e);
							}
						}
					});
				});
			}

			var api = {
				getNotificationEventTitle: function (e) {
					switch (e.eventType) {
						case 'error-http':
							return 'Server Error' + (e.data && e.data.status ? ' (' + e.data.status + ')' : '');
						case 'error':
							return 'Application Error';
						default:
							return e.message;
					}
				},
				getNotificationEventIcon: function (e) {
					switch (e.eventCategory) {
						case 'error':
							return 'glyphicon-warning-sign';
						case 'warning':
							return 'glyphicon-exclamation-sign';
						case 'success':
							return 'glyphicon-ok';
						case 'information':
							return 'glyphicon-info-sign';
						default:
							return 'glyphicon-info-sign';
					}
				},
				showNotificationHistory: function (e) {
					//reset visibility
					if (!$scope.acas.notifications.visible) {
						closeAllHeaderDropDownMenus();
					}
					$scope.acas.notifications.visible = !$scope.acas.notifications.visible;
					//prevent event from propagating
					e.preventDefault();
					e.stopPropagation();
				},
				showNotificationDetails: function (e) {

					var modal = $modal.open({
						templateUrl: acas.cdnBaseUrl + '/notifications/NotificationDetail.html',
						controller: 'acas-notifications-angular-modal-controller',
						resolve: {
							items: function () {
								return {
									event: e,
									historyIndex: e.index,
									history: api.history,
									getNotificationEventIcon: api.getNotificationEventIcon,
									getNotificationEventTitle: api.getNotificationEventTitle
								};
							}
						}
					});

				},
				visible: false,
				hasErrors: false,
				history: [],
				recent: [],
				maxRecentNotifications: 5

			}


			//attach listener to receive all notification events
			acas.notifications.onNotification.bind(notificationEventHandler);
			$scope.$on('$destory', function () {
				acas.notifications.onNotification.unbind(notificationEventHandler);
			})

			return api;
		}
	}])

	acas.notifications.angular.controller('acas-notifications-angular-modal-controller', ['$scope', '$modalInstance', '$window', 'items', function ($scope, $modalInstance, $window, items) {
		$scope.isCollapsed = true;
		$scope.historyIndex = items.historyIndex;
		$scope.history = items.history;
		$scope.closeModal = function () {
			$modalInstance.close();
		}
		$scope.getNotificationEventIcon = items.getNotificationEventIcon;
		$scope.getNotificationEventTitle = items.getNotificationEventTitle;

		$scope.previousNotification = function () {
			$scope.historyIndex--;
		}
		$scope.nextNotification = function () {
			$scope.historyIndex++;
		}

		$scope.reportErrorEvent = function (event) {
			var e = _.clone(event, true);
			if (e && e.data && e.data.data) {
				//remove HTML from JSON object
				delete e.data.data;
			}
			var body = [];
			for (var key in e) {
				if (e.hasOwnProperty(key) && !(e[key] instanceof Function)) {
					body.push(key + ': ');
					if (e[key]) {
						if (e[key] instanceof Array || e[key] instanceof Object) {
							body.push(JSON.stringify(e[key], undefined, '\t'));
						} else if (e[key] instanceof Date) {
							body.push(acas.utility.formatting.formatDateTime(e[key]));
						} else {
							body.push(e[key]);
						}
					}
					body.push('\n');
				}
			}
			
			if (!acas.config.notifications.reportToAddress) {
				if (console && console.error) {
					console.error('Cannot report notification event: email address not configured.')
				}
			}
			else {
				var link = 'mailto:' + encodeURIComponent(acas.config.notifications.reportToAddress) + '?subject=Application%20Error%20-%20' + encodeURIComponent(items.getNotificationEventTitle(e)) + '&body=' + encodeURIComponent('The following error occurred:\n\n' + body.join(''));
				if (link.length >= 2048) {
					//restrict link to conform to GET request standards
					link = link.substring(0, 2047);
				}
				if (acas.utility.isIE9()) {
					var mailtoIframe = jQuery('<iframe src="' + link + '"/>');
					jQuery('body').append(mailtoIframe);
					mailtoIframe.remove();
				}
				else {
					$window.location = link;
				}
			}			
		}

		$scope.$watch('historyIndex', function () {
			jQuery('#notificationJsonRaw').css('display', 'none');
			jQuery('#notificationJsonHtml').css('display', 'none');
			jQuery('#notificationServerHtml').css('display', 'none');
			try {
				jQuery('#notificationJsonRaw').text(JSON.stringify($scope.history[$scope.historyIndex], undefined, '\t'));
				if ($scope.history[$scope.historyIndex].data != null && $scope.history[$scope.historyIndex].data.data != null && $scope.history[$scope.historyIndex].data.data.length > 0) {
					jQuery('#notificationServerHtml')[0].contentWindow.document.body.innerHTML = $scope.history[$scope.historyIndex].data.data;
					jQuery('#notificationServerHtml').css('display', '');
				} else {
					jQuery('#notificationJsonHtml').jsonFormat('#notificationJsonRaw');
					jQuery('#notificationJsonHtml').css('display', '');
				}
			} catch (ex) {
				jQuery('#notificationJsonRaw').css('display', '');
			}
		});
	}])
})
