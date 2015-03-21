/*
  acFreezeHeader is based on the open source (MIT License) freezeheader jquery plugin 
  by Brent Muir: http://brentmuir.com/projects/freezeheader
  The code has been modified to support ACAS's use cases and not much more.

*/

/*
 * Limitations: one thead per table
 * 
 */
acas.module('acFreezeHeader', 'acas.ui.angular', 'jquery', function () {
	acas.ui.angular.directive('acFreezeHeader', ['$compile', function ($compile) {
		var freezer = (function ($) {
			var utilities = {
				init: function (element, scope) {
					return element.each(function () {
						var $this = $(element)

						// If the plugin hasn't been initialized yet
						if (!$this.data('freezeHeader')) {
							// add divs within TH and TD elements to force width. 
							$this.find('th, td').wrapInner('<div>')
							utilities.updateHeaders($this, scope)
						}
						$(window).bind('resize.freezeHeader_' + scope.$id, { table: $this }, utilities.resize)
						$(window).bind('scroll.freezeHeader_' + scope.$id, { table: $this }, utilities.scroll)
						// force a scroll and resize to initialize correctly
						$(window).trigger('scroll')
						$(window).trigger('resize')
					})
				},

				destroy: function (element, scope) {
					return element.each(function () {

						$(window).unbind('resize.freezeHeader_' + scope.$id)
						$(window).unbind('scroll.freezeHeader_' + scope.$id)

						var data = $(this).data().freezeHeader
						if (data) {
							data.top.remove()
						}

						$(this).removeData('freezeHeader')
					})
				},

				resize: function (event) {
					var table = event.data.table
					var topHeader = table.data('freezeHeader').top;
					//set the width of the header th elements to the same as the data table th elements
					topHeader.find('th>div').each(function (i) {
						if (typeof window.getComputedStyle == 'function') {
							$(this).width(window.getComputedStyle(table.find('th>div').eq(i).get(0), '').getPropertyValue('width'))
						} else {
							$(this).width(table.find('th>div').eq(i).width())
						}
					})
				},

				scroll: function (event) {
					var table = event.data.table
					var scrollTop = $(window).scrollTop()
					var topHeader = table.data('freezeHeader').top

					if (table.children('thead').offset().top < scrollTop) {
						var tableOffset = table.offset()
						var tableBottom = tableOffset.top + table.height() - topHeader.height()
						if (topHeader.css('visibility') !== 'visible') {
							$(window).trigger('resize') //when the topHeader first becomes visible, force a resize
						}
						topHeader.css({
							visibility: 'visible',
							position: 'fixed',
							top: Math.min(Math.max(0, tableOffset.top - scrollTop), tableBottom - scrollTop),
							left: tableOffset.left,
							right: $((window.navigator.userAgent.indexOf("MSIE ") > 0) ? window : document).width() - tableOffset.left - table.width()
						})

					}
					else {
						topHeader.css({ visibility: 'hidden' })
					}

				},

				updateHeaders: function (element, scope) {
					if ($(element).data('freezeHeader') && $(element).data('freezeHeader').top) {
						$(element).data('freezeHeader').top.remove()
					}

					// To create a frozen top header, we clone the entire table and remove the TBODY
					// Need to wrap the table in a div because dynamically setting position:fixed on a table
					// doesn't work in IE8, but setting it on the div does.
					// Set initial div positioning to overlap existing table to work around IE8 bug (otherwise
					// document height will include the cloned tables even though they are moved later)		
					var top = element.clone(false).children('tbody').remove().end().appendTo(document.body)
									.wrap('<div>').parent()
					//.css({ position: 'absolute', top: $(element).offset().top, left: $(element).offset().left })

					//$compile(top.find('thead'))(scope)
					$(element).data('freezeHeader', { top: top, naturalHeader: $(element).children('thead') })
					$(window).trigger('scroll')
					$(window).trigger('resize')
				}
			}

			var api = {
				freeze: function (element, scope) {
					return utilities.init.apply(this, arguments)
				},
				updateHeaders: utilities.updateHeaders,
				destroy: utilities.destroy
			}

			return api
		})(jQuery)

		return {
			restrict: 'A',
			link: function (scope, element) {
				freezer.freeze(element, scope)

				//all these watches make things very slow. Currently, webport has headers that 
				//that are dynamic but only update once. They're all covered by a single watch that is disabled after the first time
				//the values change

				//when the visibility of the header changes, or anything else with in it changes, update it
				//scope.$watch(function () {
				//	return element.data('freezeHeader').top[0].innerHTML
				//}, function (newValue, oldValue) {
				//	freezer.updateHeaders(element, scope)
				//})

				//if the content of the original header changes, we need to update the fixed header too
				//look for a faster way to do this?
				scope.$watch(function () {
					var naturalHeader = element.data('freezeHeader').naturalHeader
					if (!naturalHeader || !naturalHeader[0]) return;
					return naturalHeader[0].innerHTML
				}, function () {
					freezer.updateHeaders(element, scope)
				})

				//it's possible that the header doesn't even exist until data is loaded, if the headers are determined dynamically. 
				//if the element has theads with changing content, update the element
				var disableWatch = scope.$watch(function () { return element.clone(false).children('tbody').remove().end()[0].innerHTML },
					function (newValue, oldValue) {
						if (newValue !== oldValue) {
							freezer.updateHeaders(element, scope)
							disableWatch()
						}
					})

				scope.$on('$destroy', function () {
					freezer.destroy(element, scope)
				})
			}
		}
	}])
})

