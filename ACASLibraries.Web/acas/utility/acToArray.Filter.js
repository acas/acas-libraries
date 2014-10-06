acas.module('acToArray', 'acas.utility.angular', function () {
	acas.utility.angular
		.filter('acToArray', function () {
			//for ng-repeat to work over an object, format it this way. 
			//Optionally pass in the name of a property to use as the sort order
			//this is pretty inefficient, probably. It's best to just ng-repeat over an array, whenever possible
			return function (input, orderBy) {
				return acas.utility.array.objectToArray(input, orderBy)
			}
		})
})
