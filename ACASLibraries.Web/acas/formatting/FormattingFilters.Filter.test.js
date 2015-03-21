
describe('acas.formatting', function () {

	beforeEach(function () {
		module('acas.formatting.angular');
	})

	describe('numeric formatting: ', function () {
		// these tests just verify that we pass through to formatNumber() correctly, they run over these values

		var values = [1, 11, 111, 1111, 111111111111, 132456789098, 0, .123, 0.12345, '000.43255', '00000', '00000.12354', 1234512341234.3, 23., 234.0, 23.42452]
		for (var i in values) {
			values.push(values[i] * -1)
		}

		it('has correct defaults', function () {
			expect(acas.config.numericDisplayDefaults).toEqual({
				minPrecision: 2,
				maxPrecision: 10,
				thousandsSeparator: true,
				negativeParenthesis: true
			})
		})

		describe('acFormatMoney filter', function () {
			it('exists', inject(function ($filter) {
				expect($filter('acFormatMoney')).not.toBeNull();
			}));

			it('formats properly', inject(function (acFormatMoneyFilter) {
				for (var i in values) {
					expect(acFormatMoneyFilter(i)).toBe(acas.utility.formatting.formatNumber(i, 2, 2))
				}
			}));
		})

		describe('acFormatInteger filter', function () {

			it('exists', inject(function ($filter) {
				expect($filter('acFormatNumber')).not.toBeNull();
			}));

			it('formats properly', inject(function (acFormatIntegerFilter) {
				for (var i in values) {
					expect(acFormatIntegerFilter(i)).toBe(acas.utility.formatting.formatNumber(i, 0, 0))
				}
			}));
		})

		describe('acFormatNumber filter', function () {
			it('exists', inject(function ($filter) {
				expect($filter('acFormatNumber')).not.toBeNull();
			}));

			it('formats properly with defaults', inject(function (acFormatNumberFilter) {
				for (var i in values) {
					expect(acFormatNumberFilter(i)).toBe(acas.utility.formatting.formatNumber(i))
					expect(acFormatNumberFilter(i)).toBe(acas.utility.formatting.formatNumber(i, acas.config.numericDisplayDefaults.minPrecision, acas.config.numericDisplayDefaults.maxPrecision))
				}
			}))

			it('formats properly with varying precision', inject(function (acFormatNumberFilter) {
				for (var min = 0; min <= 15; min++) {
					for (var max = min; max <= 15; max++) {
						for (var i in values) {
							expect(acFormatNumberFilter(i, { minPrecision: min, maxPrecision: max })).toBe(acas.utility.formatting.formatNumber(i, min, max))
						}
					}
				}

			}))

			it('matches acFormatInteger when set to {0, 0}', inject(function (acFormatNumberFilter, acFormatIntegerFilter) {
				for (var i in values) {
					expect(acFormatNumberFilter(i, { minPrecision: 0, maxPrecision: 0 })).toBe(acFormatIntegerFilter(i))
				}
			}))

			it('matches acFormatMoney when set to {2, 2}', inject(function (acFormatNumberFilter, acFormatMoneyFilter) {
				for (var i in values) {
					expect(acFormatNumberFilter(i, { minPrecision: 2, maxPrecision: 2 })).toBe(acFormatMoneyFilter(i))
				}
			}))
		})

		describe('acFormatPercent filter', function () {
		    it('exists', inject(function ($filter) {
		        expect($filter('acFormatPercent')).not.toBeNull();
		    }))

		    it('formats properly', inject(function ($filter) {
		        expect($filter('acFormatPercent')('')).toBeNull()
		        expect($filter('acFormatPercent')(null)).toBeNull()
		        expect($filter('acFormatPercent')('0.11')).toBe('11.00%')
		        expect($filter('acFormatPercent')('1.0')).toBe('100.00%')
		    }))
		})
	})

	describe('datetime formatting', function () {
		describe('acFormatDate filter', function () {
			it('exists', inject(function ($filter) {
				expect($filter('acFormatDate')).not.toBeNull();
			}));

			it('formats properly', inject(function ($filter) {
				expect($filter('acFormatDate')('')).toBe('')
				expect($filter('acFormatDate')(null)).toBe('')
				expect($filter('acFormatDate')('2011-03-31T00:00:00')).toBe('3/31/2011')
				expect($filter('acFormatDate')('2012-03-30T12:01:00')).toBe('3/30/2012')
			}))
		})

		describe('acFormatTime filter', function () {
			it('exists', inject(function ($filter) {
				expect($filter('acFormatTime')).not.toBeNull();
			}));

			it('formats properly', inject(function ($filter) {
				expect($filter('acFormatTime')('')).toBe('')
				expect($filter('acFormatTime')(null)).toBe('')
				expect($filter('acFormatTime')('2011-03-31T00:00:00')).toBe('12:00 AM')
				expect($filter('acFormatTime')('2012-03-30T12:01:00')).toBe('12:01 PM')
			}))
		})

		describe('acFormatDateTime filter', function () {
			it('exists', inject(function ($filter) {
				expect($filter('acFormatDateTime')).not.toBeNull();
			}));

			it('formats properly', inject(function ($filter) {
				expect($filter('acFormatDateTime')('')).toBe('')
				expect($filter('acFormatDateTime')(null)).toBe('')
				expect($filter('acFormatDateTime')('2011-03-31T00:00:00')).toBe('3/31/2011 12:00 AM')
				expect($filter('acFormatDateTime')('2012-03-30T12:01:00')).toBe('3/30/2012 12:01 PM')
			}))
		})
	})

	describe('username formatting', function () {
		describe('acFormatUsername filter', function () {
			it('exists', inject(function ($filter) {
				expect($filter('acFormatUsername')).not.toBeNull();
			}));

			it('formats properly', inject(function ($filter) {
				expect($filter('acFormatDateTime')('')).toBe('')
				// won't work on this (shows up as ACASJonathan Fast, which might be incorrect?):
			    // expect($filter('acFormatUsername')('ACAS\Jonathan.Fast')).toBe('Jonathan Fast')
				expect($filter('acFormatUsername')(null)).toBe('')
				expect($filter('acFormatUsername')('Aaron.Greenwald')).toBe('Aaron Greenwald')
				expect($filter('acFormatUsername')('Brian.Miller')).toBe('Brian Miller')
			}))
		})
	})

	describe('yes/no formatting', function () {
		it('should handle normal values correctly', inject(function (acFormatYesNoFilter) {
			expect(acFormatYesNoFilter(0)).toBe('No')
			expect(acFormatYesNoFilter(false)).toBe('No')
			expect(acFormatYesNoFilter('0')).toBe('No')

			expect(acFormatYesNoFilter(1)).toBe('Yes')
			expect(acFormatYesNoFilter(true)).toBe('Yes')
			expect(acFormatYesNoFilter('1')).toBe('Yes')

			expect(acFormatYesNoFilter('')).toBe('')
			expect(acFormatYesNoFilter(null)).toBe('')
			expect(acFormatYesNoFilter(undefined)).toBe('')
		}))

		it('should handle less common values correctly', inject(function (acFormatYesNoFilter) {
			expect(acFormatYesNoFilter(-1)).toBe('Yes')			
			expect(acFormatYesNoFilter('-1')).toBe('Yes')
			expect(acFormatYesNoFilter('0.000')).toBe('No')			
			expect(acFormatYesNoFilter('0.001')).toBe('Yes')
			expect(acFormatYesNoFilter('hello there')).toBe('Yes')
			expect(acFormatYesNoFilter(NaN)).toBe('No')

		}))

		it('should handle funky values correctly', inject(function (acFormatYesNoFilter) {
			expect(acFormatYesNoFilter('       ')).toBe('')
			expect(acFormatYesNoFilter('       0  ')).toBe('No')
			expect(acFormatYesNoFilter('true')).toBe('Yes')
			expect(acFormatYesNoFilter('false')).toBe('Yes')
		}))		
	})

	describe('absolute value formatting', function () {
		it('should take absolute value of input', inject(function (acAbsoluteValueFilter) {			
			expect(acAbsoluteValueFilter(0)).toBe(0)
			expect(acAbsoluteValueFilter('0')).toBe(0)
			expect(acAbsoluteValueFilter('-0')).toBe(0)
			expect(acAbsoluteValueFilter(-0)).toBe(0)

			expect(acAbsoluteValueFilter('124')).toBe(124)
			expect(acAbsoluteValueFilter('-1233')).toBe(1233)
			expect(acAbsoluteValueFilter('-1233.1')).toBe(1233.1)

			expect(acAbsoluteValueFilter('')).toBe('')
			expect(acAbsoluteValueFilter('boo')).toBe('')
			expect(acAbsoluteValueFilter('null')).toBe('')
			expect(acAbsoluteValueFilter('undefined')).toBe('')
			expect(acAbsoluteValueFilter(null)).toBe('')
			expect(acAbsoluteValueFilter(undefined)).toBe('')			

			expect(acAbsoluteValueFilter(-23)).toBe(23)
			expect(acAbsoluteValueFilter(-23.34)).toBe(23.34)
			expect(acAbsoluteValueFilter(-23.544)).toBe(23.544)
			expect(acAbsoluteValueFilter(24.544)).toBe(24.544)
			expect(acAbsoluteValueFilter(24.)).toBe(24)
		}))
	})

});
