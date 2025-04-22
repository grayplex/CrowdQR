// Enhanced form validation
document.addEventListener('DOMContentLoaded', function () {
    // Add custom validation for all forms
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function (event) {
            if (!this.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }

            this.classList.add('was-validated');

            // Highlight the first invalid field
            const firstInvalid = this.querySelector(':invalid');
            if (firstInvalid) {
                firstInvalid.focus();

                // Scroll to the first invalid field with some offset
                setTimeout(() => {
                    window.scrollTo({
                        top: firstInvalid.getBoundingClientRect().top + window.pageYOffset - 100,
                        behavior: 'smooth'
                    });
                }, 100);
            }
        }, false);
    });

    // Add pattern validation for song name field
    const songNameInputs = document.querySelectorAll('input[name="NewSongRequest.SongName"]');
    songNameInputs.forEach(input => {
        if (!input.hasAttribute('pattern')) {
            input.setAttribute('pattern', '.{2,255}');
            input.setAttribute('title', 'Song name must be between 2 and 255 characters');
        }
    });
});