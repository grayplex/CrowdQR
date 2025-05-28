# CrowdQR E2E Testing Checklists

## Overview

This document outlines comprehensive End-to-End (E2E) test scenarios for the CrowdQR application. These tests validate the complete user journey from audience member interaction to DJ management.

## Test Environment Setup Checklist

### Prerequisites

- [ ] Docker and Docker Compose installed
- [ ] Application running via `docker-compose up`
- [ ] API accessible at `http://localhost:5000`
- [ ] Web app accessible at `http://localhost:8080`
- [ ] Database properly seeded with test data
- [ ] Multiple browsers/devices available for testing
- [ ] Network connectivity stable

### Test Data Preparation

- [ ] Test DJ account created (username: `test_dj`, email: `dj@test.com`)
- [ ] Test event created with slug `test-event`
- [ ] QR code generated for event access
- [ ] Test audience usernames prepared (`audience1`, `audience2`, etc.)

---

## Core User Journey Test Scenarios

### 1. DJ Authentication & Setup

#### 1.1 DJ Registration

- [ ] Navigate to DJ registration page
- [ ] Enter valid registration details:
  - [ ] Username: `test_dj_new`
  - [ ] Email: `newdj@test.com`
  - [ ] Password: `TestPassword123!`
  - [ ] Confirm Password: `TestPassword123!`
- [ ] Submit registration form
- [ ] Verify success message displayed
- [ ] Verify email verification notice (if enabled)
- [ ] Check database for user creation

#### 1.2 DJ Login

- [ ] Navigate to DJ login page
- [ ] Enter valid credentials:
  - [ ] Username: `test_dj`
  - [ ] Password: `TestPassword123!`
- [ ] Submit login form
- [ ] Verify successful redirect to DJ dashboard
- [ ] Verify authentication state (user menu shows DJ name)
- [ ] Check browser storage for auth tokens

#### 1.3 Event Creation

- [ ] Access event creation form from DJ dashboard
- [ ] Enter event details:
  - [ ] Name: `Test Live Event`
  - [ ] Slug: `test-live-event`
  - [ ] Status: Active
- [ ] Submit event creation form
- [ ] Verify event appears in DJ dashboard
- [ ] Verify QR code is generated
- [ ] Test QR code accessibility

### 2. Audience Interaction Flow

#### 2.1 Audience Access via QR Code

- [ ] Scan QR code or navigate to event URL directly
- [ ] Enter audience username (e.g., `audience_user_1`)
- [ ] Verify successful entry to event interface
- [ ] Verify event name displays correctly
- [ ] Check session creation in browser storage

#### 2.2 Song Request Submission

- [ ] Access request submission form
- [ ] Enter song details:
  - [ ] Song Name: `Test Song Title`
  - [ ] Artist Name: `Test Artist` (optional)
- [ ] Submit request
- [ ] Verify request appears in request list
- [ ] Verify request status shows as "Pending"
- [ ] Check real-time update via SignalR

#### 2.3 Voting on Requests

- [ ] View existing requests from other users
- [ ] Click vote button on a request
- [ ] Verify vote count increments
- [ ] Verify vote button state changes (disabled/checked)
- [ ] Try voting on same request again (should be prevented)
- [ ] Vote on different request (should succeed)
- [ ] Remove vote and verify count decrements

### 3. Real-Time Updates

#### 3.1 SignalR Connection

- [ ] Open browser developer tools > Network tab
- [ ] Verify SignalR connection established
- [ ] Monitor for SignalR messages during interactions
- [ ] Test connection resilience (briefly disconnect network)

#### 3.2 Multi-User Real-Time Sync

- [ ] Open event in multiple browser windows/tabs
- [ ] Submit request in one window
- [ ] Verify request appears in other windows without refresh
- [ ] Vote on request in one window
- [ ] Verify vote count updates in other windows
- [ ] Test with multiple audience members simultaneously

### 4. DJ Dashboard Management

#### 4.1 Request Management Interface

- [ ] Access DJ dashboard
- [ ] Navigate to event management
- [ ] Verify all pending requests are visible
- [ ] Verify vote counts display correctly
- [ ] Verify requester names are shown
- [ ] Check request timestamps

#### 4.2 Request Approval Workflow

- [ ] Select a pending request
- [ ] Click "Approve" button
- [ ] Verify request status changes to "Approved"
- [ ] Verify status update appears in audience view
- [ ] Check real-time notification sent via SignalR

#### 4.3 Request Rejection Workflow

- [ ] Select a pending request
- [ ] Click "Reject" button
- [ ] Verify request status changes to "Rejected"
- [ ] Verify status update appears in audience view
- [ ] Check that rejected requests are handled appropriately

#### 4.4 Dashboard Analytics

- [ ] Verify total request count displays correctly
- [ ] Check active user count
- [ ] Verify vote statistics are accurate
- [ ] Test dashboard refresh and data persistence

---

## Edge Cases & Error Scenarios

### 5. Input Validation & Security

#### 5.1 Malicious Input Testing

- [ ] Submit request with XSS payload in song name
- [ ] Submit request with SQL injection patterns
- [ ] Submit extremely long song/artist names (>255 chars)
- [ ] Submit empty/whitespace-only requests
- [ ] Test special characters in all input fields

#### 5.2 Authentication & Authorization

- [ ] Try accessing DJ dashboard without login
- [ ] Try accessing other DJ's events
- [ ] Test token expiration handling
- [ ] Verify audience users can't access admin functions
- [ ] Test concurrent login sessions

#### 5.3 Rate Limiting & Spam Prevention

- [ ] Submit multiple requests rapidly from same user
- [ ] Vote on multiple requests quickly
- [ ] Test duplicate request submission prevention
- [ ] Verify vote limits per user per request

### 6. Performance & Reliability

#### 6.1 Load Testing (Manual)

- [ ] Open 10+ browser tabs as different audience members
- [ ] Submit requests simultaneously
- [ ] Vote on requests concurrently
- [ ] Monitor response times and system stability
- [ ] Check for memory leaks or connection issues

#### 6.2 Network Resilience

- [ ] Test with slow network connection
- [ ] Simulate network interruption during request submission
- [ ] Test reconnection after SignalR disconnect
- [ ] Verify offline handling and error messages

### 7. Cross-Platform Compatibility

#### 7.1 Browser Testing

- [ ] Chrome (latest version)
- [ ] Firefox (latest version)
- [ ] Safari (if available)
- [ ] Edge (latest version)
- [ ] Mobile Chrome (Android)
- [ ] Mobile Safari (iOS)

#### 7.2 Device Testing

- [ ] Desktop computer (1920x1080)
- [ ] Laptop (1366x768)
- [ ] Tablet (iPad or Android)
- [ ] Mobile phone (various screen sizes)
- [ ] Test touch interactions on mobile devices

#### 7.3 Responsive Design

- [ ] Verify layouts adapt to different screen sizes
- [ ] Test QR code scanning on mobile devices
- [ ] Verify form usability on small screens
- [ ] Check button sizes for touch interaction

---

## Data Integrity & Persistence

### 8. Database Operations

#### 8.1 Data Consistency

- [ ] Submit request and verify database entry
- [ ] Vote on request and check vote table
- [ ] Approve request and verify status update
- [ ] Check foreign key relationships maintained
- [ ] Verify cascade deletes work correctly

#### 8.2 Session Management

- [ ] Create audience session and verify database record
- [ ] Test session timeout handling
- [ ] Verify session cleanup on browser close
- [ ] Check session request count tracking

### 9. API Endpoint Testing

#### 9.1 Direct API Calls (via Postman/curl)

- [ ] POST `/api/auth/login` with valid credentials
- [ ] GET `/api/event/slug/{slug}` for event details
- [ ] POST `/api/request` to create song request
- [ ] POST `/api/vote` to vote on request
- [ ] PUT `/api/request/{id}/status` to approve/reject
- [ ] Verify all responses return correct HTTP status codes
- [ ] Check response body formats match expected DTOs

#### 9.2 API Error Handling

- [ ] Test invalid authentication tokens
- [ ] Submit malformed JSON payloads
- [ ] Test non-existent resource requests (404s)
- [ ] Verify error messages are user-friendly
- [ ] Check that sensitive information isn't leaked in errors

---

## Regression Testing Checklist

### 10. Previous Bug Fixes Verification

- [ ] Verify vote count accuracy doesn't drift
- [ ] Confirm duplicate vote prevention works
- [ ] Test request submission validation
- [ ] Verify SignalR connection stability
- [ ] Check authentication state persistence
- [ ] Confirm role-based access control

---

## Test Completion Criteria

### Success Criteria

- [ ] All critical scenarios pass without issues
- [ ] Real-time updates work reliably across devices
- [ ] No data loss or corruption observed
- [ ] Performance remains acceptable under load
- [ ] Security validations pass
- [ ] Cross-browser compatibility confirmed

### Documentation Requirements

- [ ] All test results documented with screenshots
- [ ] Bugs/issues logged with reproduction steps
- [ ] Performance observations recorded
- [ ] Enhancement suggestions compiled
- [ ] Test environment details documented

---

## Notes Section

### Test Environment Details

- **Date of Testing**: ___________
- **Tester Name**: ___________
- **Application Version**: ___________
- **Browser Versions Tested**: ___________
- **Devices Used**: ___________

### Issues Discovered

| Issue # | Severity | Description | Reproduction Steps | Status |
|---------|----------|-------------|-------------------|--------|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |

### Enhancement Suggestions

1.
2.
3.

### Performance Observations

- Average request submission time: ___________
- SignalR message delivery time: ___________
- Dashboard load time: ___________
- Concurrent user capacity: ___________
