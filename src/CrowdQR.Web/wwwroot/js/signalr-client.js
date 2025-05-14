/**
 * SignalR client for CrowdQR application
 * Handles real-time communication between frontend and backend
 */

// Define the CrowdQR namespace if it doesn't exist
window.CrowdQR = window.CrowdQR || {};

// SignalR client module
window.CrowdQR.SignalR = (function () {
    // Private variables
    let connection = null;
    let eventId = null;
    let isConnected = false;
    let eventHandlers = {};

    // Initialize the connection
    function init(apiBaseUrl) {
        if (!apiBaseUrl) {
            console.error("API base URL is required");
            return false;
        }

        console.log("Connecting to SignalR hub at:", apiBaseUrl + "/hubs/crowdqr");

        try {
            // Build the SignalR connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl(`${apiBaseUrl}/hubs/crowdqr`, {
                    skipNegotiation: false,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                //.withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Debug)
                .build();

            // Set up connection event handlers
            connection.onreconnecting(error => {
                console.warn("SignalR reconnecting:", error);
                isConnected = false;
                triggerEvent('connectionStatus', { status: 'reconnecting' });
            });

            connection.onreconnected(connectionId => {
                console.log("SignalR reconnected:", connectionId);
                isConnected = true;
                triggerEvent('connectionStatus', { status: 'connected' });

                // Rejoin event group if was previously in one
                if (eventId) {
                    joinEvent(eventId).catch(console.error);
                }
            });

            connection.onclose(error => {
                console.warn("SignalR connection closed:", error);
                isConnected = false;
                triggerEvent('connectionStatus', { status: 'disconnected' });

                // Set up a manual reconnect if automatic reconnection fails
            });

            // Register standard event handlers
            registerStandardEventHandlers();

            return true;
        } catch (error) {
            console.error("Error initializing SignalR connection:", error);
            return false;
        }
    }

    // Start the connection
    async function start() {
        if (!connection) {
            console.error("Connection not initialized");
            return false;
        }

        if (connection.state === signalR.HubConnectionState.Connected) {
            console.log("SignalR already connected");
            return true;
        }

        try {
            await connection.start();
            console.log("SignalR connected");
            isConnected = true;
            triggerEvent('connectionStatus', { status: 'connected' });

            // Clear any reconnect timer since we're connected
            //clearReconnectTimer();

            return true;
        } catch (error) {
            console.error("SignalR connection error:", error);
            isConnected = false;
            triggerEvent('connectionStatus', { status: 'error', error });

            // Set up reconnect timer
            // startReconnectTimer();

            return false;
        }
    }

    // Join an event group to receive updates for that event
    async function joinEvent(id) {
        if (!isConnected) {
            console.warn("Not connected to SignalR hub");
            return false;
        }

        try {
            eventId = id;
            await connection.invoke('JoinEvent', parseInt(id));
            console.log(`Joined event ${id}`);
            triggerEvent('eventJoined', { eventId: id });
            return true;
        } catch (error) {
            console.error(`Error joining event ${id}:`, error);
            return false;
        }
    }

    // Leave an event group
    async function leaveEvent() {
        if (!isConnected || !eventId) {
            console.warn("Not connected or not in an event");
            return false;
        }

        try {
            const id = eventId;
            eventId = null;
            await connection.invoke('LeaveEvent', parseInt(id));
            console.log(`Left event ${id}`);
            triggerEvent('eventLeft', { eventId: id });
            return true;
        } catch (error) {
            console.error(`Error leaving event ${eventId}:`, error);
            return false;
        }
    }

    // Set up a reconnect timer with exponential backoff
    /*
    function startReconnectTimer() {
        clearReconnectTimer();

        // Start with a base delay of 2 seconds, then increase exponentially
        let retryCount = 0;
        const maxRetryCount = 10; // Maximum number of retries
        const baseDelay = 2000; // 2 seconds
        const maxDelay = 60000; // Maximum delay of 1 minute

        reconnectInterval = setInterval(() => {
            if (!isConnected) {
                if (retryCount < maxRetryCount) {
                    // Calculate exponential backoff delay (2s, 4s, 8s, 16s, etc.)
                    const delay = Math.min(maxDelay, baseDelay * Math.pow(2, retryCount));

                    console.log(`Attempting to reconnect to SignalR hub (attempt ${retryCount + 1}/${maxRetryCount})...`);

                    startConnection()
                        .then(success => {
                            if (success) {
                                retryCount = 0; // Reset retry count on successful connection
                                clearReconnectTimer();
                            } else {
                                retryCount++; // Increment retry count on failed attempt
                            }
                        })
                        .catch(() => {
                            retryCount++; // Increment retry count on error
                        });
                } else {
                    console.error("Maximum reconnection attempts reached. Please refresh the page.");
                    clearReconnectTimer();
                    triggerEvent('connectionStatus', {
                        status: 'failed',
                        error: 'Maximum reconnection attempts reached'
                    });
                }
            } else {
                clearReconnectTimer();
            }
        }, 5000); // Check connection status every 5 seconds
    }

    // Clear the reconnect timer
    function clearReconnectTimer() {
        if (reconnectInterval) {
            clearInterval(reconnectInterval);
            reconnectInterval = null;
        }
    }
    */

    // Register standard event handlers for CrowdQR events
    function registerStandardEventHandlers() {
        // Request events
        connection.on('requestAdded', data => {
            console.log('Request added:', data);
            triggerEvent('requestAdded', data);
        });

        connection.on('requestStatusUpdated', data => {
            console.log('Request status updated:', data);
            triggerEvent('requestStatusUpdated', data);
        });

        // Vote events
        connection.on('voteAdded', data => {
            console.log('Vote added:', data);
            triggerEvent('voteAdded', data);
        });

        connection.on('voteRemoved', data => {
            console.log('Vote removed:', data);
            triggerEvent('voteRemoved', data);
        });

        // User events
        connection.on('userJoinedEvent', data => {
            console.log('User joined event:', data);
            triggerEvent('userJoinedEvent', data);
        });

        connection.on('userLeftEvent', data => {
            console.log('User left event:', data);
            triggerEvent('userLeftEvent', data);
        })
    }

    // Register event handlers
    function on(event, callback) {
        if (typeof callback !== 'function') {
            console.error(`Invalid callback for event ${event}`);
            return;
        }

        eventHandlers[event] = eventHandlers[event] || [];
        eventHandlers[event].push(callback);
    }

    // Trigger an event
    function triggerEvent(event, data) {
        if (!eventHandlers[event]) return;

        for (const callback of eventHandlers[event]) {
            try {
                callback(data);
            } catch (error) {
                console.error(`Error in ${event} event handler:`, error);
            }
        }
    }

    // Attempt connection with fallback to alternate transport methods
    /*
    async function attemptConnectionWithFallback(apiBaseUrl) {
        // Try WebSockets first (default)
        try {
            console.log("Attempting to connect with WebSockets...");
            connection = new signalR.HubConnectionBuilder()
                .withUrl(`${apiBaseUrl}/hubs/crowdqr`, {
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            await setupConnectionHandlers();
            await connection.start();
            console.log("Connected successfully using WebSockets");
            isConnected = true;
            triggerEvent('connectionStatus', { status: 'connected' });
            return true;
        } catch (wsError) {
            console.warn("WebSocket connection failed:", wsError);

            // Try SSE (Server-Sent Events) as fallback
            try {
                console.log("Attempting to connect with Server-Sent Events...");
                connection = new signalR.HubConnectionBuilder()
                    .withUrl(`${apiBaseUrl}/hubs/crowdqr`, {
                        transport: signalR.HttpTransportType.ServerSentEvents
                    })
                    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                    .configureLogging(signalR.LogLevel.Information)
                    .build();

                await setupConnectionHandlers();
                await connection.start();
                console.log("Connected successfully using Server-Sent Events");
                isConnected = true;
                triggerEvent('connectionStatus', { status: 'connected' });
                return true;
            } catch (sseError) {
                console.warn("Server-Sent Events connection failed:", sseError);

                // Finally, try Long Polling as last resort
                try {
                    console.log("Attempting to connect with Long Polling...");
                    connection = new signalR.HubConnectionBuilder()
                        .withUrl(`${apiBaseUrl}/hubs/crowdqr`, {
                            transport: signalR.HttpTransportType.LongPolling
                        })
                        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                        .configureLogging(signalR.LogLevel.Information)
                        .build();

                    await setupConnectionHandlers();
                    await connection.start();
                    console.log("Connected successfully using Long Polling");
                    isConnected = true;
                    triggerEvent('connectionStatus', { status: 'connected' });
                    return true;
                } catch (lpError) {
                    console.error("All connection methods failed:", lpError);
                    isConnected = false;
                    triggerEvent('connectionStatus', { status: 'failed' });
                    return false;
                }
            }
        }
    }

    // Set up the connection event handlers
    async function setupConnectionHandlers() {
        // Set up connection event handlers
        connection.onreconnecting(error => {
            console.warn("SignalR reconnecting:", error);
            isConnected = false;
            triggerEvent('connectionStatus', { status: 'reconnecting' });
        });

        connection.onreconnected(connectionId => {
            console.log("SignalR reconnected:", connectionId);
            isConnected = true;
            triggerEvent('connectionStatus', { status: 'connected' });

            // Rejoin event group if was previously in one
            if (eventId) {
                joinEvent(eventId).catch(console.error);
            }
        });

        connection.onclose(error => {
            console.warn("SignalR connection closed:", error);
            isConnected = false;
            triggerEvent('connectionStatus', { status: 'disconnected' });

            // Set up a manual reconnect if automatic reconnection fails
            startReconnectTimer();
        });

        // Register standard event handlers
        registerStandardEventHandlers();
    }
    */

    // Check the health of the connection
    function checkHealth() {
        return isConnected;
        /*
        if (!isConnected || !connection) {
            return false;
        }

        try {
            // We'll try to invoke a simple ping method on the server
            // If it throws an exception, the connection is unhealthy
            await connection.invoke('Ping');
            return true;
        } catch (error) {
            console.warn('Connection health check failed:', error);
            return false;
        }
        */
    }

    // Reconnect to the server
    async function reconnect() {
        if (isConnected) {
            // Already connected
            return true;
        }

        // Try to stop the connection first if it's in a strange state
        try {
            await connection.stop();
        } catch (stopError) {
            // Ignore errors when stopping
        }

        return await startConnection();
    }


    // Measure connection latency
    async function measureLatency() {
        if (!isConnected || !connection) {
            return null;
        }

        try {
            const start = performance.now();
            await connection.invoke('Ping');
            const end = performance.now();

            const roundTripTime = Math.round(end - start);
            return {
                roundTripTime,
                timestamp: Date.now()
            };
        } catch (error) {
            console.warn('Latency measurement failed:', error);
            return null;
        }
    }

    // Public API
    return {
        init,
        start,
        joinEvent,
        leaveEvent,
        on,
        isConnected: () => isConnected,
        getCurrentEventId: () => eventId,
        checkHealth,
        reconnect,
        measureLatency
        //attemptConnectionWithFallback
    };
})();