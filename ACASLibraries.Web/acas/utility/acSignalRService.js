acas.module('acSignalRService', 'acas.utility.angular', 'jQuery', 'underscorejs', function () {
    acas.utility.angular.factory('acSignalRService', ['$interval', function ($interval) {
        return new function () {
            var utilities = {
                hubs: [],
                addHub: function (hubName, connection) {
                    var exists = this.findHub(hubName)
                    if (!exists) {
                        var newHub = {
                            name: hubName,
                            connection: (connection ? connection.connection : {}),
                            methods: (connection ? connection.server : {}),
                            listeners: (connection ? connection.client : {}),
                            pings: [],
                            _: (connection ? connection : {})
                        }
                        if (newHub.connection) {
                        	newHub.connection.disconnected(utilities.disconnectedHandler)
                        	newHub.connection.reconnecting(utilities.reconnectingHandler)
                        	newHub.connection.reconnected(utilities.reconnectedHandler)
                        	newHub.connection.connectionSlow(utilities.slowConnectionHandler)
                        }
                        if (newHub.connection.start) {
                            newHub.connection.start()
                        }
                        this.hubs.push(newHub)
                    }
                    this.updateHubNames()
                },
                getAvailableHubs: function () {
                	// check for jQuery signalr lib
                    if (jQuery.signalR) {
                        var connectionKeys = _.keys(jQuery.connection)
                        for (var i = 0; i < connectionKeys.length; i++) {
                            // the hubName property is indicative of the automatically 
                            // generated proxy script having already run
                            if (_.has(jQuery.connection[connectionKeys[i]], 'hubName')) {
                                this.addHub(jQuery.connection[connectionKeys[i]].hubName,
                                    jQuery.connection[connectionKeys[i]])
                            }
                        }
                    }
                },
                destroyHubs: function () {
                    for (var i = 0; i < utilities.hubs.length; i++) {
                        // stop the connection
                        this.endHubPings(utilities.hubs[i].name)
                        if (utilities.hubs[i].connection.stop) {
                            utilities.hubs[i].connection.stop()
                        }
                        utilities.hubs.splice(i, 1)
                    }
                },
                refreshHubs: function () {
                    this.destroyHubs()
                    this.getAvailableHubs()
                },
                findHub: function (hubName) {
                    return _.find(utilities.hubs, function (hub) { return hub.name === hubName })
                },
                invokeHubMethod: function (hubName, methodName, args) {
                    var hub = this.findHub(hubName)
                    if (hub && _.has(hub.methods, methodName)) {
                        hub.methods[methodName].apply(this, args)
                    }
                },
                invokeHubMethodPing: function (intervalMillis, hubName, methodName, args) {
                    var hub = this.findHub(hubName)
                    if (hub && _.has(hub.methods, methodName)) {
                        var p = new this.pingFactory()
                        p.startPingCallback(intervalMillis, hub.methods[methodName], args)
                        hub.pings.push({ method: methodName, pinger: p })
                    }
                },
                endHubPings: function (hubName) {
                    var hub = this.findHub(hubName)
                    if (hub) {
                        for (var i = 0; i < hub.pings.length; i++) {
                            hub.pings[i].pinger.endPing()
                            hub.pings.splice(i, 1)
                        }
                    }
                },
                endHubMethodPings: function (hubName, methodName) {
                    var hub = this.findHub(hubName)
                    if (hub && _.has(hub.methods, methodName)) {
                        for (var i = 0; i < hub.pings.length; i++) {
                            if (hub.pings[i].method === methodName) {
                                hub.pings[i].pinger.endPing()
                                hub.pings.splice(i, 1)
                            }
                        }
                    }
                },
                addHubListener: function (hubName, methodName, receiverFn) {
                    var hub = this.findHub(hubName)
                    if (hub && _.has(hub.methods, methodName)) {
                        hub.listeners[methodName] = receiverFn
                    }
                },
                updateHubNames: function () {
                    api.hubs = _.map(utilities.hubs, function (hub) {
                        return {
                            name: hub.name,
                            methods: _.map(
                                     _.keys(hub.methods),
	                        function (methodName) {
	                            return { name: methodName }
	                        })
                        }
                    })
                },
                pingFactory: function () {
                    var utilities = {
                        running: false,
                        startMillis: 1000,
                        currentMillis: 0,
                        intervalMillis: 10,
                        intervalObj: null,
                        granularity: 10,
                        iterations: 0,
                        callback: function () { },
                        args: [],
                        timerStartFn: function (startMillis, intervalMillis, callbackFn, args) {
                            this.startMillis = startMillis
                            this.intervalMillis = intervalMillis
                            if (callbackFn) {
                                this.callback = callbackFn
                                this.args = args
                            }
                            this.beginTimeout()
                        },
                        beginTimeout: function () {
                            var self = this
                            self.currentMillis = this.startMillis
                            self.running = true
                            // or setInterval
                            self.intervalObj = $interval(function () {
                                if (self.running === false) {
                                    // or clearInterval
                                    $interval.cancel(self.intervalObj)
                                    self.iterations = 0
                                }
                                if (self.currentMillis <= 0) {
                                    self.currentMillis = self.startMillis
                                    self.callback.apply(this, self.args)
                                }
                                self.currentMillis -= 1
                                self.iterations++
                            })
                        },
                        timerEndFn: function () {
                            this.running = false
                        }
                    }

                    var api = {
                        startPing: function (millis) {
                            if (!utilities.running) {
                                utilities.timerStartFn(millis, utilities.granularity)
                            }
                        },
                        startPingCallback: function (millis, callback, args) {
                            if (!utilities.running) {
                                utilities.timerStartFn(millis, utilities.granularity, callback, args)
                            }
                        },
                        endPing: function () {
                            if (utilities.running) {
                                utilities.timerEndFn()
                            }
                        },
                        value: function () {
                            return utilities.currentMillis
                        },
                        zeroIterations: function() {
                        	utilities.iterations = 0
                        }
                    }
                    return api
                },
                disconnectedHandler: function() {
                	// this is an error case, attempt reconnect
                	var p = new pingFactory()
                	if (jQuery.signalR) {
	                	p.startPingCallback(5000, function() {
	                		var anyDisconnected = false
	                		for (var i = 0; i < this.hubs.length; i++) {
	                			if (this.hubs[i].connection.state === 
	                				jQuery.signalR.connectionState.disconnected) {
	                				this.hubs[i].connection.start()
	                				anyDisconnected = true
	                			}
	                		}
	                		if (!anyDisconnected) {
	                			p.endPing()
	                		}
	                	}, [])
                	}
                },
                slowConnectionHandler: function() {
                	// implement slow connection handling
                	alert('Connection is slow') // please replace!
                },
                reconnectingHandler: function() {
                	// implement the reconnecting handler
                	alert('Reconnecting') // please replace!
                },
                reconnectedHandler: function() {
                	// implement the reconnected handler
                	alert('Reconnected') // please replace!
                },
                initialize: function () {
                    this.getAvailableHubs()
                    // test functions, always initialize after refreshing the hubs list
                    /*
                        this.addHubListener('echohub', 'echo', function (data) { console.log('eccchhhhoooooooo: ', data) })
                        this.invokeHubMethodPing(500, 'echohub', 'echo', ['blah'])
                    */
                }
            }

            var api = {
                hubs: [],
                // in case you loaded this module after SignalR, a call to this may be required
                // also good in case you want to attempt reconnection
                refresh: function () {
                    utilities.refreshHubs()
                },
                invokeHubMethod: function (hubName, methodName, args) {
                    utilities.invokeHubMethod(hubName, methodName, args)
                },
                invokeHubMethodPing: function (intervalMillis, hubName, methodName, args) {
                    utilities.invokeHubMethodPing(intervalMillis, hubName, methodName, args)
                },
                endHubMethodPings: function (hubName, methodName) {
                    utilities.endHubMethodPings(hubName, methodName)
                },
                addHubListener: function (hubName, methodName, receiverFn) {
                    utilities.addHubListener(hubName, methodName, receiverFn)
                },
                // stop all live connections with the server
                destroy: function () {
                    utilities.destroyHubs()
                }
            }
            utilities.initialize()
            return api
        }
    }])
})