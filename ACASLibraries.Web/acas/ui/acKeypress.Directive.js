
acas.module('acKeypress', 'acas.ui.angular', function () {
	/*
	Usage: add ac-keypress as an attribute to an existing, clickable element. 
	The only argument is the keypress to monitor, see below. Note that if the same keypress is added to multiple elements, all bets are off.
	If an element is not visible (based on offsetParent), the click event on that element will not be fired on keypress. This protects hidden elements
	from being clicked when they're not supposed to be clickable.

	Currently supports the following keybindings, not all browsers work for all keypresses:
		- ctrl-s
		- ctrl-shift-s
		- ctrl-e
		- ctrl-n
	It would be nice to add a tooltip to the element that showed the keyboard shortcut, but it has to work in all browsers first.
	Not sure how this will work on fixed elements.
	*/
	acas.ui.angular.directive('acKeypress', ['$document', '$timeout', function ($document, $timeout) {

		return {
			restrict: 'A',
			link: function (scope, element, attributes) {
				if (document.all && !window.atob) { //IE9
					return //we don't support keyboard shortcuts in IE9 because the offsetParent thing doesn't work, and this is just a bonus feature anyway.
				}
				var keyPressed = function () { return false }
				switch (attributes.acKeypress.toLowerCase()) {
					case 'ctrl-s':
						keyPressed = function (event) { return event.which === 83 && event.ctrlKey }
						break
					case 'ctrl-shift-s':
						keyPressed = function (event) { return event.which === 83 && event.ctrlKey && event.shiftKey}
						break
					case 'ctrl-e':
						keyPressed = function (event) { return event.which === 69 && event.ctrlKey }
						break
					case 'ctrl-n': //ctrl-n doesn't work in chrome
						keyPressed = function (event) { return event.which === 78 && event.ctrlKey }
						break
				}
				$document.on('keydown', function (event) {
					//we need to check the offsetParent to verify that the element is currently visible. If it's not, it shouldn't fire (user may be in readonly mode, etc)
					if (keyPressed(event) && element[0].offsetParent !== null) {
						event.preventDefault()
						if ($document[0].activeElement.nodeName === 'INPUT') {
							$timeout(function () {
								$document[0].activeElement.blur()
								$timeout(function () {
									element.click()
								})
							})

						}
						else {
							element.click()
						}

					}
				})
			}
		}

	}])
});
