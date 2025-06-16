// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// src/CrowdQR.Web/wwwroot/js/site.js - Add this code

// Loading indicator management
document.addEventListener('DOMContentLoaded', function () {
    // Show loading indicator when a form is submitted
    document.querySelectorAll('form:not(.no-loading)').forEach(form => {
        // Exclude vote forms and request forms from showing global loading indicators
        if (form.classList.contains('vote-form') || form.id === 'request-form') {
            form.classList.add('no-loading');
        }

        form.addEventListener('submit', function () {
            // Don't show loading for forms with the no-loading class
            if (!this.classList.contains('no-loading')) {
                showLoading();
            }
        });
    });

    // Show loading when clicking on navigation links (except those with no-loading class)
    document.querySelectorAll('a:not(.no-loading)').forEach(link => {
        if (link.getAttribute('href') && !link.getAttribute('href').startsWith('#') &&
            !/^(javascript:|data:|vbscript:)/i.test(link.getAttribute('href')) &&
            !link.getAttribute('target')) {
            link.addEventListener('click', function () {
                showLoading();
            });
        }
    });
});

function showLoading(message = 'Loading...') {
    // Don't show loading indicators for certain form submissions
    if (document.activeElement &&
        (document.activeElement.closest('.vote-form') ||
            document.activeElement.closest('#request-form'))) {
        console.log("Skipping loading indicator for vote or request form");
        return;
    }

    let loadingDiv = document.querySelector('#global-loading');

    if (!loadingDiv) {
        loadingDiv = document.createElement('div');
        loadingDiv.id = 'global-loading';
        loadingDiv.className = 'position-fixed top-0 start-0 w-100 h-100 d-flex justify-content-center align-items-center bg-white bg-opacity-75';
        loadingDiv.style.zIndex = 9999;
        loadingDiv.innerHTML = `
            <div class="text-center">
                <div class="spinner-border text-primary mb-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div class="loading-message">${message}</div>
            </div>
        `;
        document.body.appendChild(loadingDiv);
    } else {
        loadingDiv.querySelector('.loading-message').textContent = message;
        loadingDiv.style.display = 'flex';
    }

    // Automatically hide after a timeout to prevent stuck loaders
    setTimeout(hideLoading, 10000);
}

function hideAllLoadingIndicators() {
    const loadingDiv = document.querySelector('#global-loading');
    if (loadingDiv) {
        loadingDiv.style.display = 'none';
    }
}

function hideLoading() {
    const loadingDiv = document.querySelector('#global-loading');
    if (loadingDiv) {
        loadingDiv.style.display = 'none';
    }
}

// Show or hide loading indicator programmatically
window.CrowdQR = window.CrowdQR || {};
window.CrowdQR.Loading = {
    show: showLoading,
    hide: hideLoading
};

// Automatically hide loading when page finishes loading
window.addEventListener('load', hideLoading);

window.addEventListener('DOMContentLoaded', function () {
    // Hide loading indicators after a short delay
    setTimeout(hideAllLoadingIndicators, 3000);
});