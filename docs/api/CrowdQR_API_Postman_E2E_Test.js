// CrowdQR API End-to-End Test Flow
// This script demonstrates a complete workflow testing all major features

// -----------------------------
// Step 1: Create DJ and Audience Users
// -----------------------------

// Create DJ User
pm.sendRequest({
    url: pm.variables.get("baseUrl") + "/api/user",
    method: 'POST',
    header: {
        'Content-Type': 'application/json'
    },
    body: {
        mode: 'raw',
        raw: JSON.stringify({
            "username": "test_dj_" + Date.now(),
            "role": 1 // DJ role
        })
    }
}, function (err, res) {
    if (err) {
        console.error(err);
    } else {
        const djUser = res.json();
        console.log("Created DJ User:", djUser);
        
        // Store DJ user ID for later use
        pm.collectionVariables.set("e2e_djUserId", djUser.userId);
        
        // Now create audience user
        createAudienceUser();
    }
});

// Create Audience User
function createAudienceUser() {
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/user",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "username": "test_audience_" + Date.now(),
                "role": 0 // Audience role
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const audienceUser = res.json();
            console.log("Created Audience User:", audienceUser);
            
            // Store audience user ID for later use
            pm.collectionVariables.set("e2e_audienceUserId", audienceUser.userId);
            
            // Continue to next step - create event
            createEvent();
        }
    });
}

// -----------------------------
// Step 2: Create an Event
// -----------------------------

function createEvent() {
    const djUserId = pm.collectionVariables.get("e2e_djUserId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/event",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "djUserId": parseInt(djUserId),
                "name": "Test Event " + Date.now(),
                "slug": "test-event-" + Date.now(),
                "isActive": true
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const event = res.json();
            console.log("Created Event:", event);
            
            // Store event ID for later use
            pm.collectionVariables.set("e2e_eventId", event.eventId);
            
            // Continue to next step - create user session
            createSession();
        }
    });
}

// -----------------------------
// Step 3: Create User Session
// -----------------------------

function createSession() {
    const audienceUserId = pm.collectionVariables.get("e2e_audienceUserId");
    const eventId = pm.collectionVariables.get("e2e_eventId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/session",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "userId": parseInt(audienceUserId),
                "eventId": parseInt(eventId),
                "clientIP": "192.168.1.100"
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const session = res.json();
            console.log("Created Session:", session);
            
            // Store session ID for later use
            pm.collectionVariables.set("e2e_sessionId", session.sessionId);
            
            // Continue to next step - create song requests
            createSongRequest1();
        }
    });
}

// -----------------------------
// Step 4: Create Multiple Song Requests
// -----------------------------

function createSongRequest1() {
    const audienceUserId = pm.collectionVariables.get("e2e_audienceUserId");
    const eventId = pm.collectionVariables.get("e2e_eventId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/request",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "userId": parseInt(audienceUserId),
                "eventId": parseInt(eventId),
                "songName": "Test Song 1",
                "artistName": "Test Artist 1"
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const request1 = res.json();
            console.log("Created Song Request 1:", request1);
            
            // Store request ID for later use
            pm.collectionVariables.set("e2e_requestId1", request1.requestId);
            
            // Create a second song request
            createSongRequest2();
        }
    });
}

function createSongRequest2() {
    const audienceUserId = pm.collectionVariables.get("e2e_audienceUserId");
    const eventId = pm.collectionVariables.get("e2e_eventId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/request",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "userId": parseInt(audienceUserId),
                "eventId": parseInt(eventId),
                "songName": "Test Song 2",
                "artistName": "Test Artist 2"
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const request2 = res.json();
            console.log("Created Song Request 2:", request2);
            
            // Store request ID for later use
            pm.collectionVariables.set("e2e_requestId2", request2.requestId);
            
            // Create an additional user to cast votes
            createAdditionalUser();
        }
    });
}

// -----------------------------
// Step 5: Create Additional User for Voting
// -----------------------------

function createAdditionalUser() {
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/user",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "username": "test_voter_" + Date.now(),
                "role": 0 // Audience role
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const voterUser = res.json();
            console.log("Created Voter User:", voterUser);
            
            // Store voter user ID for later use
            pm.collectionVariables.set("e2e_voterUserId", voterUser.userId);
            
            // Continue to next step - cast votes
            castVote1();
        }
    });
}

// -----------------------------
// Step 6: Cast Votes on Requests
// -----------------------------

function castVote1() {
    const audienceUserId = pm.collectionVariables.get("e2e_audienceUserId");
    const requestId1 = pm.collectionVariables.get("e2e_requestId1");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/vote",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "userId": parseInt(audienceUserId),
                "requestId": parseInt(requestId1)
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const vote1 = res.json();
            console.log("Cast Vote 1:", vote1);
            
            // Store vote ID for later use
            pm.collectionVariables.set("e2e_voteId1", vote1.voteId);
            
            // Cast another vote
            castVote2();
        }
    });
}

function castVote2() {
    const voterUserId = pm.collectionVariables.get("e2e_voterUserId");
    const requestId1 = pm.collectionVariables.get("e2e_requestId1");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/vote",
        method: 'POST',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "userId": parseInt(voterUserId),
                "requestId": parseInt(requestId1)
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const vote2 = res.json();
            console.log("Cast Vote 2:", vote2);
            
            // Store vote ID for later use
            pm.collectionVariables.set("e2e_voteId2", vote2.voteId);
            
            // Update request status (DJ approves a request)
            approveRequest();
        }
    });
}

// -----------------------------
// Step 7: DJ Approves/Rejects Requests
// -----------------------------

function approveRequest() {
    const requestId1 = pm.collectionVariables.get("e2e_requestId1");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/request/" + requestId1 + "/status",
        method: 'PUT',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "status": 1 // Approved
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            console.log("Approved Request 1");
            
            // Reject the second request
            rejectRequest();
        }
    });
}

function rejectRequest() {
    const requestId2 = pm.collectionVariables.get("e2e_requestId2");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/request/" + requestId2 + "/status",
        method: 'PUT',
        header: {
            'Content-Type': 'application/json'
        },
        body: {
            mode: 'raw',
            raw: JSON.stringify({
                "status": 2 // Rejected
            })
        }
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            console.log("Rejected Request 2");
            
            // Update session request count
            incrementRequestCount();
        }
    });
}

// -----------------------------
// Step 8: Update Session Request Count
// -----------------------------

function incrementRequestCount() {
    const sessionId = pm.collectionVariables.get("e2e_sessionId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/session/" + sessionId + "/increment-request-count",
        method: 'PUT'
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            console.log("Incremented Request Count for Session");
            
            // Get event summary
            getEventSummary();
        }
    });
}

// -----------------------------
// Step 9: Get Dashboard Information
// -----------------------------

function getEventSummary() {
    const eventId = pm.collectionVariables.get("e2e_eventId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/dashboard/event/" + eventId + "/summary",
        method: 'GET'
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const summary = res.json();
            console.log("Event Summary:", summary);
            
            // Get top requests
            getTopRequests();
        }
    });
}

function getTopRequests() {
    const eventId = pm.collectionVariables.get("e2e_eventId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/dashboard/event/" + eventId + "/top-requests?status=0&count=10",
        method: 'GET'
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const topRequests = res.json();
            console.log("Top Requests:", topRequests);
            
            // Get DJ event stats
            getDJEventStats();
        }
    });
}

function getDJEventStats() {
    const djUserId = pm.collectionVariables.get("e2e_djUserId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/dashboard/dj/" + djUserId + "/event-stats",
        method: 'GET'
    }, function (err, res) {
        if (err) {
            console.error(err);
        } else {
            const eventStats = res.json();
            console.log("DJ Event Stats:", eventStats);
            
            // Verify results and clean up if needed
            verifyResults();
        }
    });
}

// -----------------------------
// Step 10: Verify Results and Clean Up
// -----------------------------

function verifyResults() {
    // Run assertions on the data stored in collection variables
    
    // Get the event to verify it's active
    const eventId = pm.collectionVariables.get("e2e_eventId");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/event/" + eventId,
        method: 'GET'
    }, function (err, res) {
        if (err) {
            console.error(err);
            pm.test("E2E Test Failed: Could not verify event", function() {
                pm.expect(false).to.be.true;
            });
        } else {
            const event = res.json();
            
            // Run assertions
            pm.test("E2E Test - Event is active", function() {
                pm.expect(event.isActive).to.be.true;
            });
            
            pm.test("E2E Test - Event has correct DJ", function() {
                const djUserId = pm.collectionVariables.get("e2e_djUserId");
                pm.expect(event.dj.userId.toString()).to.equal(djUserId);
            });
            
            // Get the request to verify status
            getRequestForVerification();
        }
    });
}

function getRequestForVerification() {
    const requestId1 = pm.collectionVariables.get("e2e_requestId1");
    
    pm.sendRequest({
        url: pm.variables.get("baseUrl") + "/api/request/" + requestId1,
        method: 'GET'
    }, function (err, res) {
        if (err) {
            console.error(err);
            pm.test("E2E Test Failed: Could not verify request", function() {
                pm.expect(false).to.be.true;
            });
        } else {
            const request = res.json();
            
            // Run assertions
            pm.test("E2E Test - Request 1 is approved", function() {
                pm.expect(request.status).to.equal("Approved");
            });
            
            pm.test("E2E Test - Request 1 has votes", function() {
                pm.expect(request.voteCount).to.be.at.least(2);
            });
            
            console.log("E2E Test completed successfully!");
        }
    });
}