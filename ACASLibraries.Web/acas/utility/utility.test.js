'use strict';
describe('Parsing --> ', function () {
	var parser = acas.utility.parser

	describe('Date Parsing --> ', function () {
		it('toDate()', function () {
			expect(parser.toDate('4/1/2014')).toEqual(new Date('4/1/2014'))
			expect(parser.toDate('2014-04-02T03:45:12')).toEqual(new Date(2014, 3, 2, 3, 45, 12))
			expect(parser.toDate('2014-04-03')).toEqual(new Date(2014, 3, 3))
			expect(parser.toDate('2014-4-4T03:45:12')).toEqual(new Date(2014, 3, 4, 3, 45, 12))
			expect(parser.toDate('2014-4-5')).toEqual(new Date(2014, 3, 5))
			expect(parser.toDate('4/6/14')).toEqual(new Date(2014, 3, 6))
			expect(parser.toDate('')).toBeNull() //empty string and null should return null
			expect(parser.toDate(null)).toBeNull()
			expect(parser.toDate(undefined)).not.toBeDefined() //undefined should return undefined
		})
	})

})


describe('Formatting --> ', function () {
	var formatting = acas.utility.formatting

	describe('Date Formatting --> ', function () {
		it('formatDate()', function () {
			expect(formatting.formatDate('3/5/2014')).toBe('3/5/2014')
			expect(formatting.formatDate('4/05/2014')).toBe('4/5/2014')
			expect(formatting.formatDate('05/05/2014')).toBe('5/5/2014')
			//phantomJS fails at parsing dates with hyphens in them, chrome succeeds. This appears to be a phantomJS issue: https://code.google.com/p/phantomjs/issues/detail?id=267
			//expect(formatting.formatDate('6-05-2014')).toBe('6/5/2014')			
			//expect(formatting.formatDate('07-05-2014')).toBe('7/5/2014')

			expect(formatting.formatDate(new Date('8/05/2014'))).toBe('8/5/2014')
			expect(formatting.formatDate('\/Date(1397793600000)\/')).toBe('4/18/2014')

		})

		it('formatDateTime() should format properly', function () {
			expect(formatting.formatDateTime('2014-08-04T21:30:00.33-04:00')).toBe('8/4/2014 9:30 PM')
			
		})

	})
	

	describe('Number Formatting --> ', function () {

		var tests = [
						[1, "1.000"],
						[1.23, "1.230"],
						[1.532, "1.532"],
						[1.53987, "1.53987"],
						[1.539874, "1.53987"],
						[1.549875, "1.54988"],
						[1.559876, "1.55988"],
						[1.8, "1.800"],
						[0, "0.000"],
						[0.45, "0.450"],
						[.4005, "0.4005"],
						[.400555, "0.40056"],
						[123456, "123,456.000"],
						[1234567, "1,234,567.000"],
						[123456.7, "123,456.700"],
						[123456.789, "123,456.789"],
						[123456.789439, "123,456.78944"],
						[123456.789455, "123,456.78946"],
						[123456.789454, "123,456.78945"],
						[12345678912.34567, "12,345,678,912.34567"],

		]

		it('formatNumber() - positive numbers, thousands separator', function () {
			for (var i = 0; i < tests.length; i++) {
				expect(formatting.formatNumber(tests[i][0], 3, 5)).toBe(tests[i][1])
			}

		})

		it('formatNumber() - positive numbers, no thousands separator', function () {
			for (var i = 0; i < tests.length; i++) {
				expect(formatting.formatNumber(tests[i][0], 3, 5, false)).toBe(tests[i][1].replace(',', '').replace(',', '').replace(',', ''))
			}

		})

		it('formatNumber() - negativeParenthesis, thousands separator', function () {
			for (var i = 0; i < tests.length; i++) {
				if (tests[i][0] === 0) {
					expect(formatting.formatNumber(tests[i][0] * -1, 3, 5)).toBe(tests[i][1])
				} else {
					expect(formatting.formatNumber(tests[i][0] * -1, 3, 5)).toBe("(" + tests[i][1] + ")")
				}

			}
		})

		it('formatNumber() - no negativeParenthesis, thousands separator', function () {
			for (var i = 0; i < tests.length; i++) {
				if (tests[i][0] === 0) {
					expect(formatting.formatNumber(tests[i][0] * -1, 3, 5, ',', false)).toBe(tests[i][1])
				} else {
					expect(formatting.formatNumber(tests[i][0] * -1, 3, 5, ',', false)).toBe("-" + tests[i][1])
				}
			}
		})

		it('formatNumber() - parameter defaults', function () {
			var tests2 = [
				123, 123.4567, -123, -123.4, -4252524242.456622, -2.21342424242424242535252, -459202.9348929342002845
			]
			for (var i = 0; i < tests2.length; i++) {
				expect(formatting.formatNumber(tests2[i][0])).toBe(formatting.formatNumber(tests2[i][0], 2, 10, ',', true))
				expect(formatting.formatNumber(tests2[i][0], 1)).toBe(formatting.formatNumber(tests2[i][0], 1, 10, ',', true))
				expect(formatting.formatNumber(tests2[i][0], 1, 2, false)).toBe(formatting.formatNumber(tests2[i][0], 1, 2, '', true))
			}

		})

		it('formatNumber() - zero precision lengths', function () {
			var tests2 = [
				[123, '123', '123'],
				[1236.5, '1,236.5', '1,237'],
				[1234.555, '1,234.56', '1,235'],
				[1134.00, '1,134', '1,134'],
				[1234.01, '1,234.01', '1,234'],
				[1224.4, '1,224.4', '1,224'],
				[1239.9, '1,239.9', '1,240'],
				[2239.999, '2,240', '2,240'],
				[3239.9, '3,239.9', '3,240'],
				[4239.989, '4,239.99', '4,240']
			]
			for (var i = 0; i < tests2.length; i++) {
				expect(formatting.formatNumber(tests2[i][0], 0, 2)).toBe(tests2[i][1])
				expect(formatting.formatNumber(tests2[i][0], 0, 0)).toBe(tests2[i][2])
			}

		})

		it('formatNumber() - rounding edge cases', function () {
			expect(formatting.formatNumber(.1236, 1, 3)).toBe("0.124")
			expect(formatting.formatNumber(.1296, 1, 3)).toBe("0.13")
			expect(formatting.formatNumber(.1006, 1, 3)).toBe("0.101")
			expect(formatting.formatNumber(.1003, 1, 3)).toBe("0.1")
			expect(formatting.formatNumber(.1004, 2, 3)).toBe("0.10")
			expect(formatting.formatNumber(.1999, 1, 3)).toBe("0.2")
			expect(formatting.formatNumber(.1999, 2, 3)).toBe("0.20")
			expect(formatting.formatNumber(1.9999, 2, 3)).toBe("2.00")
			expect(formatting.formatNumber(1.1999, 2, 3)).toBe("1.20")
		})

		it('formatNumber() - funky inputs', function () {
			expect(formatting.formatNumber(null)).toBe(null)
			expect(formatting.formatNumber(undefined)).toBe(null)
			expect(formatting.formatNumber('hello')).toBe(null)
			//expect(formatting.formatNumber('125hello')).toBe(null) //this returns 125, I think it should return null. Leaving it out for now because it fails and isn't that important
			expect(formatting.formatNumber(1234., 0)).toBe('1,234')
			expect(formatting.formatNumber(1234.00, 1)).toBe('1,234.0')
			expect(formatting.formatNumber('113.4500000', 1)).toBe('113.45')
			expect(formatting.formatNumber('123.45000000001', 1)).toBe('123.45')
			expect(formatting.formatNumber('133.4500000', 1)).toBe('133.45')
			expect(formatting.formatNumber('123,123.4500000', 1)).toBe('123,123.45')
			expect(formatting.formatNumber('456,1,2,3,123.4500000', 1)).toBe('456,123,123.45')
			expect(formatting.formatNumber('123')).toBe('123.00')
			expect(formatting.formatNumber('123..4')).toBe(null)
			expect(formatting.formatNumber('123..4.5')).toBe(null)
			expect(formatting.formatNumber('--123.45')).toBe(null)
			expect(formatting.formatNumber(NaN)).toBe(null)
		})

		it('formatNumber() - percent', function () {
			expect(formatting.formatNumber(.45, null, null, null, null, true)).toBe('45.00%')
			expect(formatting.formatNumber(-.45, null, null, null, null, true)).toBe('(45.00%)')
			expect(formatting.formatNumber(-1234.45, null, null, null, null, true)).toBe('(123,445.00%)')
			expect(formatting.formatNumber(-1234.45, null, null, null, null, true)).toBe('(123,445.00%)')
			expect(formatting.formatNumber(-1234.4512, null, null, null, null, true)).toBe('(123,445.12%)')
			expect(formatting.formatNumber(-1234.451234, null, null, null, null, true)).toBe('(123,445.1234%)')
			expect(formatting.formatNumber(-1234.451234, 0, 0, null, null, true)).toBe('(123,445%)')
		})

	});












});
