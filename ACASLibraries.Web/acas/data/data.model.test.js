'use strict';



describe('acas.data.model module', function () {

	it('api should contain appropriate functions', function () {
		expect(acas.data.model.define).toBeDefined
		expect(acas.data.model.require).toBeDefined
		expect(acas.data.model.validate).toBeDefined
		expect(acas.data.model.save).toBeDefined
		expect(acas.data.model.save).toBeDefined

	})

})

describe('acas.data.model ', function () {

	beforeEach(function () {
		var initialize = function () {			
			acas.data.model.define('test1', {
				load: function (t) {
					console.log('running load 1')
					_.extend(t, { test1: '1' })
					return t
				},
				save: function () {
					var deferred = Q.defer()
					window.setTimeout(function () {
						deferred.resolve()
					}, 2000)
					return deferred.promise
				}
			})
			acas.data.model.define('test2', {
				load: function (t) {					
					var deferred = Q.defer()
					window.setTimeout(function () {
						_.extend(t, { test2: '2' })
						console.log('running load 2')
						deferred.resolve(t)
					}, 1000)
					return deferred.promise
				},
				validate: function () {
					return true
				},
				save: function () {
					return true
				}
			})
			acas.data.model.define('test3', {
				load: function (t) {
					console.log('running load 3')
					_.extend(t, { test3: '3' })
					return t
				},
				save: function () {
					return t
				},
				validate: function () {
					var deferred = Q.defer()
					window.setTimeout(function () {
						deferred.resolve(false)
					}, 2000)
					return deferred.promise
				},
				dependencies: ['test1', 'test2']
			})
		}
		initialize()
	})

	it('should create a model with define() ', function () {
		acas.data.model.define('test', {
			load: function () {
				return { data: '1' }
			}
		})
		expect(acas.data.model.getLoadState('test')).toBe('uninitialized')
		expect(acas.data.model.getLoadState('some-other-name')).toBeUndefined
	})
	var target = {}
	beforeEach(function (done) {						
		acas.data.model.require(['test1', 'test3'], target).then(function (target) {
			expect(target.test1).toEqual('1')
			expect(target.test2).toEqual('2')
			expect(target.test3).toEqual('3')
			done()
		})
	})

	it('should load data with require()', function () {		
		expect(target.test1).toEqual('1')
		expect(target.test2).toEqual('2')
		expect(target.test3).toEqual('3')
		

	})

	//it('acas.data.model.validate()', function () {

	//	var target = {}
	//	var promiseRequire = acas.data.model.require(['test1', 'test3'], target)
	//	acas.data.model.validate('test1').then(function (valid) {
	//		expect(valid).toEqual(true)
	//	})
	//	acas.data.model.validate('test2').then(function (valid) {
	//		expect(valid).toEqual(true)
	//	})
	//	promiseRequire.then(function (data) {
	//		acas.data.model.validate('test3').then(function (valid) {
	//			expect(valid).toEqual(false)
	//		})
	//	})
	//})

	//it('acas.data.model.save()', function () {


	//	var target = {}
	//	var promiseRequire = acas.data.model.require(['test1', 'test3'], target)
	//	var required = false
	//	var test1saved = false
	//	var test23saved = 0
	//	promiseRequire.then(function () {
	//		required = true
	//		acas.data.model.save('test1').then(function () {
	//			test1saved = true
	//		})
	//		acas.data.model.save(['test2', 'test3']).then(function () {
	//			test23saved++
	//		})
	//	})

	//	jasmine.Clock.useMock()
	//	jasmine.Clock.tick(8000)
	//	//expect(required).toEqual(true)
	//	//expect(test1saved).toEqual(true)
	//	//expect(test23saved).toEqual(2)
	//})

})


window.setTimeout(function () {



	var activeTest = function () {


		var saveTest = function () {
			var target = {}
			var required = false
			var test1saved = false
			var test23saved = 0
			acas.data.model.require(['test1', 'test3'], target).then(function () {
				required = true
				acas.data.model.save('test1').then(function () {
					test1saved = true
				})
				_.each(acas.data.model.events.afterSave(['test2', 'test3']), function (promise) {
					promise.progress(function () {
						test23saved++
					})
				})
				window.setTimeout(function () {
					acas.data.model.save(['test2', 'test3'])
				}, 3000)
			})

			acas.data.model.events.define().progress(function () {
				console.log('define listener called')
			})

			window.setTimeout(function () {
				console.log("test1saved is true = " + test1saved)
				console.log("test23saved is 2 = " + test23saved)
			}, 10000)
		}()

	}()

}, 5000)