/**
 * Exercise Feature JavaScript
 * Study Helper - Exercise Module with LaTeX support
 * Provides progressive enhancement for exercise forms and result pages
 */

(function () {
    'use strict';

    // ===== Declare variables in outer scope for cross-feature access =====
    let form, input, submitBtn, submitText, spinner, isRatio, maxValue, validateInput;

    // ===== Form Validation and Submission =====
    form = document.getElementById('exerciseForm');
    if (form) {
        input = document.getElementById('userAnswer');
        submitBtn = document.getElementById('submitBtn');
        submitText = submitBtn.querySelector('.submit-text');
        spinner = submitBtn.querySelector('.spinner-border');

        // Check if this is a ratio problem
        isRatio = input.dataset.isRatio === 'true';
        maxValue = isRatio ? 100 : 999999999.99;

        // Initially disable submit button
        submitBtn.disabled = true;

        /**
         * Validates input value based on problem type
         */
        validateInput = function() {
            const value = parseFloat(input.value);
            const isValid = input.value && 
                           !isNaN(value) && 
                           value >= 0 && 
                           value <= maxValue;

            submitBtn.disabled = !isValid;

            // Clear custom validation message
            input.setCustomValidity('');

            return isValid;
        };

        // Real-time validation on input
        input.addEventListener('input', validateInput);

        // Format on blur
        input.addEventListener('blur', function () {
            if (input.value) {
                const value = parseFloat(input.value);

                if (!isNaN(value) && value >= 0) {
                    // Format to 2 decimal places
                    input.value = value.toFixed(2);
                }

                // Validate range
                if (value < 0 || value > maxValue) {
                    const message = isRatio 
                        ? 'Please enter a value between 0 and 100'
                        : 'Please enter a value between $0 and $999,999,999.99';
                    input.setCustomValidity(message);
                    input.reportValidity();
                }
            }
        });

        // Handle form submission
        form.addEventListener('submit', function (e) {
            const value = parseFloat(input.value);

            // Final validation before submit
            if (!input.value || isNaN(value) || value < 0 || value > maxValue) {
                e.preventDefault();
                const message = isRatio
                    ? 'Please enter a valid ratio between 0 and 100'
                    : 'Please enter a valid dollar amount between $0 and $999,999,999.99';
                input.setCustomValidity(message);
                input.reportValidity();
                return false;
            }

            // Show loading state
            submitBtn.disabled = true;
            submitBtn.classList.add('loading');
            submitText.textContent = 'Submitting...';
            spinner.classList.remove('d-none');
        });

        // Auto-focus input on page load
        setTimeout(function () {
            input.focus();
        }, 100);

        // Handle browser back button (restore form state)
        window.addEventListener('pageshow', function (event) {
            if (event.persisted) {
                // Page was restored from cache
                submitBtn.disabled = false;
                submitBtn.classList.remove('loading');
                submitText.textContent = 'Submit Answer';
                spinner.classList.add('d-none');
            }
        });

        // Prevent negative values
        input.addEventListener('keypress', function (e) {
            if (e.key === '-') {
                e.preventDefault();
            }
        });

        // Prevent pasting negative values
        input.addEventListener('paste', function (e) {
            setTimeout(function () {
                const value = parseFloat(input.value);
                if (value < 0) {
                    input.value = Math.abs(value).toFixed(2);
                    validateInput();
                }
            }, 0);
        });
    }

    // ===== Keyboard Shortcuts =====
    document.addEventListener('keydown', function (e) {
        // Ctrl/Cmd + Enter to submit form
        if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
            if (form && !submitBtn.disabled) {
                e.preventDefault();
                form.submit();
            }
        }

        // Escape key to clear input
        if (e.key === 'Escape' && input && document.activeElement === input) {
            input.value = '';
            input.blur();
            submitBtn.disabled = true;
        }
    });

    // ===== Result Page Handling =====
    const resultHeader = document.querySelector('.result-header');
    if (resultHeader) {
        // Check if correct or incorrect
        const isCorrect = resultHeader.classList.contains('result-correct');

        // Create live region for screen reader announcement
        const announcement = isCorrect 
            ? 'Correct answer! Well done.' 
            : 'Incorrect answer. Please review the solution.';

        const liveRegion = document.createElement('div');
        liveRegion.setAttribute('role', 'status');
        liveRegion.setAttribute('aria-live', 'polite');
        liveRegion.className = 'visually-hidden';
        liveRegion.textContent = announcement;
        document.body.appendChild(liveRegion);

        // Remove after announcement
        setTimeout(function () {
            if (document.body.contains(liveRegion)) {
                document.body.removeChild(liveRegion);
            }
        }, 3000);

        // Add hover effect to answer cards
        const answerCards = document.querySelectorAll('.answer-card');
        answerCards.forEach(function(card) {
            card.addEventListener('mouseenter', function() {
                this.style.transform = 'translateY(-2px)';
                this.style.boxShadow = '0 4px 8px rgba(0,0,0,0.15)';
            });

            card.addEventListener('mouseleave', function() {
                this.style.transform = 'translateY(0)';
                this.style.boxShadow = 'none';
            });
        });
    }

    // ===== Auto-dismiss Alerts =====
    const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    if (typeof bootstrap !== 'undefined' && bootstrap.Alert) {
        alerts.forEach(function (alert) {
            setTimeout(function () {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                bsAlert.close();
            }, 5000);
        });
    }

    // ===== Smooth Scroll to Validation Errors =====
    const validationMessages = document.querySelectorAll('.field-validation-error');
    if (validationMessages.length > 0) {
        const firstError = validationMessages[0];
        const errorField = firstError.previousElementSibling || 
                          firstError.parentElement.querySelector('input, select, textarea');

        if (errorField) {
            errorField.focus();
            errorField.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    // ===== Prevent Double Submission =====
    let isSubmitting = false;
    if (form) {
        form.addEventListener('submit', function (e) {
            if (isSubmitting) {
                e.preventDefault();
                return false;
            }
            isSubmitting = true;

            // Reset flag after 3 seconds (in case submission fails)
            setTimeout(function () {
                isSubmitting = false;
            }, 3000);
        });
    }

    // ===== Copy Solution to Clipboard =====
    const solutionBox = document.querySelector('.solution-box');
    if (solutionBox) {
        // Add copy button
        const copyBtn = document.createElement('button');
        copyBtn.className = 'btn btn-sm btn-outline-secondary copy-solution-btn';
        copyBtn.innerHTML = '📋 Copy Solution';
        copyBtn.style.cssText = 'position: absolute; top: 0.5rem; right: 0.5rem;';
        copyBtn.type = 'button';

        solutionBox.style.position = 'relative';
        solutionBox.appendChild(copyBtn);

        copyBtn.addEventListener('click', function() {
            const solutionText = document.querySelector('.solution-text').textContent;
            navigator.clipboard.writeText(solutionText).then(function() {
                copyBtn.innerHTML = '✓ Copied!';
                setTimeout(function() {
                    copyBtn.innerHTML = '📋 Copy Solution';
                }, 2000);
            }).catch(function(err) {
                if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                    console.error('Failed to copy: ', err);
                }
            });
        });
    }

    // ===== Performance Logging (Dev Only) =====
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        window.addEventListener('load', function () {
            if (window.performance && window.performance.timing) {
                const loadTime = window.performance.timing.loadEventEnd - 
                                window.performance.timing.navigationStart;
                console.log('Exercise page load time:', loadTime + 'ms');
            }
        });
    }

    // ===== Tooltip Initialization (if Bootstrap tooltips are used) =====
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // ===== Number Format Helper =====
    function formatCurrency(value) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD',
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(value);
    }

    function formatRatio(value) {
        return value.toFixed(2);
    }

    // ===== Export for testing =====
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            validateInput: validateInput,
            formatCurrency: formatCurrency,
            formatRatio: formatRatio
        };
    }

})();
