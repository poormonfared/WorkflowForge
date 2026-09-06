window.workflowForge = window.workflowForge || {};

window.workflowForge.downloadFile = (fileName, base64) => {
    const link = document.createElement('a');
    link.href = 'data:application/zip;base64,' + base64;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
