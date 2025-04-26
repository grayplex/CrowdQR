window.CrowdQR = window.CrowdQR || {};

// Helper for making authenticated AJAX requests
window.CrowdQR.Ajax = (function () {
    // Get the anti-forgery token
    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    }

    // Make an authenticated AJAX request
    function sendRequest(url, method, data, successCallback, errorCallback, showGlobalLoading = false) {
        // Create headers object with authentication token if available
        const headers = {};

        // Add anti-forgery token if it exists
        const antiForgeryToken = getAntiForgeryToken();
        if (antiForgeryToken) {
            headers['RequestVerificationToken'] = antiForgeryToken;
        }

        // Show global loading if requested
        if (showGlobalLoading && window.CrowdQR.Loading) {
            window.CrowdQR.Loading.show();
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
            success: function (response) {
                // Hide global loading if it was shown
                if (showGlobalLoading && window.CrowdQR.Loading) {
                    window.CrowdQR.Loading.hide();
                }

                // Call success callback
                if (typeof successCallback === 'function') {
                    successCallback(response);
                }
            },
            error: function (xhr, status, error) {
                // Hide global loading if it was shown
                if (showGlobalLoading && window.CrowdQR.Loading) {
                    window.CrowdQR.Loading.hide();
                }

                // Call error callback
                if (typeof errorCallback === 'function') {
                    errorCallback(xhr, status, error);
                }
            }
        });
    }

    return {
        post: function (url, data, successCallback, errorCallback, showGlobalLoading = false) {
            sendRequest(url, 'POST', data, successCallback, errorCallback, showGlobalLoading);
        },
        get: function (url, successCallback, errorCallback, showGlobalLoading = false) {
            sendRequest(url, 'GET', null, successCallback, errorCallback, showGlobalLoading);
        },
        put: function (url, data, successCallback, errorCallback, showGlobalLoading = false) {
            sendRequest(url, 'PUT', data, successCallback, errorCallback, showGlobalLoading);
        },
        delete: function (url, successCallback, errorCallback, showGlobalLoading = false) {
            sendRequest(url, 'DELETE', null, successCallback, errorCallback, showGlobalLoading);
        }
    };
})();