(function () {
    'use strict';

    // Timer state
    let secondsRemaining = 30;
    let timerInterval = null;

    // DOM element references (cached for performance)
    const timerCountdown = document.getElementById('timer-countdown');
    const timerSection = document.getElementById('timer-section');
    const answerSection = document.getElementById('answer-section');
    const showAnswerBtn = document.getElementById('show-answer-btn');

    /**
     * Initializes the flashcard timer when the page loads.
     */
    function initializeFlashcard() {
        // Null guards
        if (!timerCountdown || !timerSection || !answerSection || !showAnswerBtn) {
            console.error('Required flashcard elements not found in DOM');
            return;
        }

        // Start countdown automatically
        startTimer();

        // Add click event listener to "Show Answer" button
        showAnswerBtn.addEventListener('click', function(e) {
            e.preventDefault();
            revealAnswer();
        });

        // Keyboard shortcut: Space bar to reveal answer
        document.addEventListener('keydown', function (e) {
            if (e.code === 'Space' && answerSection.classList.contains('flashcard-hidden')) {
                e.preventDefault(); // Prevent page scroll
                revealAnswer();
            }
        });
    }

    /**
     * Starts the 30-second countdown timer.
     */
    function startTimer() {
        // Clear any existing timer
        if (timerInterval) {
            clearInterval(timerInterval);
        }

        // Reset state
        secondsRemaining = 30;
        timerCountdown.textContent = secondsRemaining;

        // Start interval (runs every 1 second)
        timerInterval = setInterval(function () {
            secondsRemaining--;
            timerCountdown.textContent = secondsRemaining;

            if (secondsRemaining <= 0) {
                clearInterval(timerInterval);
                revealAnswer();
            }
        }, 1000);
    }

    /**
     * Reveals the answer section and hides the timer.
     */
    function revealAnswer() {
        // Clear timer immediately
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }

        // Hide timer section
        timerSection.classList.add('flashcard-hidden');

        // Show answer section
        answerSection.classList.remove('flashcard-hidden');

        // Hide "Show Answer" button
        showAnswerBtn.classList.add('flashcard-hidden');

        // Announce to screen readers
        answerSection.setAttribute('aria-live', 'assertive');
    }

    /**
     * Resets the card to its initial (answer hidden, timer running) state.
     * Called when the browser restores the page from the back/forward cache.
     */
    function resetCard() {
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }

        // Restore element visibility to page-load defaults
        if (timerSection)   timerSection.classList.remove('flashcard-hidden');
        if (answerSection)  answerSection.classList.add('flashcard-hidden');
        if (showAnswerBtn)  showAnswerBtn.classList.remove('flashcard-hidden');

        startTimer();
    }

    // Initialize when DOM is ready
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
})();
