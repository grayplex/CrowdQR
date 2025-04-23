window.CrowdQR = window.CrowdQR || {};

// Helper for making authenticated AJAX requests
window.CrowdQR.Ajax = (function () {
    // Get the anti-forgery token
    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    }

    // Make an authenticated AJAX request
    function sendRequest(url, method, data, successCallback, errorCallback) {
        // Create headers object with authentication token if available
        const headers = {};

        // Add anti-forgery token if it exists
        const antiForgeryToken = getAntiForgeryToken();
        if (antiForgeryToken) {
            headers['RequestVerificationToken'] = antiForgeryToken;
        }

        // Make the AJAX request
        $.ajax({
            url: url,
            type: method,
            data: data,
            headers: headers,
            xhrFields: {
                withCredentials: true // This is crucial, sends cookies with the request
            },
            success: successCallback,
            error: errorCallback
        });
    }

    return {
        post: function (url, data, successCallback, errorCallback) {
            sendRequest(url, 'POST', data, successCallback, errorCallback);
        },
        get: function (url, successCallback, errorCallback) {
            sendRequest(url, 'GET', null, successCallback, errorCallback);
        }
    };
})();