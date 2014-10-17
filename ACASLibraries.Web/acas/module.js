/*************
Defines the dependency checking mechanism for acas libraries code. Scroll to the bottom to add support for more third party libraries
*******************/

if (typeof (acas) !== 'undefined') {
	throw 'The acas object is already defined, so ACAS Libraries code cannot be initialized'
}

var acas = {
	modules: [],
	version: 'Not implemented yet.', //this should be updated during the deploy/build
	cdnBaseUrl: (function () { //this will run in the compiled acas-libraries[.min].js file. get the url it is served from
			var url = document.scripts[document.scripts.length - 1].src
			var file = Math.max(url.indexOf('acas-libraries.min.js'), url.indexOf('acas-libraries.js'))
			return document.scripts[document.scripts.length - 1].src.substr(0, file)
	})(),
	listModules: function () {
		if (typeof (console) === 'object') {
			for (var i in this.modules) {
				var module = this.modules[i]
				if (module.loaded) {
					console.log(module.name + ': Loaded Successfully')
				} else {
					if (module.thirdPartyLibrary) {
						console.warn(module.name + ' is not loaded.')
					}
					else {
						console.warn(module.name + ': ' + module.message)
					}

				}

			}
		}
	},

	libraryDependency: function (name, checkLoadedFunction) {
		//a library dependency is treated just like any other module, except for how the loaded attribute is calculated.
		//the checkLoadedFunction should return a truthy value if the library is loaded, a falsy value if it's not. 
		//no message is supported at this time
		this.modules.push({
			name: name,
			loaded: !!checkLoadedFunction(),
			thirdPartyLibrary: true
		})
	},

	module: function () {
		//this function creates a module if all the dependencies are there
		//the last argument must be a function that should be executed, the first must be the name of the module, 
		//and rest are optional and should contain the dependencies to be checked before running that function. 
		//If any of the dependencies are not there, the function will be skipped 
		//In either case, the outcome is stored in acas.modules

		var isLoaded = function (dependency) {
			var isLoaded = false

			for (var i in acas.modules) {
				if (acas.modules[i].name === dependency && acas.modules[i].loaded) {
					isLoaded = true
					break
				}
			}
			return isLoaded
		}

		//some setup and error checking
		if (arguments.length < 2) { return } //a module cannot be created without at least a name and a function

		var name = arguments[0]
		var fn = arguments[arguments.length - 1]

		for (var i = 0; i < acas.modules.length; i++) {
			if (acas.modules[i].name === name) {
				if (typeof (console) === 'object') {
					console.error('Failure to create acas libraries module "' + name + '". An ACAS Libraries module with that name already exists.')
				}
				return
			}
		}

		if (typeof (fn) !== 'function') {
			if (typeof (console) === 'object') {
				console.error('Failure to create acas libraries module "' + name + '". An ACAS Libraries module cannot be created without a final argument of type function.')
			}
			return
		}

		//check if all dependencies are present
		var missingDependencies = []
		for (var i = 1; i < arguments.length - 1; i++) {
			if (!isLoaded(arguments[i])) {
				missingDependencies.push(arguments[i])
			}
		}
		//create the module, if possible
		if (missingDependencies.length === 0) {
			acas.modules.push({
				name: name,
				loaded: true,
				thirdPartyLibrary: false
			})
			arguments[arguments.length - 1]() //everything is loaeded. yay! execute the function
		}
		else {
			acas.modules.push({
				name: name,
				loaded: false,
				thirdPartyLibrary: false,
				message: 'ACAS Libraries module "' + name + '" was not loaded because of missing dependencies: ' + missingDependencies.join(', ')
			})
		}

	},

	//instantiate the structure early so that modules within these objects don't have to worry about other modules not creating this structure (especially if angular isn't loaded)
	utility: {},
	reporting: {},
	formatting: {},
	notifications: {},
	ui: {}
}

//create modules for the third party library dependencies supported
acas.libraryDependency('underscorejs', function () {
	return (typeof (_) === 'function' && !!_.VERSION) //currently any version is acceptable
})
acas.libraryDependency('jquery', function () {
	return (typeof (jQuery) === 'function') //currently any version is acceptable
})
acas.libraryDependency('angularjs', function () {
	return (typeof (angular) === 'object' && !!angular.version) //currently any version is acceptable
})
acas.libraryDependency('jquery.noty', function () {
	return (typeof (jQuery) === 'function') && (typeof (jQuery.noty) === 'object')
})
acas.libraryDependency('jquery.blockUI', function () {
	return (typeof (jQuery) === 'function') && (typeof (jQuery.blockUI) === 'function')
})

acas.libraryDependency('ui.bootstrap', function () {	
	try {
		angular.module('####', [])
		angular.injector(['####', 'ng', 'ui.bootstrap']).get('$modal')
	}
	catch (ex) {
		return false
	}
	return true
})

acas.libraryDependency('Q', function () {
	return (typeof (Q) === 'function') && (typeof(Q.defer) === 'function')
})

acas.libraryDependency('jquery.ui', function () {
	return (typeof(jQuery) === 'function') && (typeof(jQuery.ui) === 'object' && !!jQuery.ui.version)
})

acas.libraryDependency('select2', function () {
	return jQuery && jQuery.fn.select2 && typeof(jQuery.fn.select2.defaults) === 'object'
})