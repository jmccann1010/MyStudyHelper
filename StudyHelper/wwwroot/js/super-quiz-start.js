/**
 * Super Quiz Start Page - Dynamic Preview Updates
 * Handles real-time updates to question count and estimated time
 * when user selects different question count options.
 */

(function () {
    'use strict';

    // Constants
    const SECONDS_PER_QUESTION = 15;
    const MINUTES_PER_QUESTION = SECONDS_PER_QUESTION / 60; // 0.25

    /**
     * Initialize dynamic preview functionality on page load
     */
    document.addEventListener('DOMContentLoaded', function () {
        initializePreviewUpdates();
    });

    /**
     * Set up event listeners and perform initial preview update
     */
    function initializePreviewUpdates() {
        const radioButtons = document.querySelectorAll('input[name="questionCount"]');
        const previewCount = document.getElementById('preview-count');
        const previewTime = document.getElementById('preview-time');

        // Validate required elements exist
        if (!previewCount || !previewTime || radioButtons.length === 0) {
            console.warn('Super Quiz Start: Required preview elements not found');
            return;
        }

        // Attach change event listeners to all radio buttons
        radioButtons.forEach(function (radio) {
            radio.addEventListener('change', function () {
                updatePreview(previewCount, previewTime);
                updateSelectionVisuals();
            });
        });

        // Attach click event listeners to option containers for better UX
        const optionContainers = document.querySelectorAll('.super-quiz-option');
        optionContainers.forEach(function (container) {
            container.addEventListener('click', function (e) {
                // Only trigger if the click wasn't directly on the radio button or label
                if (e.target.tagName !== 'INPUT' && e.target.tagName !== 'LABEL') {
                    const radio = container.querySelector('input[type="radio"]');
                    if (radio) {
                        radio.checked = true;
                        radio.dispatchEvent(new Event('change'));
                    }
                }
            });
        });

        // Perform initial update on page load
        updatePreview(previewCount, previewTime);
        updateSelectionVisuals();
    }

    /**
     * Update preview cards with selected question count and estimated time
     * @param {HTMLElement} previewCount - Element displaying question count
     * @param {HTMLElement} previewTime - Element displaying estimated time
     */
    function updatePreview(previewCount, previewTime) {
        const selectedRadio = document.querySelector('input[name="questionCount"]:checked');

        if (!selectedRadio) {
            console.warn('Super Quiz Start: No radio button selected');
            return;
        }

        // Read question count from value (which is the actual count)
        const count = parseInt(selectedRadio.value, 10);

        if (isNaN(count) || count < 0) {
            console.error('Super Quiz Start: Invalid question count:', selectedRadio.value);
            // Display error state in preview cards
            previewCount.textContent = '??';
            previewTime.textContent = 'Error';
            return;
        }

        // Calculate estimated time using constant
        const timeMinutes = count * MINUTES_PER_QUESTION;

        // Add pulse animation to preview cards
        previewCount.parentElement.parentElement.classList.add('preview-updating');
        previewTime.parentElement.parentElement.classList.add('preview-updating');

        // Update question count
        previewCount.textContent = count;

        // Format and update time display
        previewTime.textContent = formatTime(timeMinutes);

        // Remove animation class after animation completes
        setTimeout(function () {
            previewCount.parentElement.parentElement.classList.remove('preview-updating');
            previewTime.parentElement.parentElement.classList.remove('preview-updating');
        }, 300);
    }

    /**
     * Update visual styling for selected option
     */
    function updateSelectionVisuals() {
        const optionContainers = document.querySelectorAll('.super-quiz-option');

        optionContainers.forEach(function (container) {
            const radio = container.querySelector('input[type="radio"]');
            if (radio && radio.checked) {
                container.classList.add('selected');
            } else {
                container.classList.remove('selected');
            }
        });
    }

    /**
     * Format time in minutes or hours
     * @param {number} minutes - Time in minutes
     * @returns {string} Formatted time string
     */
    function formatTime(minutes) {
        if (minutes < 60) {
            return Math.round(minutes) + ' minutes';
        } else {
            return (minutes / 60).toFixed(1) + ' hours';
        }
    }

})();
