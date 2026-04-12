window.evolverDownloadFile = function (fileName, base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'download.xlsx';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

/** UTF-8 文本下载（如 CSV）；mime 默认带 BOM 的 text/csv，便于 Excel 识别中文 */
window.evolverDownloadTextFile = function (fileName, text, mimeType) {
    const mime = mimeType || 'text/csv;charset=utf-8';
    const blob = new Blob([text], { type: mime });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'export.csv';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

/** 将二维数组导出为 xlsx（需已加载 SheetJS / XLSX） */
window.evolverExportAoaToXlsx = function (aoaJson, fileName, sheetName) {
    if (typeof XLSX === 'undefined') {
        alert('未加载 SheetJS，无法导出 Excel。请刷新页面重试。');
        return;
    }
    var aoa;
    try {
        aoa = typeof aoaJson === 'string' ? JSON.parse(aoaJson) : aoaJson;
    } catch (e) {
        console.error(e);
        alert('导出数据解析失败。');
        return;
    }
    var ws = XLSX.utils.aoa_to_sheet(aoa);
    var wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName || 'Sheet1');
    XLSX.writeFile(wb, fileName || 'export.xlsx');
};

/** 简单 PDF 表格（需 jsPDF + autotable） */
window.evolverExportAoaToPdf = function (title, aoaJson, fileName) {
    var pdfCtor = typeof window.jsPDF !== 'undefined' ? window.jsPDF : window.jspdf && window.jspdf.jsPDF;
    if (typeof pdfCtor === 'undefined') {
        alert('未加载 jsPDF，无法导出 PDF。请刷新页面重试。');
        return;
    }
    var aoa;
    try {
        aoa = typeof aoaJson === 'string' ? JSON.parse(aoaJson) : aoaJson;
    } catch (e) {
        console.error(e);
        alert('导出数据解析失败。');
        return;
    }
    if (!aoa || aoa.length === 0) {
        alert('没有可导出的数据。');
        return;
    }
    var doc = new pdfCtor({ orientation: 'landscape', unit: 'pt', format: 'a4' });
    doc.setFontSize(12);
    doc.text(title || 'Export', 40, 36);
    doc.autoTable({
        head: [aoa[0]],
        body: aoa.slice(1),
        startY: 48,
        styles: { fontSize: 7, cellPadding: 2 },
        headStyles: { fillColor: [66, 139, 202] }
    });
    doc.save(fileName || 'export.pdf');
};
