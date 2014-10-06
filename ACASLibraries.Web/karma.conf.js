module.exports = function (config) {
	config.set({

		// base path, that will be used to resolve files and exclude
		basePath: './',

		// frameworks to use
		frameworks: ['jasmine'],

		// list of files / patterns to load in the browser
		files: [
					'http://code.jquery.com/jquery-1.11.1.min.js',

					'lib/angular.min.js', //always test against bleeding edge angular, see how that works
					'lib/angular-mocks.js',					   
					'http://cdnjs.cloudflare.com/ajax/libs/q.js/1.0.1/q.min.js',
					
					'http://cdnjs.cloudflare.com/ajax/libs/underscore.js/1.6.0/underscore-min.js',					
					'http://cdnjs.cloudflare.com/ajax/libs/angular-ui-bootstrap/0.10.0/ui-bootstrap.min.js',

					'acas/module.js',
					'acas/config.js',
					'acas/utility/utility.js',
					'acas/**/*.Module.js',
					'acas/**/*.Service.js',
					'acas/**/*.Directive.js',
					'acas/**/*.Filter.js',
					'acas/**/*.Controller.js',					
					'acas/**/!(*.test).js',					

					'acas/**/*.test.js'

		],
		
		preprocessors: {
			'acas/**/!(*.test).js': 'coverage'
			},

		coverageReporter : {
			type : 'text-summary'				
		},

		// the progress reporter is responsible for reporting how many tests were run in how much time
		//coverage outputs the coverage summary
		reporters: ['progress', 'coverage'],

		// web server port
		port: 9876,

		// enable / disable colors in the output (reporters and logs)
		colors: true,
		
		logLevel: config.LOG_INFO,
		
		// Start these browsers
		browsers: ['PhantomJS'],

		// If browser does not capture in given timeout [ms], kill it
		captureTimeout: 60000,

		//autoWatch will keep watching the files and the tests will run continuously		
		//the grunt build task can override singleRun to have the tests run just once and exit
		autoWatch: true,
		singleRun: false
		
	});
};