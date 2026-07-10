/**
 * Theme Selector - Appearance page theme preview and selection
 * Handles user interactions for previewing and applying themes.
 */
(function() {
    'use strict';

    const THEME_STORAGE_KEY = 'selectedTheme';
    let previewMode = false;
    let originalTheme = null;

    /**
     * Preview a theme temporarily without saving.
     * @param {string} themeId - The theme ID to preview.
     */
    function previewTheme(themeId) {
        if (!previewMode) {
            originalTheme = window.ThemeLoader.getSavedTheme();
            previewMode = true;
            showPreviewBanner();
        }

        window.ThemeLoader.applyTheme(themeId);
    }

    /**
     * Apply and save a theme permanently.
     * @param {string} themeId - The theme ID to apply.
     */
    function applyTheme(themeId) {
        try {
            // Save to localStorage
            localStorage.setItem(THEME_STORAGE_KEY, themeId);

            // Apply immediately
            window.ThemeLoader.applyTheme(themeId);

            // Exit preview mode
            exitPreviewMode();

            // Update UI to show active theme
            updateActiveThemeBadges(themeId);

            // Show success notification
            showToast(`Theme "${getThemeName(themeId)}" applied successfully!`);

        } catch (error) {
            console.error('Failed to save theme:', error);
            showToast('Failed to save theme. Please try again.', 'error');
        }
    }

    /**
     * Exit preview mode and restore original theme.
     */
    function exitPreviewMode() {
        if (previewMode) {
            previewMode = false;
            hidePreviewBanner();
        }
    }

    /**
     * Show a preview mode banner.
     */
    function showPreviewBanner() {
        let banner = document.getElementById('previewBanner');
        if (!banner) {
            banner = document.createElement('div');
            banner.id = 'previewBanner';
            banner.className = 'alert alert-info alert-dismissible fade show';
            banner.style.cssText = 'position: fixed; top: 60px; left: 50%; transform: translateX(-50%); z-index: 1050; min-width: 300px;';
            banner.innerHTML = `
                <strong>Preview Mode</strong> - Click "Apply Theme" to save your selection.
                <button type="button" class="btn-close" aria-label="Exit preview"></button>
            `;
            document.body.appendChild(banner);

            banner.querySelector('.btn-close').addEventListener('click', function() {
                window.ThemeLoader.applyTheme(originalTheme);
                exitPreviewMode();
            });
        }
        banner.classList.add('show');
    }

    /**
     * Hide the preview mode banner.
     */
    function hidePreviewBanner() {
        const banner = document.getElementById('previewBanner');
        if (banner) {
            banner.classList.remove('show');
            setTimeout(() => banner.remove(), 150);
        }
    }

    /**
     * Update "Currently Active" badges on theme cards.
     * @param {string} activeThemeId - The currently active theme ID.
     */
    function updateActiveThemeBadges(activeThemeId) {
        const badges = document.querySelectorAll('.current-theme-badge');
        badges.forEach(badge => {
            const card = badge.closest('.theme-card');
            const cardThemeId = card.getAttribute('data-theme-id');

            if (cardThemeId === activeThemeId) {
                badge.classList.remove('d-none');
            } else {
                badge.classList.add('d-none');
            }
        });
    }

    /**
     * Show a toast notification.
     * @param {string} message - The message to display.
     * @param {string} type - 'success' or 'error'.
     */
    function showToast(message, type = 'success') {
        const toastElement = document.getElementById('themeToast');
        const toastBody = document.getElementById('themeToastMessage');

        if (!toastElement || !toastBody) return;

        // Update message and style
        toastBody.textContent = message;
        toastElement.className = `toast align-items-center text-white border-0 ${type === 'error' ? 'bg-danger' : 'bg-success'}`;

        // Show toast
        const toast = new bootstrap.Toast(toastElement, { delay: 3000 });
        toast.show();
    }

    /**
     * Get human-readable theme name from theme ID.
     * @param {string} themeId - The theme ID.
     * @returns {string} Theme display name.
     */
    function getThemeName(themeId) {
        const themeNames = {
            'theme-default': 'Default',
            'theme-dark-mode': 'Dark Mode',
            'theme-high-contrast': 'High Contrast',
            'theme-ocean-blue': 'Ocean Blue',
            'theme-warm-sunset': 'Warm Sunset'
        };
        return themeNames[themeId] || themeId;
    }

    /**
     * Initialize theme selector on page load.
     */
    function initThemeSelector() {
        // Attach event listeners to Preview buttons
        const previewButtons = document.querySelectorAll('.preview-theme-btn');
        previewButtons.forEach(button => {
            button.addEventListener('click', function() {
                const themeId = this.getAttribute('data-theme-id');
                previewTheme(themeId);
            });
        });

        // Attach event listeners to Apply buttons
        const applyButtons = document.querySelectorAll('.apply-theme-btn');
        applyButtons.forEach(button => {
            button.addEventListener('click', function() {
                const themeId = this.getAttribute('data-theme-id');
                applyTheme(themeId);
            });
        });

        // Show active theme badge on page load
        const currentTheme = window.ThemeLoader.getSavedTheme();
        updateActiveThemeBadges(currentTheme);
    }

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', initThemeSelector);
})();
