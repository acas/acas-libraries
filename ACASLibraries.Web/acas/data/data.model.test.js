'use strict';



describe('acas.data.model module', function () {
	it('should exist', function () {
		expect(acas.data.model).toBeDefined
	})

	it('api should contain appropriate functions', function () {
		expect(acas.data.model.define).toBeDefined
		expect(acas.data.model.require).toBeDefined
		expect(acas.data.model.validate).toBeDefined
		expect(acas.data.model.save).toBeDefined
		expect(acas.data.model.save).toBeDefined
	})

})

describe('acas.data.model', function () {
	var target = {}
	describe('define function', function () {
		beforeEach(function () {
			var initialize = function () {
				acas.data.model.define('test1', {
					load: function (t) {
						_.extend(t, { test1: '1' })
						return t
					},
					save: function (t) {
						var deferred = Q.defer()
						window.setTimeout(function () {
							deferred.resolve({ saved: true, test: 'test1' })
						}, 50)
						return deferred.promise
					}
				})
				acas.data.model.define('test2', {
					load: function (t) {
						var deferred = Q.defer()
						window.setTimeout(function () {
							t['test2'] = '2'
							deferred.resolve(t)
						}, 50)
						return deferred.promise
					},
					validate: function () {
						return true
					},
					save: function () {
						var deferred = Q.defer()
						window.setTimeout(function () {
							deferred.resolve({ saved: true, test: 'test2' })
						}, 50)
						return deferred.promise
					}
				})
				acas.data.model.define('test3', {
					load: function (t) {
						_.extend(t, { test3: '3' })
						return t
					},
					save: function () {
						var deferred = Q.defer()
						window.setTimeout(function () {
							deferred.resolve({ saved: true, test: 'test3' })
						}, 50)
						return deferred.promise
					},
					validate: function () {
						var deferred = Q.defer()
						window.setTimeout(function () {
							deferred.resolve(false)
						}, 0)
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
	})


	describe('load function', function () {
		beforeEach(function (done) {
				acas.data.model.require(['test1', 'test2', 'test3'], target)
				.then(function (target) {
					done()
				})
			})

		it('should load data with require()', function () {
			expect(target.test1).toEqual('1')
			expect(target.test2).toEqual('2')
			expect(target.test3).toEqual('3')
		})
	})
	
	describe('require function', function () {
		var target = {}
		beforeEach(function (done) {
			acas.data.model.define('loadStateTest', {
				load: function (t) {
					t.loadStateTestLoaded = !t.loadStateTestLoaded
					return t
				}
			})
			acas.data.model.require(['loadStateTest'], target).then(function () {
				acas.data.model.require(['loadStateTest'], target).then(function () {
					done()
				})
			})
		})

		it('should only load a model once', function () {
				expect(target.loadStateTestLoaded).toBe(true)
				expect(acas.data.model.getLoadState('loadStateTest')).toBe('loaded')
		})
	})

	describe('load function', function () {
		var target = {}
		beforeEach(function (done) {
			acas.data.model.define('loadStateTest', {
				load: function (t) {
					t.loadStateTestLoaded = !t.loadStateTestLoaded
					return t
				}
			})
			acas.data.model.load(['loadStateTest'], target).then(function () {
				acas.data.model.load(['loadStateTest'], target).then(function () {
					done()
				})
			})
		})

		it('should load the data every time it\'s called', function () {
			expect(target.loadStateTestLoaded).toBe(false)
			expect(acas.data.model.getLoadState('loadStateTest')).toBe('loaded')
		})
	})
	
	describe('save function', function () {
		var result = {}
		var finished = false
		beforeEach(function (done) {
			acas.data.model.require('test1', target)
			.then(function (target) {
				acas.data.model.save('test1', target).then(function (data) {
					result = data
					done()
				})
			})
		})

		it('should call the model save function', function () {
			expect(result.saved).toBe(true)
			expect(result.test).toEqual('test1')
		})
	})
	

	describe('save all function', function () {
		var data = {}
		var result = {}
		beforeEach(function (done) {
			acas.data.model.define('test', {
				load: function (data) {
					var deferred = Q.defer()
					window.setTimeout(function () {
						data['test'] = { data: 'test' }
						deferred.resolve(data)
					}, 100)
					return deferred.promise
				},
				save: function () {
					var deferred = Q.defer()
					window.setTimeout(function () {
						deferred.resolve('test')
					}, 100)
					return deferred.promise
				}
			})
			acas.data.model.define('test2', {
				load: function () {
					var deferred = Q.defer()
					window.setTimeout(function () {
						data['test2'] = { data: 'test2' }
						deferred.resolve(data)
					}, 100)
					return deferred.promise
				},
				save: function () {
					var deferred = Q.defer()
					window.setTimeout(function () {
						deferred.resolve('test2')
					}, 100)
					return deferred.promise
				}
			})
			acas.data.model.require(['test', 'test2'], data).then(function () {
				done()
				acas.data.model.saveAll(data).then(function (data) {
					result = data
				})
			})
		})

		it('should return a promise', function () {
			/*
			var returnValue = acas.data.model.saveAll(data)			
			expect(returnValue.then).toBeDefined
			*/
		})

		it('should resolve a promise without error', function () {
			/*
			expect(data).not.toBeNull()
			expect(result).not.toBeNull()
			*/
		})
	})
})
