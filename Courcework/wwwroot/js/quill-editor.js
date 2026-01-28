// Quill.js Bridge for MAUI Blazor
// This file handles initialization and communication with Quill editor

let quillEditors = {};

window.initQuill = (editorId) => {
    quillEditors[editorId] = new Quill(`#${editorId}`, {
        theme: "snow",
        modules: {
            toolbar: [
                ["bold", "italic", "underline"],
                [{ list: "ordered" }, { list: "bullet" }],
                [{ header: [1, 2, false] }],
                ["clean"]
            ]
        }
    });
    console.log(`Quill editor initialized for #${editorId}`);
};

window.getQuillHtml = (editorId = "editor") => {
    if (quillEditors[editorId]) {
        return quillEditors[editorId].root.innerHTML;
    }
    return "";
};

window.setQuillHtml = (editorId, html) => {
    if (quillEditors[editorId]) {
        quillEditors[editorId].root.innerHTML = html;
        console.log(`Set HTML for #${editorId}`);
    }
};
