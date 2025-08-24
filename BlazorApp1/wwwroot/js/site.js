window.scrollToBottom = (element) => {
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
};
