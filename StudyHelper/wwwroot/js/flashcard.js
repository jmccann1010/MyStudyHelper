// Flashcard timer and reveal functionality
// Wrapped in IIFE to prevent global scope pollution

(function() {
    'use strict';

    let timerInterval;
    let secondsRemaining = 30;

    // Start countdown timer on page load
    document.addEventListener('DOMContentLoaded', function() {
        startTimer();

        // Add click event listener to "Show Answer" button
        const showAnswerBtn = document.getElementById('show-answer-btn');
        if (showAnswerBtn) {
            showAnswerBtn.addEventListener('click', function(e) {
                e.preventDefault();
                revealTerm();
            });
        }
    });

    function startTimer() {
        secondsRemaining = 30; // Reset state
        const timerCountdown = document.getElementById('timer-countdown');

        if (!timerCountdown) return; // Guard clause

        // Set initial display to 30
        timerCountdown.textContent = secondsRemaining;

        timerInterval = setInterval(function() {
            secondsRemaining--;
            timerCountdown.textContent = secondsRemaining;

            if (secondsRemaining <= 0) {
                clearInterval(timerInterval);
                revealTerm();
            }
        }, 1000);
    }

    function revealTerm() {
        // Clear interval if still running
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }

        // Hide timer section immediately
        const timerSection = document.getElementById('timer-section');
        if (timerSection) {
            timerSection.style.display = 'none';
        }

        // Show term section with animation
        const termSection = document.getElementById('term-section');
        if (termSection) {
            termSection.style.display = 'block';
        }

        // Hide "Show Answer" button
        const showAnswerBtn = document.getElementById('show-answer-btn');
        if (showAnswerBtn) {
            showAnswerBtn.style.display = 'none';
        }
    }

    // Keyboard shortcut (Space = Show Answer)
    document.addEventListener('keydown', function(event) {
        const showAnswerBtn = document.getElementById('show-answer-btn');

        if (event.code === 'Space' && showAnswerBtn && showAnswerBtn.style.display !== 'none') {
            event.preventDefault();
            revealTerm();
        }
    });
})();
