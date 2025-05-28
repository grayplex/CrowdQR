# Core E2E Test Script: Login → Request → Vote → Admin Approval

## Test Overview

This script provides step-by-step instructions for executing the critical user journey in CrowdQR: DJ login, audience request submission, voting, and admin approval workflow.

## Prerequisites

- CrowdQR application running via Docker Compose
- API available at `http://localhost:5000`
- Web app available at `http://localhost:8080`
- At least 2 different browsers or devices for multi-user testing

---

## Phase 1: DJ Setup and Authentication

### Step 1.1: DJ Login

**Objective**: Authenticate as DJ and access admin dashboard

1. **Open Browser 1** (designated as DJ browser)
2. **Navigate to**: `http://localhost:8080/login`
3. **Enter Credentials**:
   - Username: `test_dj`
   - Password: `TestPassword123!`
4. **Click**: "Login" button
5. **Verify**: Successful redirect to DJ dashboard
6. **Screenshot**: Save as `01_dj_login_success.png`

**Expected Results**:

- ✅ Login successful without errors
- ✅ Dashboard displays user name/role
- ✅ Navigation shows DJ-specific options

### Step 1.2: Event Setup

**Objective**: Create or verify test event exists

1. **In DJ Dashboard**, navigate to Events section
2. **Check for existing event** with slug `test-event`
3. **If event doesn't exist**:
   - Click "Create New Event"
   - Name: `E2E Test Event`
   - Slug: `test-event`
   - Status: Active
   - Click "Create"
4. **Note the Event URL**: `http://localhost:8080/event/test-event`
5. **Screenshot**: Save as `02_event_setup.png`

**Expected Results**:

- ✅ Event created successfully or existing event found
- ✅ Event shows as "Active"
- ✅ Event URL is accessible

---

## Phase 2: Audience Member Flow

### Step 2.1: Audience Access

**Objective**: Join event as audience member

1. **Open Browser 2** (designated as Audience Browser 1)
2. **Navigate to**: `http://localhost:8080/event/test-event`
3. **Enter Username**: `audience_tester_1`
4. **Click**: "Join Event" button
5. **Verify**: Successful entry to event interface
6. **Screenshot**: Save as `03_audience_join.png`

**Expected Results**:

- ✅ Event page loads correctly
- ✅ Event name displays prominently
- ✅ Request submission form is visible
- ✅ Current requests list is visible (may be empty)

### Step 2.2: Submit Song Request

**Objective**: Create a new song request

1. **In Audience Browser 1**, locate request submission form
2. **Enter Song Details**:
   - Song Name: `Bohemian Rhapsody`
   - Artist Name: `Queen`
3. **Click**: "Submit Request" button
4. **Verify**: Request appears in the requests list
5. **Note**: Request status should be "Pending"
6. **Screenshot**: Save as `04_request_submitted.png`

**Expected Results**:

- ✅ Request submitted without errors
- ✅ Request appears in list immediately
- ✅ Shows correct song/artist information
- ✅ Status shows as "Pending"
- ✅ Vote count starts at 0

### Step 2.3: Second Audience Member

**Objective**: Add another audience member for voting

1. **Open Browser 3** (or Incognito/Private window)
2. **Navigate to**: `http://localhost:8080/event/test-event`
3. **Enter Username**: `audience_tester_2`
4. **Click**: "Join Event"
5. **Verify**: Can see the request submitted by audience_tester_1
6. **Screenshot**: Save as `05_second_audience_join.png`

**Expected Results**:

- ✅ Second user joins successfully
- ✅ Can see existing requests from other users
- ✅ Real-time sync working (request visible without refresh)

---

## Phase 3: Voting Workflow

### Step 3.1: Vote on Request

**Objective**: Test voting functionality

1. **In Browser 3** (audience_tester_2), find the "Bohemian Rhapsody" request
2. **Click**: Vote button/icon for that request
3. **Verify**: Vote count increases to 1
4. **Verify**: Vote button changes state (disabled/highlighted)
5. **Screenshot**: Save as `06_vote_cast.png`

**Expected Results**:

- ✅ Vote count increments immediately
- ✅ Vote button visual state changes
- ✅ User cannot vote again on same request

### Step 3.2: Real-Time Vote Updates

**Objective**: Verify real-time synchronization

1. **Switch to Browser 2** (audience_tester_1)
2. **Verify**: Vote count shows as 1 without page refresh
3. **Try to vote** on your own request
4. **Submit another request**:
   - Song Name: `Hotel California`
   - Artist Name: `Eagles`
5. **Switch to Browser 3** (audience_tester_2)
6. **Verify**: New request appears automatically
7. **Vote on the new request**
8. **Screenshot**: Save as `07_realtime_updates.png`

**Expected Results**:

- ✅ Vote updates appear in real-time across browsers
- ✅ New requests appear automatically
- ✅ Vote counts sync correctly
- ✅ No page refresh required

---

## Phase 4: DJ Admin Approval Workflow

### Step 4.1: DJ Dashboard Review

**Objective**: Review pending requests in admin interface

1. **Switch to Browser 1** (DJ Dashboard)
2. **Navigate to Event Management** or refresh dashboard
3. **Verify**: Both submitted requests are visible
4. **Check**: Vote counts match what audience sees
5. **Note**: Request details and timestamps
6. **Screenshot**: Save as `08_dj_dashboard_requests.png`

**Expected Results**:

- ✅ All pending requests visible to DJ
- ✅ Vote counts accurate
- ✅ Requester usernames shown
- ✅ Request timestamps present

### Step 4.2: Approve First Request

**Objective**: Test request approval workflow

1. **In DJ Dashboard**, locate "Bohemian Rhapsody" request
2. **Click**: "Approve" button
3. **Verify**: Request status changes to "Approved"
4. **Check**: Request moves to approved section or updates visually
5. **Screenshot**: Save as `09_request_approved.png`

**Expected Results**:

- ✅ Status updates immediately in DJ interface
- ✅ Request marked as approved clearly

### Step 4.3: Audience Sees Approval

**Objective**: Verify real-time status updates to audience

1. **Switch to Browser 2** (audience_tester_1)
2. **Verify**: "Bohemian Rhapsody" status shows as "Approved"
3. **Switch to Browser 3** (audience_tester_2)  
4. **Verify**: Same status update visible
5. **Screenshot**: Save as `10_audience_sees_approval.png`

**Expected Results**:

- ✅ Status change appears in real-time for all audience members
- ✅ Visual indication of approval clear
- ✅ No page refresh required

### Step 4.4: Reject Second Request

**Objective**: Test request rejection workflow

1. **Switch back to Browser 1** (DJ Dashboard)
2. **Locate**: "Hotel California" request
3. **Click**: "Reject" button
4. **Verify**: Request status changes to "Rejected"
5. **Switch to Audience Browsers**
6. **Verify**: Rejection visible to both audience members
7. **Screenshot**: Save as `11_request_rejected.png`

**Expected Results**:

- ✅ Rejection processed correctly
- ✅ Status updates in real-time for audience
- ✅ Clear visual indication of rejection

---

## Phase 5: Additional Validation Tests

### Step 5.1: Vote Restrictions

**Objective**: Verify voting restrictions work correctly

1. **In Browser 2** (audience_tester_1), try to vote on "Bohemian Rhapsody" again
2. **Verify**: Cannot vote twice on same request
3. **Try voting** on the rejected "Hotel California" request
4. **Note**: Behavior for voting on rejected requests

**Expected Results**:

- ✅ Duplicate voting prevented
- ✅ Appropriate feedback given to user

### Step 5.2: New Request After Approval

**Objective**: Test continued functionality

1. **In Browser 2**, submit a new request:
   - Song Name: `Sweet Child O' Mine`
   - Artist Name: `Guns N' Roses`
2. **In Browser 3**, vote on this new request
3. **Verify**: All functionality still works after previous approvals/rejections
4. **Screenshot**: Save as `12_continued_functionality.png`

**Expected Results**:

- ✅ New requests can be submitted after approvals
- ✅ Voting continues to work normally
- ✅ System remains stable

---

## Phase 6: Session Management Testing

### Step 6.1: Logout and Re-access

**Objective**: Test session persistence and cleanup

1. **In DJ Browser**, logout from dashboard
2. **Try to access event management** without login
3. **Verify**: Redirected to login page
4. **Re-login** with DJ credentials
5. **Verify**: Can access dashboard and see all requests

**Expected Results**:

- ✅ Logout works correctly
- ✅ Unauthorized access prevented
- ✅ Re-login restores full functionality

### Step 6.2: Audience Session Persistence

**Objective**: Test audience session handling

1. **In Browser 2**, refresh the page
2. **Verify**: Still shows as audience_tester_1
3. **Close browser** and reopen to event URL
4. **Note**: Whether username is remembered or re-entry required

**Expected Results**:

- ✅ Session persistence works as designed
- ✅ User experience remains smooth

---

## Test Completion Checklist

### Critical Path Verification

- [ ] DJ can login successfully
- [ ] DJ can access event management dashboard  
- [ ] Audience members can join events
- [ ] Song requests can be submitted
- [ ] Voting system works correctly
- [ ] Real-time updates function across all users
- [ ] DJs can approve requests
- [ ] DJs can reject requests
- [ ] Status changes sync in real-time
- [ ] Vote restrictions are enforced

### Performance Observations

- Request submission response time: ________
- Vote registration time: ________
- Real-time update latency: ________
- Dashboard load time: ________

### Issues Discovered

| Issue | Severity | Description | Browser | Reproduction Steps |
|-------|----------|-------------|---------|-------------------|
| | | | | |
| | | | | |

### Screenshots Captured

- [ ] 01_dj_login_success.png
- [ ] 02_event_setup.png
- [ ] 03_audience_join.png
- [ ] 04_request_submitted.png
- [ ] 05_second_audience_join.png
- [ ] 06_vote_cast.png
- [ ] 07_realtime_updates.png
- [ ] 08_dj_dashboard_requests.png
- [ ] 09_request_approved.png
- [ ] 10_audience_sees_approval.png
- [ ] 11_request_rejected.png
- [ ] 12_continued_functionality.png

### Overall Test Result

- [ ] **PASS** - All critical functionality works as expected
- [ ] **PASS WITH ISSUES** - Core functionality works but issues noted
- [ ] **FAIL** - Critical functionality broken

### Notes

_Use this space to record any additional observations, unexpected behaviors, or suggestions for improvement._

---

**Test Completed By**: ________________  
**Date**: ________________  
**Time**: ________________  
**Environment**: ________________
