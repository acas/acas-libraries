
describe('acas.formatting', function () {



	beforeEach(function () {
		module('acas.formatting.angular');
	})
	describe('number formatting: ', function () {
		//these tests just verify that we pass through to formatNumber() correctly, they run over these values

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

});
