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
    let reconnectInterval = null;
    let eventHandlers = {};

    // Initialize the connection
    function initConnection(apiBaseUrl) {
        if (!apiBaseUrl) {
            console.error("API base URL is required");
            return false;
        }

        try {
            // Build the SignalR connection
            connection = new signalR.HubConnectionBuilder()
                .withUrl(`${apiBaseUrl}/hubs/crowdqr`)
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Retry policy
                .configureLogging(signalR.LogLevel.Information)
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
                startReconnectTimer();
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
    async function startConnection() {
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
            clearReconnectTimer();

            return true;
        } catch (error) {
            console.error("SignalR connection error:", error);
            isConnected = false;
            triggerEvent('connectionStatus', { status: 'error', error });

            // Set up reconnect timer
            startReconnectTimer();

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

    // Set up a reconnect timer
    function startReconnectTimer() {
        clearReconnectTimer();

        reconnectInterval = setInterval(() => {
            if (!isConnected) {
                console.log("Attempting to reconnect to SignalR hub...");
                startConnection().catch(console.error);
            } else {
                clearReconnectTimer();
            }
        }, 5000); // Try every 5 seconds
    }

    // Clear the reconnect timer
    function clearReconnectTimer() {
        if (reconnectInterval) {
            clearInterval(reconnectInterval);
            reconnectInterval = null;
        }
    }

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

    // Public API
    return {
        init: initConnection,
        start: startConnection,
        joinEvent: joinEvent,
        leaveEvent: leaveEvent,
        on: on,
        getConnection: () => connection,
        isConnected: () => isConnected,
        getCurrentEventId: () => eventId
    };
})();