// Quiz JavaScript - Progressive Enhancements
// All functionality degrades gracefully without JavaScript

(function() {
    'use strict';

    document.addEventListener('DOMContentLoaded', function() {
        initializeQuizAnswers();
        initializeFormValidation();
        initializeAccessibility();
    });

    /**
     * Enhance answer option selection with visual feedback
     */
    function initializeQuizAnswers() {
        const answerOptions = document.querySelectorAll('.quiz-answer-option');

        if (answerOptions.length === 0) {
            return;
        }

        answerOptions.forEach(function(option) {
            const radio = option.querySelector('input[type="radio"]');

            if (!radio) {
                return;
            }

            // Add selected class when radio button changes
            radio.addEventListener('change', function() {
                // Remove selected class from all options
                answerOptions.forEach(function(opt) {
                    opt.classList.remove('selected');
                });

                // Add selected class to current option
                if (radio.checked) {
                    option.classList.add('selected');
                }
            });

            // Allow clicking anywhere on the option to select
            option.addEventListener('click', function(e) {
                // Don't trigger if clicking the radio button itself
                if (e.target !== radio) {
                    radio.checked = true;
                    radio.dispatchEvent(new Event('change'));
                }
            });

            // Add keyboard support (Enter or Space on option)
            option.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    radio.checked = true;
                    radio.dispatchEvent(new Event('change'));
                }
            });
        });
    }

    /**
     * Add client-side form validation
     */
    function initializeFormValidation() {
        const quizForm = document.querySelector('.quiz-form');

        if (!quizForm) {
            return;
        }

        quizForm.addEventListener('submit', function(e) {
            const selectedAnswer = quizForm.querySelector('input[name="selectedAnswerIndex"]:checked');

            if (!selectedAnswer) {
                e.preventDefault();
                showValidationError('Please select an answer before submitting.');
                return false;
            }
        });
    }

    /**
     * Display validation error message
     */
    function showValidationError(message) {
        // Check if error already exists
        let errorDiv = document.querySelector('.quiz-validation-error');

        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'alert alert-warning alert-dismissible fade show quiz-validation-error';
            errorDiv.setAttribute('role', 'alert');

            const messageSpan = document.createElement('span');
            messageSpan.textContent = message;

            const closeButton = document.createElement('button');
            closeButton.type = 'button';
            closeButton.className = 'btn-close';
            closeButton.setAttribute('data-bs-dismiss', 'alert');
            closeButton.setAttribute('aria-label', 'Close');

            errorDiv.appendChild(messageSpan);
            errorDiv.appendChild(closeButton);

            const container = document.querySelector('.quiz-container');
            if (container) {
                container.insertBefore(errorDiv, container.firstChild);
            }
        } else {
            // Update existing error message
            errorDiv.querySelector('span').textContent = message;
        }

        // Focus on error for screen readers
        errorDiv.focus();
    }

    /**
     * Enhance accessibility features
     */
    function initializeAccessibility() {
        // Auto-focus first radio button for keyboard users
        const firstRadio = document.querySelector('.quiz-answer-option input[type="radio"]');
        if (firstRadio && isKeyboardUser()) {
            // Small delay to avoid interfering with page load
            setTimeout(function() {
                firstRadio.focus();
            }, 100);
        }

        // Announce result to screen readers
        const feedbackAlert = document.querySelector('.quiz-feedback');
        if (feedbackAlert) {
            feedbackAlert.setAttribute('aria-live', 'polite');
            feedbackAlert.setAttribute('aria-atomic', 'true');
        }
    }

    /**
     * Detect if user is navigating with keyboard
     */
    function isKeyboardUser() {
        // Simple heuristic: check if Tab key was recently pressed
        let keyboardNavigation = false;

        document.addEventListener('keydown', function(e) {
            if (e.key === 'Tab') {
                keyboardNavigation = true;
            }
        });

        document.addEventListener('mousedown', function() {
            keyboardNavigation = false;
        });

        return keyboardNavigation;
    }

    /**
     * Optional: Add keyboard shortcut for quick answer selection (A, B, C, D)
     */
    function initializeKeyboardShortcuts() {
        document.addEventListener('keydown', function(e) {
            // Only work if focus is not in a form input
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                return;
            }

            const key = e.key.toUpperCase();

            if (['A', 'B', 'C', 'D'].includes(key)) {
                const index = key.charCodeAt(0) - 'A'.charCodeAt(0);
                const radio = document.querySelector(`input[name="selectedAnswerIndex"][value="${index}"]`);

                if (radio) {
                    radio.checked = true;
                    radio.dispatchEvent(new Event('change'));
                    radio.focus();
                    e.preventDefault();
                }
            }
        });
    }

    // Uncomment to enable keyboard shortcuts
    // initializeKeyboardShortcuts();

})();
