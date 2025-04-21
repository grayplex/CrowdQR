// CrowdQR API Postman Tests
// These test scripts should be added to each request in your Postman collection

// ------------------- User Controller Tests -------------------

// Get All Users
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array", function () {
    const responseJson = pm.response.json();
    pm.expect(Array.isArray(responseJson)).to.be.true;
});

// Store a user ID for later tests if available
if (pm.response.json().length > 0) {
    pm.collectionVariables.set("userId", pm.response.json()[0].userId);
}

// Get User By ID
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected user fields", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson).to.have.property('userId');
    pm.expect(responseJson).to.have.property('username');
    pm.expect(responseJson).to.have.property('role');
    pm.expect(responseJson).to.have.property('createdAt');
});

// Get Users By Role
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of users with the requested role", function () {
    const responseJson = pm.response.json();
    pm.expect(Array.isArray(responseJson)).to.be.true;

    if (responseJson.length > 0) {
        // Extract role from URL path
        const urlPathParts = pm.request.url.path;
        const roleIndex = urlPathParts.findIndex(part => part === "role") + 1;
        const roleValue = parseInt(urlPathParts[roleIndex]);

        pm.expect(responseJson[0].role).to.equal(roleValue);
    }
});

// Get User By Username
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected user fields with matching username", function () {
    const responseJson = pm.response.json();

    // Extract username from URL path
    const urlPathParts = pm.request.url.path;
    const usernameIndex = urlPathParts.findIndex(part => part === "username") + 1;
    const requestedUsername = urlPathParts[usernameIndex];

    pm.expect(responseJson).to.have.property('userId');
    pm.expect(responseJson).to.have.property('username');
    pm.expect(responseJson.username.toLowerCase()).to.equal(requestedUsername.toLowerCase());
});

// Create User
pm.test("Status code is 201 Created", function () {
    pm.response.to.have.status(201);
});

pm.test("Response contains the created user", function () {
    const responseJson = pm.response.json();
    const requestBody = JSON.parse(pm.request.body.raw);

    pm.expect(responseJson).to.have.property('userId');
    pm.expect(responseJson.username).to.equal(requestBody.username);
    pm.expect(responseJson.role).to.equal(requestBody.role);

    // Store the created user ID for later tests
    pm.collectionVariables.set("createdUserId", responseJson.userId);
});

// Update User
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// Delete User
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// ------------------- Event Controller Tests -------------------

// Get All Events
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array", function () {
    const responseJson = pm.response.json();
    pm.expect(Array.isArray(responseJson)).to.be.true;
});

// Store an event ID for later tests if available
if (pm.response.json().length > 0) {
    pm.collectionVariables.set("eventId", pm.response.json()[0].eventId);
}

// Get Event By ID
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected event fields", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson).to.have.property('eventId');
    pm.expect(responseJson).to.have.property('name');
    pm.expect(responseJson).to.have.property('slug');
    pm.expect(responseJson).to.have.property('isActive');
    pm.expect(responseJson).to.have.property('dj');
    pm.expect(responseJson.dj).to.have.property('userId');
    pm.expect(responseJson.dj).to.have.property('username');
});

// Get Event By Slug
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected event fields with matching slug", function () {
    const responseJson = pm.response.json();

    // Extract slug from URL path
    const urlPathParts = pm.request.url.path;
    const slugIndex = urlPathParts.findIndex(part => part === "slug") + 1;
    const requestedSlug = urlPathParts[slugIndex];

    pm.expect(responseJson).to.have.property('eventId');
    pm.expect(responseJson).to.have.property('name');
    pm.expect(responseJson).to.have.property('slug');
    pm.expect(responseJson.slug).to.equal(requestedSlug);
});

// Get Events By DJ
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of events with the requested DJ", function () {
    const responseJson = pm.response.json();

    // Extract DJ ID from URL path
    const urlPathParts = pm.request.url.path;
    const djIdIndex = urlPathParts.findIndex(part => part === "dj") + 1;
    const requestedDjId = parseInt(urlPathParts[djIdIndex]);

    pm.expect(Array.isArray(responseJson)).to.be.true;
    if (responseJson.length > 0) {
        pm.expect(responseJson[0].dj.userId).to.equal(requestedDjId);
    }
});

// Create Event
pm.test("Status code is 201 Created", function () {
    pm.response.to.have.status(201);
});

pm.test("Response contains the created event", function () {
    const responseJson = pm.response.json();
    const requestBody = JSON.parse(pm.request.body.raw);

    pm.expect(responseJson).to.have.property('eventId');
    pm.expect(responseJson.name).to.equal(requestBody.name);
    pm.expect(responseJson.slug).to.equal(requestBody.slug);
    pm.expect(responseJson.djUserId).to.equal(requestBody.djUserId);

    // Store the created event ID for later tests
    pm.collectionVariables.set("createdEventId", responseJson.eventId);
});

// Update Event
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// Delete Event
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// ------------------- Request Controller Tests -------------------

// Get All Requests
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array", function () {
    const responseJson = pm.response.json();
    pm.expect(Array.isArray(responseJson)).to.be.true;
});

// Store a request ID for later tests if available
if (pm.response.json().length > 0) {
    pm.collectionVariables.set("requestId", pm.response.json()[0].requestId);
}

// Get Request By ID
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected request fields", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson).to.have.property('requestId');
    pm.expect(responseJson).to.have.property('userId');
    pm.expect(responseJson).to.have.property('eventId');
    pm.expect(responseJson).to.have.property('songName');
    pm.expect(responseJson).to.have.property('status');
    pm.expect(responseJson).to.have.property('voteCount');
    pm.expect(responseJson).to.have.property('votes');
    pm.expect(Array.isArray(responseJson.votes)).to.be.true;
});

// Get Requests By Event
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of requests for the requested event", function () {
    const responseJson = pm.response.json();

    // Extract event ID from URL path
    const urlPathParts = pm.request.url.path;
    const eventIdIndex = urlPathParts.findIndex(part => part === "event") + 1;
    const requestedEventId = parseInt(urlPathParts[eventIdIndex]);

    pm.expect(Array.isArray(responseJson)).to.be.true;
    if (responseJson.length > 0) {
        pm.expect(responseJson[0].eventId).to.equal(requestedEventId);
    }
});

// Create Request
pm.test("Status code is 201 Created", function () {
    pm.response.to.have.status(201);
});

pm.test("Response contains the created request", function () {
    const responseJson = pm.response.json();
    const requestBody = JSON.parse(pm.request.body.raw);

    pm.expect(responseJson).to.have.property('requestId');
    pm.expect(responseJson.userId).to.equal(requestBody.userId);
    pm.expect(responseJson.eventId).to.equal(requestBody.eventId);
    pm.expect(responseJson.songName).to.equal(requestBody.songName);
    pm.expect(responseJson.status).to.equal(0); // Default status is Pending (0)

    // Store the created request ID for later tests
    pm.collectionVariables.set("createdRequestId", responseJson.requestId);
});

// Update Request Status
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// Delete Request
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// ------------------- Vote Controller Tests -------------------

// Get All Votes
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array", function () {
    const responseJson = pm.response.json();
    pm.expect(Array.isArray(responseJson)).to.be.true;
});

// Store a vote ID for later tests if available
if (pm.response.json().length > 0) {
    pm.collectionVariables.set("voteId", pm.response.json()[0].voteId);
}

// Get Vote By ID
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected vote fields", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson).to.have.property('voteId');
    pm.expect(responseJson).to.have.property('requestId');
    pm.expect(responseJson).to.have.property('userId');
    pm.expect(responseJson).to.have.property('createdAt');
});

// Get Votes By Request
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of votes for the requested request", function () {
    const responseJson = pm.response.json();

    // Extract request ID from URL path
    const urlPathParts = pm.request.url.path;
    const requestIdIndex = urlPathParts.findIndex(part => part === "request") + 1;
    const requestedRequestId = parseInt(urlPathParts[requestIdIndex]);

    pm.expect(Array.isArray(responseJson)).to.be.true;
    if (responseJson.length > 0) {
        pm.expect(responseJson[0].requestId).to.equal(requestedRequestId);
    }
});

// Create Vote
pm.test("Status code is 201 Created", function () {
    pm.response.to.have.status(201);
});

pm.test("Response contains the created vote", function () {
    const responseJson = pm.response.json();
    const requestBody = JSON.parse(pm.request.body.raw);

    pm.expect(responseJson).to.have.property('voteId');
    pm.expect(responseJson.requestId).to.equal(requestBody.requestId);
    pm.expect(responseJson.userId).to.equal(requestBody.userId);

    // Store the created vote ID for later tests
    pm.collectionVariables.set("createdVoteId", responseJson.voteId);
});

// Create Duplicate Vote (should fail)
pm.test("Status code is 409 Conflict for duplicate vote", function () {
    pm.response.to.have.status(409);
});

pm.test("Error message indicates duplicate vote", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson.detail).to.include("already voted");
});

// Delete Vote
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// Delete Vote By User And Request
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// ------------------- Session Controller Tests -------------------

// Get All Sessions
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array", function () {
    const responseJson = pm.response.json();
    pm.expect(Array.isArray(responseJson)).to.be.true;
});

// Store a session ID for later tests if available
if (pm.response.json().length > 0) {
    pm.collectionVariables.set("sessionId", pm.response.json()[0].sessionId);
}

// Get Session By ID
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected session fields", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson).to.have.property('sessionId');
    pm.expect(responseJson).to.have.property('user');
    pm.expect(responseJson.user).to.have.property('userId');
    pm.expect(responseJson.user).to.have.property('username');
    pm.expect(responseJson).to.have.property('event');
    pm.expect(responseJson.event).to.have.property('eventId');
    pm.expect(responseJson.event).to.have.property('name');
    pm.expect(responseJson).to.have.property('lastSeen');
    pm.expect(responseJson).to.have.property('requestCount');
});

// Get Sessions By Event
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of sessions for the requested event", function () {
    const responseJson = pm.response.json();

    // Extract event ID from URL path
    const urlPathParts = pm.request.url.path;
    const eventIdIndex = urlPathParts.findIndex(part => part === "event") + 1;
    const requestedEventId = parseInt(urlPathParts[eventIdIndex]);

    pm.expect(Array.isArray(responseJson)).to.be.true;
    if (responseJson.length > 0) {
        pm.expect(responseJson[0].eventId).to.equal(requestedEventId);
    }
});

// Get Sessions By User
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of sessions for the requested user", function () {
    const responseJson = pm.response.json();

    // Extract user ID from URL path
    const urlPathParts = pm.request.url.path;
    const userIdIndex = urlPathParts.findIndex(part => part === "user") + 1;
    const requestedUserId = parseInt(urlPathParts[userIdIndex]);

    pm.expect(Array.isArray(responseJson)).to.be.true;
    if (responseJson.length > 0) {
        pm.expect(responseJson[0].userId).to.equal(requestedUserId);
    }
});

// Get Session By Event And User
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains session for the requested event and user", function () {
    const responseJson = pm.response.json();

    // Extract event ID and user ID from URL path
    const urlPathParts = pm.request.url.path;
    const eventIdIndex = urlPathParts.findIndex(part => part === "event") + 1;
    const userIdIndex = urlPathParts.findIndex(part => part === "user") + 1;

    const requestedEventId = parseInt(urlPathParts[eventIdIndex]);
    const requestedUserId = parseInt(urlPathParts[userIdIndex]);

    pm.expect(responseJson.event.eventId).to.equal(requestedEventId);
    pm.expect(responseJson.user.userId).to.equal(requestedUserId);
});

// Create or Update Session
pm.test("Status code is 201 Created or 200 OK", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 201]);
});

pm.test("Response contains the session data", function () {
    const responseJson = pm.response.json();
    const requestBody = JSON.parse(pm.request.body.raw);

    pm.expect(responseJson).to.have.property('sessionId');
    pm.expect(responseJson.userId).to.equal(requestBody.userId);
    pm.expect(responseJson.eventId).to.equal(requestBody.eventId);

    // Store the created session ID for later tests
    pm.collectionVariables.set("createdSessionId", responseJson.sessionId);
});

// Increment Request Count
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// Delete Session
pm.test("Status code is 204 No Content", function () {
    pm.response.to.have.status(204);
});

// ------------------- Dashboard Controller Tests -------------------

// Get Event Summary
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response contains expected dashboard summary fields", function () {
    const responseJson = pm.response.json();
    pm.expect(responseJson).to.have.property('eventId');
    pm.expect(responseJson).to.have.property('totalRequests');
    pm.expect(responseJson).to.have.property('pendingRequests');
    pm.expect(responseJson).to.have.property('approvedRequests');
    pm.expect(responseJson).to.have.property('rejectedRequests');
    pm.expect(responseJson).to.have.property('totalVotes');
    pm.expect(responseJson).to.have.property('activeUsers');
    pm.expect(responseJson).to.have.property('topRequests');
    pm.expect(Array.isArray(responseJson.topRequests)).to.be.true;
    pm.expect(responseJson).to.have.property('recentlyApproved');
    pm.expect(Array.isArray(responseJson.recentlyApproved)).to.be.true;
    pm.expect(responseJson).to.have.property('recentlyRejected');
    pm.expect(Array.isArray(responseJson.recentlyRejected)).to.be.true;
    pm.expect(responseJson).to.have.property('activeUsersList');
    pm.expect(Array.isArray(responseJson.activeUsersList)).to.be.true;
});

// Get Top Requests
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of requests sorted by vote count", function () {
    const responseJson = pm.response.json();

    pm.expect(Array.isArray(responseJson)).to.be.true;

    // Check that the array is sorted by vote count (descending)
    if (responseJson.length > 1) {
        for (let i = 0; i < responseJson.length - 1; i++) {
            pm.expect(responseJson[i].voteCount >= responseJson[i + 1].voteCount).to.be.true;
        }
    }
});

// Get DJ Event Stats
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

pm.test("Response is an array of event statistics for the DJ", function () {
    const responseJson = pm.response.json();

    pm.expect(Array.isArray(responseJson)).to.be.true;
    if (responseJson.length > 0) {
        pm.expect(responseJson[0]).to.have.property('eventId');
        pm.expect(responseJson[0]).to.have.property('name');
        pm.expect(responseJson[0]).to.have.property('slug');
        pm.expect(responseJson[0]).to.have.property('isActive');
        pm.expect(responseJson[0]).to.have.property('requestCounts');
        pm.expect(responseJson[0].requestCounts).to.have.property('total');
        pm.expect(responseJson[0].requestCounts).to.have.property('pending');
        pm.expect(responseJson[0].requestCounts).to.have.property('approved');
        pm.expect(responseJson[0].requestCounts).to.have.property('rejected');
        pm.expect(responseJson[0]).to.have.property('totalVotes');
    }
});