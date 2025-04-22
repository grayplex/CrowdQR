// Session management
let sessionRefreshInterval = null;

function setupSessionRefresh(intervalMinutes = 5) {
    // Clear any existing interval
    if (sessionRefreshInterval) {
        clearInterval(sessionRefreshInterval);
    }

    // Set up new interval to refresh the session
    const intervalMs = intervalMinutes * 60 * 1000;
    sessionRefreshInterval = setInterval(refreshSession, intervalMs);

    // Also refresh when user interacts with the page
    document.addEventListener('click', debounce(refreshSession, 5000));
    document.addEventListener('keypress', debounce(refreshSession, 5000));
}

function refreshSession() {
    // Call the session refresh endpoint
    fetch('/api/session/refresh', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    }).catch(error => {
        console.warn('Failed to refresh session:', error);
    });
}

// Utility function to limit the frequency of session refreshes
function debounce(func, wait) {
    let timeout;
    return function () {
        const context = this;
        const args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(() => {
            func.apply(context, args);
        }, wait);
    };
}

// Initialize session refresh if the user is logged in
document.addEventListener('DOMContentLoaded', function () {
    if (document.body.dataset.userLoggedIn === 'true') {
        setupSessionRefresh();
    }
});