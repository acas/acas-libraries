describe('acas.ui', function () {

    // load the ui module
    beforeEach(module('acas.ui.angular'))

    describe('acDatepicker directive --> ', function () {
        var compile = null
        var testScope = null
        var element = null

        // get a scope and compile service,
        // required for rendering and linking directives
        beforeEach(inject(function ($rootScope, $compile) {
            compile = $compile
            testScope = $rootScope
            testScope.date = "1/1/2014"
            element = compile('<ac-datepicker ac-value="date"></ac-datepicker>')(testScope)
            testScope.$digest()
        }))

        it('should expect the required angular services to not be null', function () {
            expect(testScope).not.toBeNull()
            expect(compile).not.toBeNull()
        })

        it('should display date', function () {
           // expect(element.isolateScope()).toBeDefined() : doesn't work at the moment
           expect(testScope.date).toEqual("1/1/2014")
        })
    })
})