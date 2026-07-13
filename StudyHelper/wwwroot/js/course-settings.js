// course-settings.js
// Hydrates the remove-course confirmation modal and validates the CourseName field client-side.
(function () {
    'use strict';

    // ── Confirm-remove modal ──────────────────────────────────────────────────
    var confirmModal = document.getElementById('confirmRemoveModal');
    if (confirmModal) {
        confirmModal.addEventListener('show.bs.modal', function (event) {
            // Button that triggered the modal
            var button = event.relatedTarget;
            var courseName = button ? button.getAttribute('data-course-name') : '';

            // Populate modal label and hidden input with the selected course name
            document.getElementById('modalCourseName').textContent = courseName;
            document.getElementById('modalCourseNameInput').value = courseName;
        });
    }

    // ── Live regex feedback on CourseName input ───────────────────────────────
    // Mirrors the server-side regex: letters, numbers, hyphens, underscores only.
    var courseNameInput = document.getElementById('AddCourse_CourseName');
    if (courseNameInput) {
        var validPattern = /^[a-zA-Z0-9_-]*$/;
        courseNameInput.addEventListener('input', function () {
            if (!validPattern.test(this.value)) {
                this.classList.add('is-invalid');
            } else {
                this.classList.remove('is-invalid');
            }
        });
    }
}());
