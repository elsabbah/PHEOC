'use strict';
const scope = document.getElementById('qira-scope');
function showScope() {
    if (!scope) return;
    scope.open = true;
    scope.scrollIntoView({ block: 'start' });
    scope.querySelector('[name=Hazard]')?.focus({ preventScroll: true });
}
document.querySelector('[data-qira-open-scope]')?.addEventListener('click', event => {
    event.preventDefault();
    showScope();
});
document.querySelector('[data-qira-answer]')?.addEventListener('submit', event => {
    const form = event.currentTarget;
    if (form.elements.Node.value === 'Q1' && event.submitter?.value === 'Yes' && form.dataset.hazardValid !== 'true') {
        event.preventDefault();
        event.stopImmediatePropagation();
        form.querySelector('[data-qira-hazard-error]').hidden = false;
        showScope();
    }
}, { capture: true });
document.querySelector('[data-qira-answer]')?.addEventListener('keydown', event => {
    // Enter in an optional text field must not choose the first answer implicitly.
    if (event.key === 'Enter' && event.target.matches('input, select')) event.preventDefault();
});
