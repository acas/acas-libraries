module.exports = function (grunt) {
	grunt.initConfig({
		pkg: grunt.file.readJSON('package.json'),
		copy: {
			files: {
				cwd: 'acas',
				src: ['**/*.html', '**/*.png'],
				dest: 'dist/<%=pkg.version %>',
				expand: true
			}
		},
		concat: {
			options: {
				banner: '/* \nThis is the compiled (but unminified) <%= pkg.name %> project, version <%= pkg.version %>. ' +
						'Released <%= grunt.template.today("yyyy-mm-dd") %> \n<%= pkg.description %> \n*/\n\n'
			},
			js: {
				files: {
					'dist/<%=pkg.version %>/<%= pkg.name %>.js': [
					'acas/module.js',
					'acas/config.js',
					'acas/utility/utility.js',
					'acas/**/*.Module.js',
					'acas/**/*.Service.js',
					'acas/**/*.Directive.js',
					'acas/**/*.Filter.js',
					'acas/**/*.Controller.js',
					'acas/**/!(*.test).js'
					]
				}
			}
		},
		uglify: {
			options: {
				banner: '/* \nThis is the compiled and minified <%= pkg.name %> project, version <%= pkg.version %>. ' +
						'Released <%= grunt.template.today("yyyy-mm-dd") %> \n<%= pkg.description %> \n*/\n\n'
			},
			js: {
				files: { 'dist/<%=pkg.version %>/<%= pkg.name %>.min.js': 'dist/<%=pkg.version %>/<%= pkg.name %>.js' }
			}
		},
		concat_css: {
			all: {
				src: ['acas/**/*.css'],
				dest: 'dist/<%=pkg.version %>/<%=pkg.name%>.css'
			}
		},
		cssmin: {
			combine: {
				files: {
					'dist/<%=pkg.version %>/<%=pkg.name%>.min.css': ['acas/**/*.css']
				}
			}
		},

		bump: {
			options: {
				files: ['package.json'],
				updateConfigs: [],
				commit: false,
				createTag: false,
				push: false
			}
		},
		karma: {
			options: {
				configFile: 'karma.conf.js'
			},
			continuous: {
				singleRun: false
			},
			single: {
				singleRun: true //the build exits as soon as the tests are done
			},
			coverage: {
				coverageReporter: {
					type: 'html',
					dir: 'coverage/'
				},
				singleRun: true
			}
		},
		watch: {
			'default': {
				files: ['acas/**/*'],
				tasks: ['default'],
				options: {
					interrupt: true
				}
			},
			'no-tests': {
				files: ['acas/**/*'],
				tasks: ['build'],
				options: {
					interrupt: true
				}
			}

		},
		connect: {
			server: {
				options: {
					port: 8282,
					base: 'dist/<%=pkg.version %>/',
					middleware: function (connect, options, middlewares) {
						// inject a custom middleware to allow CORS - 
						//important so that the consuming application can reference html resources in ACAS Libraries using standard angular ajax methods
						middlewares.unshift(function (req, res, next) {
							res.setHeader('Access-Control-Allow-Origin', '*');
							res.setHeader('Access-Control-Allow-Methods', '*');
							return next();
						});

						return middlewares;
					}
				}
			}
		}
	});

	grunt.loadNpmTasks('grunt-contrib-concat');
	grunt.loadNpmTasks('grunt-contrib-uglify');
	grunt.loadNpmTasks('grunt-contrib-copy');
	grunt.loadNpmTasks('grunt-bump');
	grunt.loadNpmTasks('grunt-concat-css');
	grunt.loadNpmTasks('grunt-contrib-cssmin');
	grunt.loadNpmTasks('grunt-karma');
	grunt.loadNpmTasks('grunt-contrib-watch');
	grunt.loadNpmTasks('grunt-contrib-connect');

	//this just compiles the files, no tests
	grunt.registerTask('build', ['concat', 'uglify', 'copy', 'concat_css', 'cssmin'])

	//'grunt' will test and build the project 
	grunt.registerTask('default', ['karma:single', 'build']);

	//this simply serves the files on localhost
	grunt.registerTask('serve', ['connect'])

	//'grunt develop' will run the tests/build (like 'grunt') and then repeat that when the files change. 
	//the testing framework has to startup each time, so it's a bit slow. But it'll build the files for you.
	//TODO: is there a way to keep karma running and still build every time the files change?
	grunt.registerTask('develop', ['default', 'serve', 'watch:default', ])

	grunt.registerTask('develop-no-tests', ['build', 'serve', 'watch:no-tests', ])


	//'grunt test' will start the test runner and keep it open with autoWatch on (equivalent to 'karma start')
	//this won't build the files when they change, but the tests will be fast
	grunt.registerTask('test', ['karma:continuous'])

	//'grunt coverage-report' will run the tests and generate the coverage report html 
	//instead of just the summary that's usually in the console
	grunt.registerTask('coverage-report', ['karma:coverage'])

	//'grunt bump' will bump the version one minor version (iteration)
	grunt.registerTask('version', ['bump']);
};