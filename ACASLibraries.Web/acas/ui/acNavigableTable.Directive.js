
/********
Copied from https://github.com/aarongreenwald/angularjs-components
See the readme there for documentation.
TODO: copy that documentation here, in case that code disappears.
****************/

acas.module('acNavigableTable', 'acas.ui.angular', function () {
	acas.ui.angular.directive('acNavigableTable', function () {
		return {
			restrict: 'A',
			scope: {
				acNavigableTable: '@'
			},
			link: function (scope, el) {
				var firstVisibleInput = function (node, tdIndex) {
					if (tdIndex !== undefined) {
						node = node.children[tdIndex] //todo what if there are varying colspans?				  
					}
					if (node) {
						var descendants = node.getElementsByTagName('*')
						for (var i = 0; i < descendants.length; i++) {
							if (descendants[i].nodeName === 'INPUT'
								  && descendants[i].offsetHeight > 0 && descendants[i].offsetWidth > 0 //the input is actually visible
							) {
								return descendants[i]
							}
						}
					}
					return undefined
				}

				var disableLeftRight = false

				if (scope.acNavigableTable === 'excel') {
					el.bind('dblclick', function () {
						disableLeftRight = true
						document.activeElement.onblur = function () {
							disableLeftRight = false
						}
					})
				}


				el.bind('keydown', function (event) {
					var keys = { left: 37, up: 38, right: 39, down: 40 }
					var key = event.which

					if (scope.acNavigableTable === 'excel') {
						if (key === 13) {
							key = 40 //in excel mode, treat the enter key like the down arrow
						}
					} else {
						if (key === 13) {
							disableLeftRight = !disableLeftRight //in standard mode, the enter key disables the left/right arrows
							document.activeElement.onblur = function () {
								disableLeftRight = false
							}
						}
					}

					//start at the currently focused element, must be an input for this to continue
					var startInput = document.activeElement
					if (startInput.nodeName !== 'INPUT') return;


					var destinationInput
					var node = startInput

					//walk along the DOM, looking for the destination input 

					//look for the startInput's TD
					do {
						node = node.parentNode
					} while (node && node.nodeName !== 'TD')
					if (!node) return //ill-formed html

					if (!disableLeftRight && (key === keys.left || key === keys.right)) {
						event.preventDefault()
						//walk along the TDs until we find one that has a visible input within it				  
						do {
							node = (key === keys.left ? node.previousElementSibling : node.nextElementSibling)
							if (node && node.nodeName === 'TD') {
								destinationInput = firstVisibleInput(node)
							}
							else return //no more TDs available - we're done here
						} while (!destinationInput)
					}
					else if (key === keys.up || key === keys.down) {
						event.preventDefault()
						var tdIndex = node.cellIndex;

						do { //find the TR
							node = node.parentNode
						} while (node && node.nodeName !== 'TR')
						if (!node) return //ill-formed html

						do {
							node = (key === keys.up ? node.previousElementSibling : node.nextElementSibling)
							if (node && node.nodeName === 'TR') {
								destinationInput = firstVisibleInput(node, tdIndex)
							}
							else return //no more rows or ill-formed html
						} while (!destinationInput)

					}

					if (destinationInput) {
						destinationInput.focus()
					}


				})
			}
		}
	})
})
;