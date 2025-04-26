/**
 * Request handler module for managing song requests and votes
 * Provides functionality for real-time request handling
 */

window.CrowdQR = window.CrowdQR || {};

// Request handler module
window.CrowdQR.RequestHandler = (function () {
    // Private variables
    let currentEventId = null;
    let currentUserId = null;
    let requests = [];
    let pendingRequestCallback = null;

    // Initialize the request handler
    function init(eventId, userId) {
        currentEventId = eventId;
        currentUserId = userId;

        // Listen for SignalR events
        listenForSignalREvents();

        console.log(`Request handler initialized for event ${eventId} and user ${userId}`);
        return true;
    }

    // Listen for SignalR events
    function listenForSignalREvents() {
        if (!window.CrowdQR.SignalR) {
            console.error("SignalR client is not available");
            return false;
        }

        // Listen for request added event
        window.CrowdQR.SignalR.on('requestAdded', handleRequestAdded);

        // Listen for request status updated event
        window.CrowdQR.SignalR.on('requestStatusUpdated', handleRequestStatusUpdated);

        // Listen for vote added event
        window.CrowdQR.SignalR.on('voteAdded', handleVoteAdded);

        // Listen for vote removed event
        window.CrowdQR.SignalR.on('voteRemoved', handleVoteRemoved);

        return true;
    }

    // Handle request added event
    function handleRequestAdded(data) {
        console.log('Request added:', data);

        // Check if this is for our current event
        if (data.eventId != currentEventId) {
            return;
        }

        // If there's a pending request callback, call it
        if (pendingRequestCallback) {
            pendingRequestCallback(data.requestId);
            pendingRequestCallback = null;
        }
    }

    // Handle request status updated event
    function handleRequestStatusUpdated(data) {
        console.log('Request status updated:', data);

        // Check if this is for our current event
        if (data.eventId != currentEventId) {
            return;
        }

        // Find the request in our local cache and update its status
        const request = findRequestById(data.requestId);
        if (request) {
            request.status = data.status;
        }
    }

    // Handle vote added event
    function handleVoteAdded(data) {
        console.log('Vote added:', data);

        // Check if this is for our current event
        if (data.eventId != currentEventId) {
            return;
        }

        // Find the request in our local cache and update its vote count
        const request = findRequestById(data.requestId);
        if (request) {
            request.voteCount = data.voteCount;

            // If this is the current user's vote, update userHasVoted
            if (data.userId == currentUserId) {
                request.userHasVoted = true;
            }
        }
    }

    // Handle vote removed event
    function handleVoteRemoved(data) {
        console.log('Vote removed:', data);

        // Check if this is for our current event
        if (data.eventId != currentEventId) {
            return;
        }

        // Find the request in our local cache and update its vote count
        const request = findRequestById(data.requestId);
        if (request) {
            request.voteCount = data.voteCount;

            // If this is the current user's vote, update userHasVoted
            if (data.userId == currentUserId) {
                request.userHasVoted = false;
            }
        }
    }

    // Find a request by ID in the local cache
    function findRequestById(requestId) {
        return requests.find(r => r.requestId == requestId);
    }

    // Submit a new song request
    async function submitRequest(songName, artistName, notes) {
        if (!currentEventId || !currentUserId) {
            console.error("Request handler not initialized properly");
            return { success: false, error: "Not initialized" };
        }

        try {
            // Create request data
            const requestData = {
                eventId: currentEventId,
                userId: currentUserId,
                songName: songName,
                artistName: artistName,
                notes: notes
            };

            // Return a promise that will be resolved when the request is added
            return new Promise((resolve, reject) => {
                // Set up the callback for when the request is added
                pendingRequestCallback = (requestId) => {
                    resolve({
                        success: true,
                        requestId: requestId
                    });
                };

                // Set a timeout in case the SignalR message doesn't come through
                setTimeout(() => {
                    if (pendingRequestCallback) {
                        pendingRequestCallback = null;
                        resolve({
                            success: true,
                            requestId: null,
                            message: "Request was submitted, but real-time update failed"
                        });
                    }
                }, 5000);

                // Submit the request using AJAX
                $.ajax({
                    url: '/Event',
                    type: 'POST',
                    data: {
                        'NewSongRequest.SongName': songName,
                        'NewSongRequest.ArtistName': artistName,
                        'NewSongRequest.Notes': notes,
                        'slug': currentEventSlug
                    },
                    error: function (xhr, status, error) {
                        pendingRequestCallback = null;
                        reject({
                            success: false,
                            error: error
                        });
                    }
                });
            });
        } catch (error) {
            console.error("Error submitting request:", error);
            return { success: false, error: error.message };
        }
    }

    // Submit a vote for a request
    async function submitVote(requestId) {
        if (!currentEventId || !currentUserId) {
            console.error("Request handler not initialized properly");
            return { success: false, error: "Not initialized" };
        }

        try {
            // Send the vote request using AJAX
            return new Promise((resolve, reject) => {
                $.ajax({
                    url: '/Event?handler=Vote',
                    type: 'POST',
                    data: {
                        requestId: requestId,
                        slug: currentEventSlug
                    },
                    headers: {
                        RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function () {
                        resolve({ success: true });
                    },
                    error: function (xhr, status, error) {
                        reject({ success: false, error: error });
                    }
                });
            });
        } catch (error) {
            console.error("Error submitting vote:", error);
            return { success: false, error: error.message };
        }
    }

    // Remove a vote from a request
    async function removeVote(requestId) {
        if (!currentEventId || !currentUserId) {
            console.error("Request handler not initialized properly");
            return { success: false, error: "Not initialized" };
        }

        try {
            // Send the remove vote request using AJAX
            return new Promise((resolve, reject) => {
                $.ajax({
                    url: '/Event?handler=RemoveVote',
                    type: 'POST',
                    data: {
                        requestId: requestId,
                        slug: currentEventSlug
                    },
                    headers: {
                        RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function () {
                        resolve({ success: true });
                    },
                    error: function (xhr, status, error) {
                        reject({ success: false, error: error });
                    }
                });
            });
        } catch (error) {
            console.error("Error removing vote:", error);
            return { success: false, error: error.message };
        }
    }

    // Set the current event slug
    let currentEventSlug = null;
    function setEventSlug(slug) {
        currentEventSlug = slug;
    }

    // Public API
    return {
        init: init,
        submitRequest: submitRequest,
        submitVote: submitVote,
        removeVote: removeVote,
        setEventSlug: setEventSlug
    };
})();