/**
 * Theme Loader - Global theme application system
 * Loads user's saved theme preference and applies it before page render.
 */
(function() {
    'use strict';

    const THEME_STORAGE_KEY = 'selectedTheme';
    const DEFAULT_THEME = 'theme-default';

    /**
     * Get the currently saved theme ID from localStorage.
     * @returns {string} Theme ID or default theme.
     */
    function getSavedTheme() {
        try {
            const savedTheme = localStorage.getItem(THEME_STORAGE_KEY);
            return savedTheme || DEFAULT_THEME;
        } catch (error) {
            console.warn('Could not access localStorage, using default theme:', error);
            return DEFAULT_THEME;
        }
    }

    /**
     * Apply a theme by setting a data attribute on the HTML element.
     * CSS uses this attribute selector to apply theme-specific styles.
     * @param {string} themeId - The theme ID to apply.
     */
    function applyTheme(themeId) {
        const validThemes = [
            'theme-default',
            'theme-dark-mode',
            'theme-high-contrast',
            'theme-ocean-blue',
            'theme-warm-sunset',
            'theme-forest-green',
            'theme-midnight-blue',
            'theme-royal-purple',
            'theme-amber-gold',
            'theme-cyberpunk',
            'theme-slate-gray',
            'theme-cherry-blossom',
            'theme-deep-teal',
            'theme-espresso',
            'theme-lavender-dream',
            'theme-mint-fresh',
            'theme-crimson-night'
        ];

        // Validate theme ID
        if (!validThemes.includes(themeId)) {
            console.warn(`Invalid theme ID: ${themeId}, falling back to default.`);
            themeId = DEFAULT_THEME;
        }

        // Set data attribute on HTML element
        document.documentElement.setAttribute('data-theme', themeId);

        // Also set a class for backward compatibility
        document.documentElement.className = themeId;
    }

    /**
     * Initialize theme loader on page load.
     */
    function initThemeLoader() {
        const themeId = getSavedTheme();
        applyTheme(themeId);
    }

    // Execute immediately (before DOMContentLoaded to prevent FOUC)
    initThemeLoader();

    // Export for use by other scripts
    window.ThemeLoader = {
        applyTheme: applyTheme,
        getSavedTheme: getSavedTheme,
        DEFAULT_THEME: DEFAULT_THEME
    };
})();
