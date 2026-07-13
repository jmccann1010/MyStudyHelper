// Flashcard timer and reveal functionality
// Wrapped in IIFE to prevent global scope pollution

(function() {
    'use strict';

    let timerInterval;
    let secondsRemaining = 30;

    function initializeFlashcard() {
        startTimer();

        // Add click event listener to "Show Answer" button
        const showAnswerBtn = document.getElementById('show-answer-btn');
        if (showAnswerBtn) {
            showAnswerBtn.addEventListener('click', function(e) {
                e.preventDefault();
                revealTerm();
            });
        }
    }

    // Start when DOM is ready — DOMContentLoaded may have already fired when
    // the script loads from the bottom of <body>, so check readyState first.
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeFlashcard);
    } else {
        initializeFlashcard();
    }

    // Reset the card when the browser restores it from the back/forward cache
    // so the answer is not already visible on page restore.
    window.addEventListener('pageshow', function(event) {
        if (event.persisted) {
            resetCard();
        }
    });

    function resetCard() {
        // Stop any running timer
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }

        // Restore hidden/visible state to the initial page-load defaults
        const timerSection = document.getElementById('timer-section');
        const termSection  = document.getElementById('term-section');
        const showAnswerBtn = document.getElementById('show-answer-btn');

        if (timerSection)   timerSection.classList.remove('flashcard-hidden');
        if (termSection)    termSection.classList.add('flashcard-hidden');
        if (showAnswerBtn)  showAnswerBtn.classList.remove('flashcard-hidden');

        startTimer();
    }

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

        // Hide timer section
        const timerSection = document.getElementById('timer-section');
        if (timerSection) {
            timerSection.classList.add('flashcard-hidden');
        }

        // Show term section
        const termSection = document.getElementById('term-section');
        if (termSection) {
            termSection.classList.remove('flashcard-hidden');
        }

        // Hide "Show Answer" button
        const showAnswerBtn = document.getElementById('show-answer-btn');
        if (showAnswerBtn) {
            showAnswerBtn.classList.add('flashcard-hidden');
        }
    }

    // Keyboard shortcut (Space = Show Answer)
    document.addEventListener('keydown', function(event) {
        const showAnswerBtn = document.getElementById('show-answer-btn');

        if (event.code === 'Space' && showAnswerBtn && !showAnswerBtn.classList.contains('flashcard-hidden')) {
            event.preventDefault();
            revealTerm();
        }
    });
})();
