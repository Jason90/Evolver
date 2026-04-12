// 通用 UI：点击外部关闭浮层（capture 阶段，不阻止其它控件收到同一次点击）
window.evolverUi = window.evolverUi || {};

window.evolverUi._exportClickOutsideHandler = null;

window.evolverUi.startExportMenuClickOutside = function (element, dotNetObj, methodName) {
    window.evolverUi.stopExportMenuClickOutside();
    if (!element || !dotNetObj) return;

    window.evolverUi._exportClickOutsideHandler = function (ev) {
        if (element.contains(ev.target)) return;
        dotNetObj.invokeMethodAsync(methodName);
    };
    document.addEventListener("click", window.evolverUi._exportClickOutsideHandler, true);
};

window.evolverUi.stopExportMenuClickOutside = function () {
    if (window.evolverUi._exportClickOutsideHandler) {
        document.removeEventListener("click", window.evolverUi._exportClickOutsideHandler, true);
        window.evolverUi._exportClickOutsideHandler = null;
    }
};

/** 整页等待光标（导入/导出等耗时操作） */
window.evolverUi.setWaitCursor = function (on) {
    var root = document.documentElement;
    if (on) root.classList.add("evolver-wait-cursor");
    else root.classList.remove("evolver-wait-cursor");
};
