// Study Materials Upload Enhancement
(function () {
    'use strict';

    // File size validation (client-side)
    const maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    /**
     * Validate file size and extension
     */
    function validateFileSize(input) {
        if (input.files && input.files[0]) {
            const file = input.files[0];

            if (file.size > maxFileSizeBytes) {
                alert('File is too large. Maximum size is 10 MB.');
                input.value = '';
                return false;
            }

            // Check file extension
            if (!file.name.toLowerCase().endsWith('.md')) {
                alert('Please select a .md (Markdown) file.');
                input.value = '';
                return false;
            }
        }
        return true;
    }

    /**
     * Show loading spinner on button
     */
    function showLoadingState(button) {
        if (button) {
            button.disabled = true;
            const originalText = button.innerHTML;
            button.setAttribute('data-original-text', originalText);
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Uploading...';
        }
    }

    /**
     * Initialize page functionality
     */
    function initializePage() {
        // Attach validation to file inputs
        const termsFile = document.getElementById('termsFile');
        const equationsFile = document.getElementById('equationsFile');

        if (termsFile) {
            termsFile.addEventListener('change', function () {
                validateFileSize(this);
            });
        }

        if (equationsFile) {
            equationsFile.addEventListener('change', function () {
                validateFileSize(this);
            });
        }

        // Show loading spinner on form submit
        const uploadForms = document.querySelectorAll('form[enctype="multipart/form-data"]');
        uploadForms.forEach(form => {
            form.addEventListener('submit', function (e) {
                const fileInput = this.querySelector('input[type="file"]');

                // Validate file is selected
                if (fileInput && (!fileInput.files || fileInput.files.length === 0)) {
                    e.preventDefault();
                    alert('Please select a file to upload.');
                    return false;
                }

                // Validate file size one more time
                if (fileInput && fileInput.files && fileInput.files[0]) {
                    if (fileInput.files[0].size > maxFileSizeBytes) {
                        e.preventDefault();
                        alert('File is too large. Maximum size is 10 MB.');
                        return false;
                    }
                }

                // Show loading state
                const btn = this.querySelector('button[type="submit"]');
                showLoadingState(btn);
            });
        });

        // Auto-dismiss alerts after 5 seconds
        const alerts = document.querySelectorAll('.alert');
        alerts.forEach(alert => {
            if (alert.classList.contains('alert-dismissible')) {
                setTimeout(() => {
                    const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                    bsAlert.close();
                }, 5000);
            }
        });

        // Confirm delete modals accessibility
        const deleteModals = document.querySelectorAll('.modal');
        deleteModals.forEach(modal => {
            modal.addEventListener('shown.bs.modal', function () {
                const firstButton = this.querySelector('.btn');
                if (firstButton) {
                    firstButton.focus();
                }
            });
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializePage);
    } else {
        initializePage();
    }
})();
