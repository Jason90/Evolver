// Tabulator 用户表：初始化、同步数据、销毁、导出（CSV / XLSX / PDF）
// 依赖：tabulator-tables、SheetJS(XLSX)、jspdf、jspdf-autotable

(function () {
    function syncJsPdfGlobal() {
        if (window.jspdf && window.jspdf.jsPDF && typeof window.jsPDF === "undefined") {
            window.jsPDF = window.jspdf.jsPDF;
        }
    }
    syncJsPdfGlobal();

    /** jsPDF 内置解析面向 TrueType（glyf）；OTF/CFF 会触发 ttffont 解析错误。使用 Noto Sans SC 的 TTF（VF 子集）。 */
    var PDF_CJK_VFS_NAME = "NotoSansSC-VF.ttf";
    var PDF_CJK_FAMILY = "EvolverNotoSansSC";
    var PDF_CJK_URLS = [
        "/fonts/NotoSansSC-VF.ttf",
        "https://cdn.jsdelivr.net/gh/notofonts/noto-cjk@main/Sans/Variable/TTF/Subset/NotoSansSC-VF.ttf"
    ];

    function arrayBufferToBase64(buffer) {
        var bytes = new Uint8Array(buffer);
        var len = bytes.byteLength;
        var chunk = 0x8000;
        var binary = "";
        for (var i = 0; i < len; i += chunk) {
            binary += String.fromCharCode.apply(null, bytes.subarray(i, Math.min(i + chunk, len)));
        }
        return btoa(binary);
    }

    function ensurePdfCjkFontBase64() {
        if (window._evolverPdfCjkV2Base64) {
            return Promise.resolve(window._evolverPdfCjkV2Base64);
        }
        if (window._evolverPdfCjkV2Promise) {
            return window._evolverPdfCjkV2Promise;
        }
        var p = new Promise(function (resolve, reject) {
            var urls = PDF_CJK_URLS.slice();
            function tryNext() {
                if (!urls.length) {
                    reject(
                        new Error(
                            "无法加载中文字体：请将 NotoSansSC-VF.ttf 放到 wwwroot/fonts/ 或检查网络访问 CDN"
                        )
                    );
                    return;
                }
                var url = urls.shift();
                fetch(url, { mode: "cors" })
                    .then(function (r) {
                        if (!r.ok) throw new Error("HTTP " + r.status);
                        return r.arrayBuffer();
                    })
                    .then(function (buf) {
                        window._evolverPdfCjkV2Base64 = arrayBufferToBase64(buf);
                        resolve(window._evolverPdfCjkV2Base64);
                    })
                    .catch(function () {
                        tryNext();
                    });
            }
            tryNext();
        });
        p.catch(function () {
            window._evolverPdfCjkV2Promise = null;
        });
        window._evolverPdfCjkV2Promise = p;
        return p;
    }

    function registerPdfCjkFont(doc, base64) {
        doc.addFileToVFS(PDF_CJK_VFS_NAME, base64);
        doc.addFont(PDF_CJK_VFS_NAME, PDF_CJK_FAMILY, "normal");
        doc.setFont(PDF_CJK_FAMILY, "normal");
    }

    /**
     * jspdf-autotable 对部分 styles.textColor（如 RGB 数组）+ 自定义字体的组合会画出“空字”；
     * 在解析/绘制单元格时强制字体与前景色。
     */
    function buildPdfAutoTableOptions(doc) {
        return {
            /** plain：避免 striped 主题为表头注入 fontStyle:bold（未注册粗体会导致整格文字不绘制） */
            theme: "plain",
            margin: { top: 44, left: 40, right: 40, bottom: 36 },
            styles: {
                font: PDF_CJK_FAMILY,
                fontSize: 8,
                cellPadding: 3,
                textColor: 30,
                fontStyle: "normal"
            },
            headStyles: {
                font: PDF_CJK_FAMILY,
                fontStyle: "normal",
                fillColor: [13, 110, 253],
                textColor: 255
            },
            bodyStyles: {
                font: PDF_CJK_FAMILY,
                fontStyle: "normal",
                textColor: 30
            },
            alternateRowStyles: {
                fillColor: [248, 249, 250],
                font: PDF_CJK_FAMILY,
                textColor: 30
            },
            didParseCell: function (data) {
                data.cell.styles.font = PDF_CJK_FAMILY;
                data.cell.styles.fontStyle = "normal";
            },
            willDrawCell: function (data) {
                doc.setFont(PDF_CJK_FAMILY, "normal");
                var sec = data.section != null ? data.section : data.row && data.row.section;
                if (sec === "head") {
                    doc.setTextColor(255, 255, 255);
                } else {
                    doc.setTextColor(30, 30, 30);
                }
            },
            didDrawPage: function (data) {
                try {
                    if (data.pageNumber !== 1) return;
                    var left = 40;
                    if (data.settings && data.settings.margin && typeof data.settings.margin.left === "number") {
                        left = data.settings.margin.left;
                    }
                    doc.setFont(PDF_CJK_FAMILY, "normal");
                    doc.setFontSize(11);
                    doc.setTextColor(33, 37, 41);
                    doc.text("用户列表", left, 28);
                } catch (e) {
                    console.warn("evolverUserTable pdf didDrawPage", e);
                }
            }
        };
    }
    let table = null;
    let dotNetRef = null;
    let hostEl = null;
    let hostResizeObserver = null;

    function disconnectHostResizeObserver() {
        if (hostResizeObserver) {
            hostResizeObserver.disconnect();
            hostResizeObserver = null;
        }
    }

    function applyTableHeightFromHost() {
        if (!table || !hostEl) return;
        var rect = hostEl.getBoundingClientRect();
        var h = Math.floor(rect.height);
        if (h >= 120) {
            table.setHeight(h);
        }
    }

    function attachHostResizeObserver(el) {
        disconnectHostResizeObserver();
        if (!el || typeof ResizeObserver === "undefined") return;
        hostResizeObserver = new ResizeObserver(function () {
            applyTableHeightFromHost();
        });
        hostResizeObserver.observe(el);
        requestAnimationFrame(function () {
            requestAnimationFrame(applyTableHeightFromHost);
        });
    }

    function buildColumns(currentUserId) {
        const uid = currentUserId != null ? Number(currentUserId) : null;

        return [
            {
                formatter: "rowSelection",
                titleFormatter: "rowSelection",
                hozAlign: "center",
                headerSort: false,
                width: 42
            },
            {
                title: "Id",
                field: "id",
                width: 90,
                sorter: "number"
            },
            {
                title: "用户名",
                field: "userName",
                minWidth: 120,
                sorter: "string"
            },
            {
                title: "Email",
                field: "email",
                minWidth: 160,
                sorter: "string"
            },
            {
                title: "电话",
                field: "phoneNumber",
                minWidth: 110,
                sorter: "string"
            },
            {
                title: "角色",
                field: "rolesText",
                minWidth: 140,
                sorter: "string"
            },
            {
                title: "状态",
                field: "statusText",
                width: 100,
                cssClass: "ev-user-status-col",
                sorter: "string",
                formatter: function (cell) {
                    const v = cell.getValue();
                    if (v === "正常") {
                        return "<span class=\"ev-user-status-pill ev-user-status--ok\">正常</span>";
                    }
                    return "<span class=\"ev-user-status-pill ev-user-status--off\">停用</span>";
                }
            },
            {
                title: "操作",
                field: "_op",
                width: 220,
                hozAlign: "left",
                headerSort: false,
                /**
                 * 使用 DOM 节点 + 直接绑定 click，并 stopPropagation。
                 * selectable: true 时，仅靠列 cellClick + 内联 HTML 容易出现点击无响应
                 *（事件被行选择或未落到带 class 的 button 上）。
                 */
                formatter: function (cell) {
                    const row = cell.getRow().getData();
                    const id = Number(row.id);
                    const userName = row.userName || "";
                    const disableDel = uid != null && id === uid;

                    const wrap = document.createElement("div");
                    wrap.className = "btn-group btn-group-sm";
                    wrap.setAttribute("role", "group");

                    /** 捕获阶段处理，避免 Tabulator 行选择等在冒泡阶段吞掉 click。 */
                    function wireClick(btn, handler) {
                        btn.addEventListener(
                            "click",
                            function (e) {
                                e.stopPropagation();
                                e.preventDefault();
                                if (!dotNetRef || btn.disabled) return;
                                handler();
                            },
                            true
                        );
                    }

                    const editBtn = document.createElement("button");
                    editBtn.type = "button";
                    editBtn.className = "btn btn-outline-primary";
                    editBtn.textContent = "编辑";
                    wireClick(editBtn, function () {
                        dotNetRef.invokeMethodAsync("OnUserTableEdit", id).catch(function (err) {
                            console.error("OnUserTableEdit", err);
                        });
                    });

                    const pwdBtn = document.createElement("button");
                    pwdBtn.type = "button";
                    pwdBtn.className = "btn btn-outline-secondary";
                    pwdBtn.textContent = "重置密码";
                    wireClick(pwdBtn, function () {
                        dotNetRef.invokeMethodAsync("OnUserTablePassword", id, userName).catch(function (err) {
                            console.error("OnUserTablePassword", err);
                        });
                    });

                    const delBtn = document.createElement("button");
                    delBtn.type = "button";
                    delBtn.className = "btn btn-outline-danger";
                    delBtn.textContent = "删除";
                    delBtn.disabled = !!disableDel;
                    wireClick(delBtn, function () {
                        dotNetRef.invokeMethodAsync("OnUserTableDelete", id).catch(function (err) {
                            console.error("OnUserTableDelete", err);
                        });
                    });

                    wrap.appendChild(editBtn);
                    wrap.appendChild(pwdBtn);
                    wrap.appendChild(delBtn);
                    return wrap;
                }
            }
        ];
    }

    function createTable(el, rows, dnRef, currentUserId) {
        hostEl = el;
        dotNetRef = dnRef;
        var initialH = Math.floor(el.getBoundingClientRect().height);
        if (initialH < 120) initialH = 400;

        table = new Tabulator(el, {
            data: rows,
            layout: "fitColumns",
            placeholder: "暂无数据",
            height: initialH,
            pagination: true,
            paginationSize: 10,
            paginationSizeSelector: [10, 15, 25, 50, 100],
            paginationCounter: "rows",
            selectable: true,
            columns: buildColumns(currentUserId),
            initialSort: [{ column: "userName", dir: "asc" }]
        });

        attachHostResizeObserver(el);
    }

    window.evolverUserTable = {
        /** hostId: 宿主元素 id（Blazor 传递字符串最稳妥） */
        sync: function (hostId, json, dnRef, currentUserId) {
            const el = typeof hostId === "string" ? document.getElementById(hostId) : hostId;
            if (!el) {
                console.warn("evolverUserTable: host not found", hostId);
                return;
            }

            let rows;
            try {
                rows = typeof json === "string" ? JSON.parse(json) : json;
            } catch (e) {
                console.error(e);
                return;
            }

            if (!table || hostEl !== el) {
                if (table) {
                    table.destroy();
                    table = null;
                }
                hostEl = el;
                createTable(el, rows, dnRef, currentUserId);
                return;
            }

            dotNetRef = dnRef;
            table.replaceData(rows);
            requestAnimationFrame(function () {
                requestAnimationFrame(applyTableHeightFromHost);
            });
        },

        /** 全文检索：在各列拼接文本中匹配（不区分大小写） */
        setGlobalFilter: function (text) {
            if (!table) return;
            const t = (text || "").trim().toLowerCase();
            if (!t) {
                table.clearFilter(true);
                return;
            }
            table.setFilter(function (data) {
                const hay = [
                    String(data.id ?? ""),
                    String(data.userName ?? ""),
                    String(data.email ?? ""),
                    String(data.phoneNumber ?? ""),
                    String(data.rolesText ?? ""),
                    String(data.statusText ?? "")
                ]
                    .join(" ")
                    .toLowerCase();
                return hay.indexOf(t) !== -1;
            });
        },

        destroy: function () {
            disconnectHostResizeObserver();
            if (table) {
                table.destroy();
                table = null;
            }
            hostEl = null;
        },

        download: function (format) {
            if (!table) return;
            const stamp = new Date().toISOString().slice(0, 19).replace(/[-:T]/g, "");
            if (format === "csv") {
                table.download("csv", "users_" + stamp + ".csv", { bom: true });
            } else if (format === "xlsx") {
                if (typeof XLSX === "undefined") {
                    alert("未加载 SheetJS，无法导出 Excel。请刷新页面重试。");
                    return;
                }
                table.download("xlsx", "users_" + stamp + ".xlsx", { sheetName: "Users" });
            } else if (format === "pdf") {
                syncJsPdfGlobal();
                var pdfCtor = typeof window.jsPDF !== "undefined" ? window.jsPDF : window.jspdf && window.jspdf.jsPDF;
                if (typeof pdfCtor === "undefined") {
                    alert("未加载 jsPDF，无法导出 PDF。请刷新页面重试。");
                    return;
                }
                ensurePdfCjkFontBase64()
                    .then(function (b64) {
                        table.download("pdf", "users_" + stamp + ".pdf", {
                            orientation: "landscape",
                            /** 勿传中文 title：Tabulator 会用默认字体覆盖 didDrawPage 绘制标题，导致乱码 */
                            autoTable: function (doc) {
                                registerPdfCjkFont(doc, b64);
                                return buildPdfAutoTableOptions(doc);
                            }
                        });
                    })
                    .catch(function (e) {
                        console.error(e);
                        alert("导出 PDF 失败：" + (e && e.message ? e.message : String(e)));
                    });
            }
        }
    };
})();
