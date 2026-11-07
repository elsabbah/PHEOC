document.getElementById('qira-add-template')?.addEventListener('click', () => {
    const list = document.getElementById('qira-templates');
    const index = list.children.length;
    if (index >= 50) return;
    const copy = list.firstElementChild.cloneNode(true);
    copy.querySelector('legend').textContent = `إجراء ${index + 1}`;
    copy.querySelectorAll('input,select').forEach(field => {
        field.name = field.name.replace(/Actions\[\d+\]/, `Actions[${index}]`);
        if (field.tagName === 'SELECT') field.selectedIndex = 0;
        else field.value = field.type === 'number' ? '0' : '';
    });
    list.append(copy);
    copy.querySelector('select').focus();
});
