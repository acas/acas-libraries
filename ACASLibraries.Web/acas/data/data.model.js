'use strict'

acas.module('acas.data.model', 'underscorejs', 'Q', function () {

	var models = {}
	var allModelsEventListeners = {}

	var loadState = {
		uninitialized: 0,
		loading: 1,
		reloading: 2,
		unstable: 3,
		loaded: 4
	}

	var saveState = {
		unsaved: 0,
		saving: 1,
		saved: 2
	}

	var isPromise = function (value) {
		return value &&
				value instanceof Object &&
				(
					(
						value.promise &&
						value.resolve &&
						value.notify
					)
					||
					(
						value.then &&
						(value.progress || value.finally) && //support angularjs $q promises in addition to kriskowal's Q promises
						value.catch
					)
				)
	}

	var consoleLog = function (data) {
		if (console && console.log)
			console.log('acas.data.model> ' + data)
	}

	var logEvent = function (modelNames, eventName, cancellable) {
		if (acas.data.model.logEvents)
			consoleLog(eventName + ' event fired' + (cancellable ? ' as cancellable' : '') + ' for ' + arrayToString(modelNames))
	}

	var arrayToString = function (values) {
		if (values instanceof Array)
			return '[' + values.join(',') + ']'
		else
			return values
	}

	var toArray = function (value) {
		return value instanceof Array ? _.clone(value) : [value]
	}

	var filterModelsForProperty = function (modelNames, property) {
		var filteredModelNames = []
		_.each(modelNames, function (modelName) {
			if (models[modelName][property] !== undefined)
				filteredModelNames.push(modelName)
		})
		return filteredModelNames
	}

	var verifyModelsDefined = function (modelNames) {
		_.each(toArray(modelNames), function (name) {
			if (!models[name])
				throw 'Model ' + name + ' not defined'
		})
	}

	var valueOrResult = function (value) {
		if (typeof value == 'function')
			return value()
		else
			return value
	}

	var asyncForEach = function (collection, async, handler) {
		var deferred = Q.defer()

		window.setTimeout(function () {
			if (async) {
				var vals = _.clone(collection).reverse()
				var total = collection.length
				var completed = 0

				var itemComplete = function () {
					completed++
					if (completed == total)
						deferred.resolve()
				}

				if (vals.length) {
					while (vals.length) {
						var itemResult = handler(vals.pop())
						if (isPromise(itemResult)) {
							itemResult.then(itemComplete)
						}
						else {
							itemComplete()
						}
					}
				}
				else {
					deferred.resolve()
				}
			} else {
				_.each(collection, function (val) { handler(val) })
				deferred.resolve()
			}
		}, 0)

		return deferred.promise
	}

	//pass modelNames == null to subscribe to events for all models
	var addModelEventListener = function (modelNames, eventName, priority) {
		if (modelNames != null) {
			modelNames = toArray(modelNames)
			verifyModelsDefined(modelNames)
			var promises = _.map(modelNames, function (name) {
				if (!models[name][eventName])
					models[name][eventName] = []
				var newDeferred = Q.defer()
				models[name][eventName].push({
					promise: newDeferred,
					priority: priority
				})
				return newDeferred.promise
			})
			if (promises.length == 1)
				return promises[0]
			else
				return promises
		}
		else {
			if (allModelsEventListeners[eventName] == null) {
				allModelsEventListeners[eventName] = []
			}
			var newDeferred = Q.defer()
			allModelsEventListeners[eventName].push({
				promise: newDeferred,
				priority: priority
			})
			return newDeferred.promise
		}
	}

	var fireModelEvent = function (modelNames, eventName, cancellable, operation) {
		var deferred = Q.defer()
		modelNames = toArray(modelNames)
		asyncForEach(modelNames, false, function (name) {
			logEvent(name, eventName, cancellable)
			var listeners = []
			_.each(models[name][eventName], function (listener) {
				listeners.push(listener)
			})
			_.each(allModelsEventListeners[eventName], function (listener) {
				listeners.push(listener)
			})
			if (listeners) {
				listeners.sort(function (a, b) {
					//sort by priority
					if (a.priority < b.priority) return -1
					else if (a.priority > b.priority) return 1
					return 0
				})
				var processEvent = function () {
					if (listeners.length) {
						var listener = listeners.pop()
						if (cancellable) {
							var result = (operation ? listener.promise.notify(name, operation) : listener.promise.notify(name, operation))
							if (result != null) {
								if (isPromise(result)) {
									result.then(processEvent)
								} else if (result !== false) {
									processEvent()
								}
							}
							else {
								(operation ? listener.promise.notify(name, operation) : listener.promise.notify(name, operation))
								processEvent()
							}
						}
						else {
							(operation ? listener.promise.notify(name, operation) : listener.promise.notify(name, operation))
							processEvent()
						}
					}
				}
				processEvent()
			}
		}, 0).then(function () {
			deferred.resolve()
		})

		return deferred.promise
	}

	var api = {
		getLoadState: function (modelName) {
			if (models[modelName] !== undefined)
				switch (models[modelName].$acDataLoadState) {
					case loadState.uninitialized:
						return 'uninitialized'
					case loadState.loading:
						return 'loading'
					case loadState.reloading:
						return 'reloading'
					case loadState.unstable:
						return 'unstable'
					case loadState.loaded:
						return 'loaded'
					default:
						throw 'Unknown model state ' + models[modelName].$acDataLoadState
				}
		},
		getSaveState: function (modelName) {
			if (models[modelName] !== undefined)
				switch (models[modelName].$acDataSaveState) {
					case saveState.uninitialized:
						return 'unitialized'
					case saveState.saving:
						return 'saving'
					case saveState.resaving:
						return 'resaving'
					case saveState.unstable:
						return 'unstable'
					case saveState.saved:
						return 'saved'
					default:
						throw 'Unknown model state ' + models[modelName].$acDataSaveState
				}
		},
		define: function (modelName, modelDefinition) {
			/*
				modelDefinition = {
					load : function | string OPTIONAL, 
					save : function | string OPTIONAL,
					loadPermission: function | boolean OPTIONAL,
					savePermission: function | boolean OPTIONAL,
					validate: function OPTIONAL | boolean OPTIONAL,
					isDirty: function OPTIONAL | boolean OPTIONAL,
					dependencies: function | array[string] OPTIONAL //array of defined model names,
					savePriority: function | int OPTIONAL //defines save priority when saving multiple models at the save time
				}
			*/

			if (!(modelName != null && modelName.length > 0))
				throw 'Model name must be defined'

			if (!(modelDefinition != null))
				throw 'Model definition is not valid'

			fireModelEvent(modelName, 'define', false)

			models[modelName] = modelDefinition
			models[modelName].$acDataLoadState = loadState.uninitialized
		},
		undefine: function (modelNames) {
			//remove the model definition
			modelNames = toArray(modelNames)
			verifyModelsDefined(modelNames)

			fireModelEvent(modelNames, 'undefine', false)

			_.each(modelNames, function (name) {
				delete models[name]
			})
		},
		events: new function () {
			return {
				define: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'define', priority)
				},
				undefine: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'undefine', priority)
				},
				require: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'require', priority)
				},
				validate: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'validate', priority)
				},
				beforeLoad: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'beforeLoad', priority)
				},
				afterLoad: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'afterLoad', priority)
				},
				beforeSave: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'beforeSave', priority)
				},
				afterSave: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'afterSave', priority)
				},
				beforeExecute: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'beforeExecute', priority)
				},
				afterExecute: function (modelNames, priority) {
					return addModelEventListener(modelNames, 'afterExecute', priority)
				},
			}
		},
		load: function (modelNames, target) {
			modelNames = toArray(modelNames).reverse()
			verifyModelsDefined(modelNames)
			modelNames = filterModelsForProperty(modelNames, 'load')

			var deferred = Q.defer()
			var modelsLoaded = 0
			fireModelEvent(modelNames, 'beforeLoad', true).then(function () {
				window.setTimeout(function () {
					asyncForEach(modelNames, valueOrResult(acas.data.model.asyncLoad), function (name) {
						var m = models[name]

						var hasPermission = false

						//wait to run this load until after the promise has been returned
						var loadComplete = function (data) {
							//add new data to target if data was provided							
							modelsLoaded++
							if (modelsLoaded == modelNames.length)
								fireModelEvent(modelNames, 'afterLoad', false).then(function () {
									deferred.resolve(target)
								})
						}

						//define loader function
						var load = function (target) {
							//check that this data is not already loading							
							if (m.$acDataLoadState !== loadState.loading && m.$acDataLoadState !== loadState.reloading) {
								//set loader state
								m.$acDataLoadState = (m.$acDataLoadState === loadState.uninitialized ? loadState.loading : loadState.reloading)

								//get load result								
								var loadResult = m.load(target)
								//check if load result is a promise
								if (isPromise(loadResult)) {
									Q(loadResult)
									.then(function (data) {
										m.$acDataLoadState = loadState.loaded
										loadComplete(data)
									})
									.catch(function () {
										m.$acDataLoadState = (m.$acDataLoadState === loadState.loading ? loadState.uninitialized : loadState.unstable)
										throw 'Load failed for model ' + name
									})
								}
								else if (loadResult) {
									//result is not a promise, check if it has a value and use result as data if so
									m.$acDataLoadState = loadState.loaded
									loadComplete(loadResult)
								}
									//load failed for some reason
								else {
									m.$acDataLoadState = (m.$acDataLoadState === loadState.loading ? loadState.uninitialized : loadState.unstable)
									throw 'Load failed for model ' + name
								}
							}
							else {
								//data is already loading, just wait for it to finish
								var waitForLoad = function () {
									//finish waiting if load is complete
									if (m.$acDataLoadState === loadState.loaded) {
										loadComplete()
									}
									else if (m.$acDataLoadState === loadState.loading || m.$acDataLoadState === loadState.reloading)
										//keep waiting if load/reload is still occurring
										window.setTimeout(waitForLoad, 10)
									else
										//load failed, stop processing
										throw 'Load failed for model ' + name
								}
								waitForLoad()
							}
						}

						//api.throwOnPermissionViolation
						if (m.loadPermission != null) {
							//check for permission
							var permissionResult = valueOrResult(m.loadPermission)

							//check if permission check was successful and immediate/boolean
							if (permissionResult === true) {
								load(target)
							}

							else if (isPromise(result))
								//check if permission check is a promise
								Q(result).then(function (hasPermission) {
									//continue loading if hasPermission is true
									if (hasPermission === true) {
										load(target)
									}
								})
							else if (throwOnPermissionViolation)
								throw 'Permission denied for loading model ' + name
						} else {
							load(target)
						}
					})
				}, 0)
			})

			return deferred.promise
		},
		require: function (modelNames, target) {
			/*
				model = string | array[string],
				target = object
			*/
			modelNames = toArray(modelNames)
			verifyModelsDefined(modelNames)

			fireModelEvent(modelNames, 'require', false)

			var deferred = Q.defer()
			var modelPromises = []
			//assemble process queue by iterating over the supplied model names
			asyncForEach(modelNames, valueOrResult(acas.data.model.asyncLoad), function (name) {
				var modelProcessQueue = []
				var modelDeferred = Q.defer()
				//add dependencies for this model
				if (models[name].dependencies != null) {
					var dependencies = valueOrResult(models[name].dependencies)
					if (!(dependencies instanceof Array)) {
						dependencies = [dependencies]
					}
					_.each(dependencies, function (dependencyName) {
						//model is not defined, check to see if it's ok to ignore this problem
						if (models[dependencyName]) {
							modelProcessQueue.push(dependencyName)
						} else if (valueOrResult(api.throwOnUndefinedDependencies)) {
							throw 'Model ' + name + ' dependency ' + dependencyName + ' not defined'
						}
					})
				}

				//load model
				var load = function () {
					var waitForExistingLoad = function () {
						if (models[name].$acDataLoadState === loadState.loaded) {
							modelDeferred.resolve(name)
						} else {
							window.setTimeout(waitForExistingLoad, 1)
						}
					}

					if(models[name].$acDataLoadState === loadState.uninitialized) {
						api.load(name, target).then(function () {
							modelDeferred.resolve(name)
						})
					} else {
						waitForExistingLoad()
					}
				}

				//model queue loader for loading dependencies
				if (modelProcessQueue.length) {
					//get the next model to process and load it
					asyncForEach(modelProcessQueue, true, function (dependencyName) {
						var loadDeferred = Q.defer()
						if (models[dependencyName].$acDataLoadState !== loadState.loaded) {
							api.load(dependencyName, target).then(function () {
								loadDeferred.resolve()
							})
						} else {
							loadDeferred.resolve()
						}
						return loadDeferred.promise
					})
					.then(load)
				} else {
					load()
				}

				return modelDeferred.promise
			})
			.then(function () {
				deferred.resolve(target)
			})

			return deferred.promise
		},
		validate: function (modelNames) {
			modelNames = toArray(modelNames).reverse()
			verifyModelsDefined(modelNames)

			fireModelEvent(modelNames, 'validate', false)

			//defer processing to allow the promise to be returned
			var deferred = Q.defer()

			var validationResults = []

			var resolve = function (valid) {
				if (!valid) {
					deferred.resolve(valid)
					return false
				} else if (modelNames.length == 0) {
					validationResults.push(valid)
					deferred.resolve(_.every(validationResults, function (v) { return v === true }))
					return false
				}
				return true
			}

			window.setTimeout(function () {
				var processValidation = true

				while (processValidation && modelNames.length) {

					var name = modelNames.pop()
					//verify model exists					
					if (!models[name]) {
						throw 'Model ' + name + ' not defined'
					}
					//check if model exists and has a validation function
					if (typeof models[name].validate == 'function') {
						var result = models[name].validate()
						if (result === true) {
							processValidation = resolve(true)
						}
						else if (isPromise(result)) {
							result.then(function (valid) {
								processValidation = resolve(valid)
							})
						}
						else {
							processValidation = resolve(false)
						}
					}
					else {
						//nothing to validate, return true by default						
						processValidation = resolve(true)
					}
				}

			}, 0)

			return deferred.promise
		},
		save: function (modelNames, target) {
			modelNames = toArray(modelNames).reverse()
			verifyModelsDefined(modelNames)
			//filter			
			modelNames = _.filter(filterModelsForProperty(modelNames, 'save'), function (name) {
				//check if dirty
				if (models[name].isDirty != null) {
					return valueOrResult(models[name].isDirty) == true
				} else {
					return true
				}
			})
			if (modelNames.length > 1)
				modelNames.sort(function (a, b) {
					//sort by priority
					if (a.savePriority < b.savePriority) return -1
					else if (a.savePriority > b.savePriority) return 1
					return 0
				})

			var deferred = Q.defer()
			var modelsSaved = 0

			if (!valueOrResult(acas.data.model.allowUnloadedModelSave)) {
				//if throwOnUnloadedModelSave is false, don't try to save the unloaded models
				if (valueOrResult(acas.data.model.throwOnUnloadedModelSave)) {
					_.each(modelNames, function (name) {
						if (models[name].$acDataLoadState != loadState.loaded) {
							throw 'Unable to save model ' + name + ', model is not in a loaded state'
						}
					})
				} else {
					modelNames = _.filter(modelNames, function (name) {
						return models[name].$acDataLoadState === loadState.loaded
					})
				}
			}

			fireModelEvent(modelNames, 'beforeSave', true).then(function () {
				var modelsToSave = modelNames.length
				asyncForEach(modelNames, valueOrResult(acas.data.model.asyncSave), function (name) {
					var m = models[name]
					//wait to run this save until after the promise has been returned
					var saveComplete = function (result) {						
						modelsSaved++
						if (modelsSaved == modelsToSave)
							//resolve the save after the afterSave event fires
							fireModelEvent(modelNames, 'afterSave', false).then(function () {
								deferred.resolve(result)
							})

					}

					var saveError = function (result) {
						// reject promise with a result on error from save
						// this operation is more likely to go wrong so an error is permissible
						deferred.reject(result)
						throw result
					}

					//define saver function
					var save = function () {
						//check that this data is not already saving
						if (m.$acDataSaveState !== saveState.saving) {
							var processSave = function () {
								//set save state
								m.$acDataSaveState = saveState.saving

								//get save result
								var saveResult = m.save(target)
								//check if save result is a promise
								if (isPromise(saveResult)) {
									Q(saveResult)
									.then(function (result) {
										m.$acDataSaveState = saveState.saved
										saveComplete(result)
									})
									.catch(function () {
										m.$acDataSaveState = saveState.unsaved
										saveError('Save failed for model ' + name)
									})
								}
								else if (saveResult) {
									//result is not a promise, check if it has a value and use result as data if so
									m.$acDataSaveState = saveState.saved
									saveComplete(saveResult)
								}
								else {
									//save failed for some reason
									m.$acDataSaveState = saveState.unsaved
									saveError('Save failed for model ' + name)
								}
							}

							api.validate(name).then(function (valid) {
								if (valid) {
									processSave()
								}
							})
						}
						else {
							//data is already saving, just wait for it to finish
							var waitForSave = function () {
								//finish waiting if save is complete
								if (m.$acDataSaveState === saveState.saved)
									saveComplete()
								else if (m.$acDataSaveState === saveState.saving)
									//keep waiting if save/resave is still occurring
									window.setTimeout(waitForSave, 10)
								else
									//save failed, stop processing
									saveError('Save failed for model ' + name)
							}
							waitForSave()
						}
					}


					//acas.data.save.throwOnPermissionViolation
					if (m.savePermission != null) {
						//check for permission
						var permissionResult = valueOrResult(m.savePermission)

						//check if permission check was successful and immediate/boolean
						if (permissionResult === true)
							save()
						else if (isPromise(result))
							//check if permission check is a promise
							Q(result).then(function (hasPermission) {
								//continue saving if hasPermission is true
								if (hasPermission === true)
									save()
							})
						else if (throwOnPermissionViolation)
							saveError('Permission denied for saving model ' + name)
					} else {
						save()
					}
				})
			})

			return deferred.promise
		},
		saveAll: function (target) {			
			var modelNames = Object.keys(models)			
			return api.save(modelNames, target)
		},
		execute: function (modelNames, operation, target) {
			modelNames = toArray(modelNames).reverse()
			verifyModelsDefined(modelNames)
			//filter			
			modelNames = _.filter(filterModelsForProperty(modelNames, operation), function (name) {
				//check if dirty
				if (models[name].isDirty != null) {
					return valueOrResult(models[name].isDirty) == true
				} else {
					return true
				}
			})
			if (modelNames.length > 1)
				modelNames.sort(function (a, b) {
					//sort by priority
					if (a[opertaion + 'Priority'] < b[opertaion + 'Priority']) return -1
					else if (a[opertaion + 'Priority'] > b[opertaion + 'Priority']) return 1
					return 0
				})

			var deferred = Q.defer()
			var modelsExecuted = 0

			if (!valueOrResult(acas.data.model.allowUnloadedModelExecute)) {
				//if throwOnUnloadedModelExecute is false, don't try to execute the unloaded models
				if (valueOrResult(acas.data.model.throwOnUnloadedModelExecute)) {
					_.each(modelNames, function (name) {
						if (models[name].$acDataLoadState != loadState.loaded) {
							throw 'Unable to execute model ' + name + ', model is not in a loaded state'
						}
					})
				} else {
					modelNames = _.filter(modelNames, function (name) {
						return models[name].$acDataLoadState === loadState.loaded
					})
				}
			}

			fireModelEvent(modelNames, 'beforeExecute', true).then(function () {
				var modelsToExecute = modelNames.length
				asyncForEach(modelNames, valueOrResult(acas.data.model.asyncExecute), function (name) {
					var m = models[name]
					//wait to run this execute until after the promise has been returned
					var executeComplete = function (result) {
						modelsExecuted++
						if (modelsExecuted == modelsToExecute)
							//resolve the execute after the afterExecute event fires
							fireModelEvent(modelNames, 'afterExecute', false).then(function () {
								deferred.resolve(result)
							})

					}

					//define executer function
					var execute = function () {
						//check that this data is not already saving
						var processExecute = function () {
							//get execute result
							var executeResult = m[operation](target)
							//check if execute result is a promise
							if (isPromise(executeResult)) {
								Q(executeResult)
								.then(function (result) {
									executeComplete(result)
								})
								.catch(function () {
									throw 'Execute '+operation+' failed for model ' + name
								})
							}
							else if (executeResult) {
								//result is not a promise, check if it has a value and use result as data if so
								executeComplete(executeResult)
							}
							else {
								//execute failed for some reason
								throw 'Execute '+operation+' failed for model ' + name
							}
						}

						api.validate(name).then(function (valid) {
							if (valid) {
								processExecute()
							}
						})
					}


					if (m[operation+'Permission'] != null) {
						//check for permission
						var permissionResult = valueOrResult(m[operation + 'Permission'])

						//check if permission check was successful and immediate/boolean
						if (permissionResult === true)
							execute()
						else if (isPromise(result))
							//check if permission check is a promise
							Q(result).then(function (hasPermission) {
								//continue executing if hasPermission is true
								if (hasPermission === true)
									execute()
							})
						else if (throwOnPermissionViolation)
							throw 'Permission denied for executing ' + operation + ' on model ' + name
					} else {
						execute()
					}
				})
			})

			return deferred.promise
		}
	}

	if (typeof window.acas == 'undefined') window.acas = {}
	function init() {
		_.extend(window.acas, {
			data: {
				model: {
					allowUnloadedModelSave: false,
					allowUnloadedModelExecute: true,

					throwOnPermissionViolation: true,
					throwOnUndefinedDependencies: true,
					throwOnUnloadedModelSave: true,
					throwOnUnloadedModelExecute: true,

					asyncLoad: true,
					asyncSave: true,
					asyncExecute: true,

					logEvents: false,

					events: {
						define: api.events.define,
						undefine: api.events.undefine,
						require: api.events.require,
						validate: api.events.validate,
						beforeLoad: api.events.beforeLoad,
						afterLoad: api.events.afterLoad,
						beforeSave: api.events.beforeSave,
						afterSave: api.events.afterSave,
						beforeExecute: api.events.beforeExecute,
						afterExecute: api.events.afterExecute,
					},

					getLoadState: api.getLoadState,
					getSaveState: api.getSaveState,
					define: api.define,
					undefine: api.undefine,
					load: api.load,
					require: api.require,
					validate: api.validate,
					save: api.save,
					saveAll: api.saveAll,
					execute: api.execute
				}
			}
		})
	}

	init()
})