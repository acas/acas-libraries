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
                        if (newHub.connection.start) {
                            newHub.connection.start()
                        }
                        this.hubs.push(newHub)
                    }
                    this.updateHubNames()
                },
                getCurrentHubs: function () {
                    if (jQuery.connection) {
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
                destroyCurrentHubs: function () {
                    for (var i = 0; i < utilities.hubs.length; i++) {
                        // stop the connection
                        this.endAllHubPings(utilities.hubs[i].name)
                        if (utilities.hubs[i].connection.stop) {
                            utilities.hubs[i].connection.stop()
                        }
                        utilities.hubs.splice(i, 1)
                    }
                },
                refreshCurrentHubs: function () {
                    this.destroyCurrentHubs()
                    this.getCurrentHubs()
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
                invokeHubMethodPing: function (hubName, methodName, intervalMillis, args) {
                    var hub = this.findHub(hubName)
                    if (hub && _.has(hub.methods, methodName)) {
                        var ps = new this.pingFactory()
                        ps.startPingCallback(intervalMillis, hub.methods[methodName], args)
                        hub.pings.push({ method: methodName, pinger: ps })
                    }
                },
                endAllHubPings: function (hubName) {
                    var hub = this.findHub(hubName)
                    if (hub) {
                        for (var i = 0; i < hub.pings.length; i++) {
                            hub.pings[i].pinger.endPing()
                            hub.pings.splice(i, 1)
                        }
                    }
                },
                endAllHubMethodPings: function (hubName, methodName) {
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
                                }
                                if (self.currentMillis <= 0) {
                                    self.currentMillis = self.startMillis
                                    self.callback.apply(this, self.args)
                                }
                                self.currentMillis -= 1
                            })
                        },
                        timerEndFn: function () {
                            this.running = false
                        }
                    }

                    var api = {
                        startPing: function (millis) {
                            if (!utilities.running) {
                                utilities.timerStartFn(millis, 10)
                            }
                        },
                        startPingCallback: function (millis, callback, args) {
                            if (!utilities.running) {
                                utilities.timerStartFn(millis, 10, callback, args)
                            }
                        },
                        endPing: function () {
                            if (utilities.running) {
                                utilities.timerEndFn()
                            }
                        },
                        value: function () {
                            return utilities.currentMillis
                        }
                    }
                    return api
                },
                initialize: function () {
                    this.getCurrentHubs()
                    // test functions, always initialize after refreshing the hubs list
                    /*
                        this.addHubListener('echohub', 'echo', function (data) { console.log('eccchhhhoooooooo: ', data) })
                        this.invokeHubMethodPing('echohub', 'echo', 500, ['blah'])
                    */
                }
            }

            var api = {
                // hubsInternal: utilities.hubs, -- debug, only names should be externally exposed
                hubs: [],
                // use at your own risk (no hubs may be available when trying to add one)
                addHub: function (hubName) {
                    utilities.addHub(hubName)
                },
                // in case you loaded this module after SignalR, this may be required
                refresh: function () {
                    utilities.refreshCurrentHubs()
                },
                invokeHubMethod: function (hubName, methodName, args) {
                    utilities.invokeHubMethod(hubName, methodName, args)
                },
                invokeHubMethodPing: function (hubName, methodName, intervalMillis, args) {
                    utilities.invokeHubMethodPing(hubName, methodName, intervalMillis, args)
                },
                endAllHubMethodPings: function (hubName, methodName) {
                    utilities.endAllHubMethodPings(hubName, methodName)
                },
                addHubListener: function (hubName, methodName, receiverFn) {
                    utilities.addHubListener(hubName, methodName, receiverFn)
                },
                // stop all live connections with the server
                destroyAllHubs: function () {
                    utilities.destroyCurrentHubs()
                }
            }
            utilities.initialize()
            return api
        }
    }])
})