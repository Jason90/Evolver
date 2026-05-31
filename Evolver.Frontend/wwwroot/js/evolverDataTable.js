// Evolver 功能表格：多实例 Tabulator、按 schema 注册列/筛选/导出；依赖 tabulator-tables、SheetJS、jspdf、jspdf-autotable
(function () {
    function syncJsPdfGlobal() {
        if (window.jspdf && window.jspdf.jsPDF && typeof window.jsPDF === "undefined") {
            window.jsPDF = window.jspdf.jsPDF;
        }
    }
    syncJsPdfGlobal();

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

    function buildPdfAutoTableOptions(doc, pdfTitle) {
        var title = pdfTitle || "列表";
        return {
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
                    doc.text(title, left, 28);
                } catch (e) {
                    console.warn("evolverDataTable pdf didDrawPage", e);
                }
            }
        };
    }

    /** hostId -> { table, hostEl, dotNetRef, schemaKey, currentUserId, resizeObserver } */
    var instances = Object.create(null);

    function getInst(hostId) {
        return instances[hostId] || null;
    }

    function disconnectHostResizeObserver(inst) {
        if (inst && inst.resizeObserver) {
            inst.resizeObserver.disconnect();
            inst.resizeObserver = null;
        }
    }

    function redrawTable(hostId) {
        var inst = getInst(hostId);
        if (!inst || !inst.table) return;
        try {
            inst.table.redraw(true);
        } catch (e) {
            console.warn("evolverDataTable redraw", e);
        }
    }

    function attachWindowResizeRedraw(hostId) {
        var inst = getInst(hostId);
        if (!inst) return;
        if (inst._onWinResize) return;
        inst._onWinResize = function () {
            redrawTable(hostId);
        };
        window.addEventListener("resize", inst._onWinResize);
    }

    function detachWindowResizeRedraw(inst) {
        if (inst && inst._onWinResize) {
            window.removeEventListener("resize", inst._onWinResize);
            inst._onWinResize = null;
        }
    }

    /**
     * 列定义工厂：ctx = { hostId, dotNetRef, currentUserId }
     * 新页面在下方 schemas 中追加即可。
     */
    var schemas = {
        users: {
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无数据",
                /** 不设置固定 height，表格随当前页行数增高；超出视口时由 maxHeight 产生内部滚动 */
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "userName", dir: "asc" }]
            },
            afterCreateTable: function (table) {
                function wireRowDrag(row) {
                    try {
                        var el = row.getElement();
                        var data = row.getData();
                        if (!el || !data) return;
                        el.setAttribute("draggable", "true");
                        if (!el.__evUserDragWired) {
                            el.addEventListener("dragstart", function () {
                                if (window.evolverUi && typeof window.evolverUi.setDraggingUserId === "function") {
                                    window.evolverUi.setDraggingUserId(Number(data.id));
                                }
                            });
                            el.__evUserDragWired = true;
                        }
                    } catch (e) {
                        console.warn("wire user row drag", e);
                    }
                }
                table.getRows().forEach(wireRowDrag);
                table.on("dataProcessed", function () {
                    table.getRows().forEach(wireRowDrag);
                });
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                var uid = ctx.currentUserId != null ? Number(ctx.currentUserId) : null;

                return [
                    {
                        formatter: "rowSelection",
                        titleFormatter: "rowSelection",
                        hozAlign: "center",
                        headerSort: false,
                        width: 42
                    },
                    { title: "Id", field: "id", width: 90, sorter: "number" },
                    { title: "用户名", field: "userName", minWidth: 120, sorter: "string" },
                    { title: "Email", field: "email", minWidth: 160, sorter: "string" },
                    { title: "电话", field: "phoneNumber", minWidth: 110, sorter: "string" },
                    { title: "归属部门", field: "departmentName", minWidth: 120, sorter: "string" },
                    { title: "角色", field: "rolesText", minWidth: 140, sorter: "string" },
                    {
                        title: "状态",
                        field: "statusText",
                        width: 100,
                        cssClass: "ev-user-status-col",
                        sorter: "string",
                        formatter: function (cell) {
                            var v = cell.getValue();
                            if (v === "正常") {
                                return "<span class=\"ev-user-status-pill ev-user-status--ok\">正常</span>";
                            }
                            return "<span class=\"ev-user-status-pill ev-user-status--off\">停用</span>";
                        }
                    },
                    { title: "更新日期", field: "updateTimeText", minWidth: 150, sorter: "string" },
                    { title: "更新人", field: "updateByText", minWidth: 110, sorter: "string" },
                    { title: "备注", field: "remark", minWidth: 140, sorter: "string" },
                    {
                        title: "操作",
                        field: "_op",
                        width: 220,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var userName = row.userName || "";
                            var disableDel = uid != null && id === uid;

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

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

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnUserTableEdit", id).catch(function (err) {
                                    console.error("OnUserTableEdit", err);
                                });
                            });

                            var pwdBtn = document.createElement("button");
                            pwdBtn.type = "button";
                            pwdBtn.className = "btn btn-outline-secondary";
                            pwdBtn.textContent = "重置密码";
                            wireClick(pwdBtn, function () {
                                dotNetRef.invokeMethodAsync("OnUserTablePassword", id, userName).catch(function (err) {
                                    console.error("OnUserTablePassword", err);
                                });
                            });

                            var delBtn = document.createElement("button");
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
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.id ?? ""),
                        String(data.userName ?? ""),
                        String(data.email ?? ""),
                        String(data.phoneNumber ?? ""),
                        String(data.departmentName ?? ""),
                        String(data.rolesText ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByText ?? ""),
                        String(data.remark ?? "")
                    ]
                        .join(" ")
                        .toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "users",
                sheetName: "Users",
                pdfTitle: "用户列表"
            }
        },
        products: {
            tabulatorOptions: {
                layout: "fitDataStretch",
                placeholder: "暂无数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "code", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    { formatter: "rowSelection", titleFormatter: "rowSelection", hozAlign: "center", headerSort: false, width: 42 },
                    { title: "商品编号", field: "id", width: 90, sorter: "number", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "商品代码", field: "code", minWidth: 120, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "商品名称", field: "name", minWidth: 140, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "商品类型", field: "productTypeCode", minWidth: 120, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "单位编号", field: "unitCode", minWidth: 100, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "商品条码", field: "barcode", minWidth: 120, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "品牌", field: "brand", minWidth: 100, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "型号", field: "model", minWidth: 100, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "成本单价", field: "unitCostText", minWidth: 90, sorter: "string", hozAlign: "right", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "建议售价", field: "suggestedPriceText", minWidth: 90, sorter: "string", hozAlign: "right", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "理论库存", field: "theoreticalStockText", minWidth: 90, sorter: "string", hozAlign: "right", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "实际库存", field: "actualStockText", minWidth: 90, sorter: "string", hozAlign: "right", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "警戒库存", field: "alertStockText", minWidth: 90, sorter: "string", hozAlign: "right", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "备注", field: "remark", minWidth: 120, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 90, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 150, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 100, sorter: "string", formatter: function (c) { return strike(c.getValue(), c.getRow().getData()); } },
                    {
                        title: "操作",
                        field: "_op",
                        width: 140,
                        frozen: true,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) {
                                    e.stopPropagation();
                                    e.preventDefault();
                                    if (!dotNetRef || btn.disabled) return;
                                    handler();
                                }, true);
                            }

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnProductTableEdit", id).catch(function (err) { console.error(err); });
                            });

                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () {
                                dotNetRef.invokeMethodAsync("OnProductTableDelete", id).catch(function (err) { console.error(err); });
                            });

                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.id ?? ""),
                        String(data.code ?? ""),
                        String(data.name ?? ""),
                        String(data.productTypeCode ?? ""),
                        String(data.unitCode ?? ""),
                        String(data.barcode ?? ""),
                        String(data.brand ?? ""),
                        String(data.model ?? ""),
                        String(data.unitCostText ?? ""),
                        String(data.suggestedPriceText ?? ""),
                        String(data.theoreticalStockText ?? ""),
                        String(data.actualStockText ?? ""),
                        String(data.alertStockText ?? ""),
                        String(data.remark ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "products",
                sheetName: "Products",
                pdfTitle: "商品信息"
            }
        },
        units: {
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "code", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;

                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    {
                        formatter: "rowSelection",
                        titleFormatter: "rowSelection",
                        hozAlign: "center",
                        headerSort: false,
                        width: 42
                    },
                    {
                        title: "单位编号",
                        field: "code",
                        minWidth: 140,
                        sorter: "string",
                        formatter: function (cell) {
                            return strike(cell.getValue(), cell.getRow().getData());
                        }
                    },
                    {
                        title: "单位名称",
                        field: "name",
                        minWidth: 170,
                        sorter: "string",
                        formatter: function (cell) {
                            return strike(cell.getValue(), cell.getRow().getData());
                        }
                    },
                    {
                        title: "是否激活",
                        field: "statusText",
                        width: 100,
                        sorter: "string",
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var v = row.isActive ? "是" : "否";
                            return strike(v, row);
                        }
                    },
                    {
                        title: "更新时间",
                        field: "updateTimeText",
                        minWidth: 170,
                        sorter: "string",
                        formatter: function (cell) {
                            return strike(cell.getValue(), cell.getRow().getData());
                        }
                    },
                    {
                        title: "更新人",
                        field: "updateByUserName",
                        minWidth: 120,
                        sorter: "string",
                        formatter: function (cell) {
                            return strike(cell.getValue(), cell.getRow().getData());
                        }
                    },
                    {
                        title: "操作",
                        field: "_op",
                        width: 140,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

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

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnUnitTableEdit", id).catch(function (err) {
                                    console.error("OnUnitTableEdit", err);
                                });
                            });

                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () {
                                dotNetRef.invokeMethodAsync("OnUnitTableDelete", id).catch(function (err) {
                                    console.error("OnUnitTableDelete", err);
                                });
                            });

                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.code ?? ""),
                        String(data.name ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ]
                        .join(" ")
                        .toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "units",
                sheetName: "Units",
                pdfTitle: "单位列表"
            }
        },
        customerCategories: {
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "categoryCode", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }
                return [
                    { formatter: "rowSelection", titleFormatter: "rowSelection", hozAlign: "center", headerSort: false, width: 42 },
                    { title: "类别代码", field: "categoryCode", minWidth: 130, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "类别名称", field: "name", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "备注", field: "remark", minWidth: 200, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { var r = cell.getRow().getData(); return strike(r.isActive ? "是" : "否", r); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作", field: "_op", width: 140, hozAlign: "left", headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");
                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) { e.stopPropagation(); e.preventDefault(); if (!dotNetRef || btn.disabled) return; handler(); }, true);
                            }
                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () { dotNetRef.invokeMethodAsync("OnCustomerCategoryTableEdit", id).catch(function (err) { console.error(err); }); });
                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () { dotNetRef.invokeMethodAsync("OnCustomerCategoryTableDelete", id).catch(function (err) { console.error(err); }); });
                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) { table.clearFilter(true); return; }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.categoryCode ?? ""),
                        String(data.name ?? ""),
                        String(data.remark ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "customer_categories",
                sheetName: "CustomerCategories",
                pdfTitle: "客户类别"
            }
        },
        customers: {
            tabulatorOptions: {
                layout: "fitDataStretch",
                placeholder: "暂无客户数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "name", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    { formatter: "rowSelection", titleFormatter: "rowSelection", hozAlign: "center", headerSort: false, width: 42 },
                    { title: "客户类别", field: "customerCategoryName", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "姓名", field: "name", minWidth: 130, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "性别", field: "gender", width: 80, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "生日", field: "birthdayText", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "职务", field: "jobTitle", minWidth: 110, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "手机", field: "phone", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "电子邮箱", field: "email", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "备注", field: "remark", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { var r = cell.getRow().getData(); return strike(r.isActive ? "是" : "否", r); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作", field: "_op", width: 140, hozAlign: "left", headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");
                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) { e.stopPropagation(); e.preventDefault(); if (!dotNetRef || btn.disabled) return; handler(); }, true);
                            }
                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () { dotNetRef.invokeMethodAsync("OnCustomerTableEdit", id).catch(function (err) { console.error(err); }); });
                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () { dotNetRef.invokeMethodAsync("OnCustomerTableDelete", id).catch(function (err) { console.error(err); }); });
                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) { table.clearFilter(true); return; }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.customerCategoryName ?? ""),
                        String(data.name ?? ""),
                        String(data.gender ?? ""),
                        String(data.birthdayText ?? ""),
                        String(data.jobTitle ?? ""),
                        String(data.phone ?? ""),
                        String(data.email ?? ""),
                        String(data.remark ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "customers",
                sheetName: "Customers",
                pdfTitle: "客户信息"
            }
        },
        suppliers: {
            tabulatorOptions: {
                layout: "fitDataStretch",
                placeholder: "暂无供应商数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "name", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    { formatter: "rowSelection", titleFormatter: "rowSelection", hozAlign: "center", headerSort: false, width: 42 },
                    { title: "供应商名称", field: "name", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "地址", field: "address", minWidth: 220, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "电话", field: "phone", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "网址", field: "website", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "备注", field: "remark", minWidth: 220, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { var r = cell.getRow().getData(); return strike(r.isActive ? "是" : "否", r); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作", field: "_op", width: 140, hozAlign: "left", headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");
                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) { e.stopPropagation(); e.preventDefault(); if (!dotNetRef || btn.disabled) return; handler(); }, true);
                            }
                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () { dotNetRef.invokeMethodAsync("OnSupplierTableEdit", id).catch(function (err) { console.error(err); }); });
                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () { dotNetRef.invokeMethodAsync("OnSupplierTableDelete", id).catch(function (err) { console.error(err); }); });
                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) { table.clearFilter(true); return; }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.name ?? ""),
                        String(data.address ?? ""),
                        String(data.phone ?? ""),
                        String(data.website ?? ""),
                        String(data.remark ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "suppliers",
                sheetName: "Suppliers",
                pdfTitle: "供应商信息"
            }
        },
        markets: {
            tabulatorOptions: {
                layout: "fitDataStretch",
                placeholder: "暂无市场数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "name", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    { formatter: "rowSelection", titleFormatter: "rowSelection", hozAlign: "center", headerSort: false, width: 42 },
                    { title: "市场名称", field: "name", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "租金", field: "rentAmountText", minWidth: 120, sorter: "string", hozAlign: "right", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "地址", field: "address", minWidth: 220, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "电话", field: "phone", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "网址", field: "website", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "备注", field: "remark", minWidth: 220, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { var r = cell.getRow().getData(); return strike(r.isActive ? "是" : "否", r); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作", field: "_op", width: 140, hozAlign: "left", headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");
                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) { e.stopPropagation(); e.preventDefault(); if (!dotNetRef || btn.disabled) return; handler(); }, true);
                            }
                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () { dotNetRef.invokeMethodAsync("OnMarketTableEdit", id).catch(function (err) { console.error(err); }); });
                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () { dotNetRef.invokeMethodAsync("OnMarketTableDelete", id).catch(function (err) { console.error(err); }); });
                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) { table.clearFilter(true); return; }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.name ?? ""),
                        String(data.rentAmountText ?? ""),
                        String(data.address ?? ""),
                        String(data.phone ?? ""),
                        String(data.website ?? ""),
                        String(data.remark ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "markets",
                sheetName: "Markets",
                pdfTitle: "市场管理"
            }
        },
        parameters: {
            tabulatorOptions: {
                layout: "fitDataStretch",
                placeholder: "暂无参数数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "paramKey", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }
                return [
                    { formatter: "rowSelection", titleFormatter: "rowSelection", hozAlign: "center", headerSort: false, width: 42 },
                    { title: "参数名称", field: "name", minWidth: 160, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "参数键名", field: "paramKey", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "参数值", field: "paramValue", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "系统内置", field: "builtInText", width: 100, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "备注", field: "remark", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { var r = cell.getRow().getData(); return strike(r.isActive ? "是" : "否", r); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作", field: "_op", width: 140, hozAlign: "left", headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");
                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) { e.stopPropagation(); e.preventDefault(); if (!dotNetRef || btn.disabled) return; handler(); }, true);
                            }
                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () { dotNetRef.invokeMethodAsync("OnParameterTableEdit", id).catch(function (err) { console.error(err); }); });
                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () { dotNetRef.invokeMethodAsync("OnParameterTableDelete", id).catch(function (err) { console.error(err); }); });
                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) { table.clearFilter(true); return; }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.name ?? ""),
                        String(data.paramKey ?? ""),
                        String(data.paramValue ?? ""),
                        String(data.builtInText ?? ""),
                        String(data.remark ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "system_parameters",
                sheetName: "Parameters",
                pdfTitle: "参数设置"
            }
        },
        tenants: {
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "name", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function statusChip(row) {
                    var txt = String(row.statusText || "");
                    if (row.statusText === "正常") {
                        return "<span class=\"ev-user-status-pill ev-user-status--ok\">" + txt + "</span>";
                    }
                    return "<span class=\"ev-user-status-pill ev-user-status--off\">" + txt + "</span>";
                }
                function strike(v, row) {
                    var text = String(v == null ? "" : v);
                    if (row && row.statusText !== "正常") {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    {
                        formatter: "rowSelection",
                        titleFormatter: "rowSelection",
                        hozAlign: "center",
                        headerSort: false,
                        width: 42
                    },
                    { title: "租户 ID", field: "tenantId", width: 90, sorter: "number", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "租户名称", field: "tenantName", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "根组织名称", field: "rootOrgName", minWidth: 120, sorter: "string" },
                    { title: "创建时间", field: "createTimeText", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "到期时间", field: "expireAtText", minWidth: 140, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "状态", field: "statusText", width: 90, cssClass: "ev-user-status-col", sorter: "string", formatter: function (cell) { return statusChip(cell.getRow().getData()); } },
                    { title: "管理员用户名", field: "adminUserName", minWidth: 120, sorter: "string" },
                    { title: "管理员邮箱", field: "adminEmail", minWidth: 160, sorter: "string" },
                    { title: "备注", field: "remark", minWidth: 160, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作",
                        field: "_op",
                        width: 200,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.tenantId);
                            var name = row.tenantName || "";
                            var canDelete = row.canDelete !== false;

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

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

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnTenantTableEdit", id).catch(function (err) {
                                    console.error("OnTenantTableEdit", err);
                                });
                            });

                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            delBtn.disabled = !canDelete;
                            wireClick(delBtn, function () {
                                dotNetRef.invokeMethodAsync("OnTenantTableDelete", id, name).catch(function (err) {
                                    console.error("OnTenantTableDelete", err);
                                });
                            });

                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.tenantId ?? ""),
                        String(data.tenantName ?? ""),
                        String(data.rootOrgName ?? ""),
                        String(data.statusText ?? ""),
                        String(data.createTimeText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.expireAtText ?? ""),
                        String(data.adminUserName ?? ""),
                        String(data.adminEmail ?? ""),
                        String(data.remark ?? "")
                    ]
                        .join(" ")
                        .toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "tenants",
                sheetName: "Tenants",
                pdfTitle: "租户列表"
            }
        },
        roles: {
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: true,
                paginationSize: 10,
                paginationSizeSelector: [10, 15, 25, 50, 100],
                paginationCounter: "rows",
                selectable: true,
                initialSort: [{ column: "name", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;

                return [
                    {
                        formatter: "rowSelection",
                        titleFormatter: "rowSelection",
                        hozAlign: "center",
                        headerSort: false,
                        width: 42
                    },
                    { title: "Id", field: "id", width: 90, sorter: "number" },
                    { title: "角色名称", field: "name", minWidth: 120, sorter: "string" },
                    { title: "规范化名称", field: "normalizedName", minWidth: 120, sorter: "string" },
                    { title: "租户 Id", field: "tenantId", width: 90, sorter: "number" },
                    { title: "组织 Id", field: "orgId", width: 90, sorter: "number" },
                    { title: "组织名称", field: "orgName", minWidth: 120, sorter: "string" },
                    { title: "更新人", field: "updateByText", width: 90, sorter: "string" },
                    { title: "更新时间", field: "updateTimeText", minWidth: 150, sorter: "string" },
                    {
                        title: "操作",
                        field: "_op",
                        width: 140,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

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

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnRoleTableEdit", id).catch(function (err) {
                                    console.error("OnRoleTableEdit", err);
                                });
                            });

                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () {
                                dotNetRef.invokeMethodAsync("OnRoleTableDelete", id).catch(function (err) {
                                    console.error("OnRoleTableDelete", err);
                                });
                            });

                            wrap.appendChild(editBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.id ?? ""),
                        String(data.name ?? ""),
                        String(data.normalizedName ?? ""),
                        String(data.tenantId ?? ""),
                        String(data.orgId ?? ""),
                        String(data.orgName ?? ""),
                        String(data.updateByText ?? ""),
                        String(data.updateTimeText ?? "")
                    ]
                        .join(" ")
                        .toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "roles",
                sheetName: "Roles",
                pdfTitle: "角色列表"
            }
        },
        organizations: {
            revision: 3,
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无组织数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: false,
                autoColumns: false,
                dataTree: true,
                dataTreeChildField: "_children",
                dataTreeElementColumn: "name",
                dataTreeStartExpanded: true,
                dataTreeChildIndent: 20,
                selectable: false,
                movableRows: true,
                initialSort: [{ column: "name", dir: "asc" }]
            },
            afterCreateTable: function (table, ctx) {
                var dotNetRef = ctx.dotNetRef;
                if (!dotNetRef) return;
                var ignoreRowMoved = true;
                setTimeout(function () {
                    ignoreRowMoved = false;
                }, 400);
                table.on("rowMoved", function (row) {
                    if (ignoreRowMoved) return;
                    try {
                        var data = row.getData();
                        var id = Number(data.id);
                        var parentRow = row.getTreeParent();
                        var newParentId = parentRow ? Number(parentRow.getData().id) : null;
                        var prevRow = typeof row.getPrevRow === "function" ? row.getPrevRow() : null;
                        var dropTargetId = prevRow ? Number(prevRow.getData().id) : null;
                        dotNetRef
                            .invokeMethodAsync("OnOrgTableMoved", id, newParentId, dropTargetId)
                            .catch(function (err) {
                                console.error("OnOrgTableMoved", err);
                            });
                    } catch (e) {
                        console.error("organizations rowMoved", e);
                    }
                });
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    {
                        title: "组织名称",
                        field: "name",
                        minWidth: 220,
                        headerSort: false,
                        widthGrow: 2,
                        formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); }
                    },
                    { title: "状态", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "修改时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作",
                        field: "_op",
                        width: 200,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var name = row.name || "";

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

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

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "修改";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnOrgTableEdit", id).catch(function (err) {
                                    console.error("OnOrgTableEdit", err);
                                });
                            });

                            var addBtn = document.createElement("button");
                            addBtn.type = "button";
                            addBtn.className = "btn btn-outline-secondary";
                            addBtn.textContent = "新增";
                            wireClick(addBtn, function () {
                                dotNetRef.invokeMethodAsync("OnOrgTableAddChild", id, name).catch(function (err) {
                                    console.error("OnOrgTableAddChild", err);
                                });
                            });

                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () {
                                dotNetRef.invokeMethodAsync("OnOrgTableDelete", id, name).catch(function (err) {
                                    console.error("OnOrgTableDelete", err);
                                });
                            });

                            wrap.appendChild(editBtn);
                            wrap.appendChild(addBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.name ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? "")
                    ]
                        .join(" ")
                        .toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "organizations",
                sheetName: "Organizations",
                pdfTitle: "组织架构"
            }
        },
        productCategories: {
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无商品类别数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: false,
                dataTree: true,
                dataTreeChildField: "_children",
                dataTreeElementColumn: "name",
                dataTreeStartExpanded: true,
                dataTreeChildIndent: 20,
                selectable: false,
                initialSort: [{ column: "code", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;
                function strike(v, rowData) {
                    var text = String(v == null ? "" : v);
                    if (rowData && rowData.isActive === false) {
                        return "<span class=\"text-decoration-line-through text-muted\">" + text + "</span>";
                    }
                    return text;
                }

                return [
                    { title: "类别编号", field: "code", minWidth: 130, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "类别名称", field: "name", minWidth: 180, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "是否激活", field: "statusText", width: 100, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新时间", field: "updateTimeText", minWidth: 170, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    { title: "更新人", field: "updateByUserName", minWidth: 120, sorter: "string", formatter: function (cell) { return strike(cell.getValue(), cell.getRow().getData()); } },
                    {
                        title: "操作",
                        field: "_op",
                        width: 220,
                        hozAlign: "left",
                        headerSort: false,
                        formatter: function (cell) {
                            var row = cell.getRow().getData();
                            var id = Number(row.id);
                            var name = row.name || "";

                            var wrap = document.createElement("div");
                            wrap.className = "btn-group btn-group-sm";
                            wrap.setAttribute("role", "group");

                            function wireClick(btn, handler) {
                                btn.addEventListener("click", function (e) {
                                    e.stopPropagation();
                                    e.preventDefault();
                                    if (!dotNetRef || btn.disabled) return;
                                    handler();
                                }, true);
                            }

                            var editBtn = document.createElement("button");
                            editBtn.type = "button";
                            editBtn.className = "btn btn-outline-primary";
                            editBtn.textContent = "编辑";
                            wireClick(editBtn, function () {
                                dotNetRef.invokeMethodAsync("OnProductCategoryTableEdit", id).catch(function (err) { console.error(err); });
                            });

                            var addBtn = document.createElement("button");
                            addBtn.type = "button";
                            addBtn.className = "btn btn-outline-secondary";
                            addBtn.textContent = "新增子级";
                            wireClick(addBtn, function () {
                                dotNetRef.invokeMethodAsync("OnProductCategoryTableAddChild", id, name).catch(function (err) { console.error(err); });
                            });

                            var delBtn = document.createElement("button");
                            delBtn.type = "button";
                            delBtn.className = "btn btn-outline-danger";
                            delBtn.textContent = "删除";
                            wireClick(delBtn, function () {
                                dotNetRef.invokeMethodAsync("OnProductCategoryTableDelete", id, name).catch(function (err) { console.error(err); });
                            });

                            wrap.appendChild(editBtn);
                            wrap.appendChild(addBtn);
                            wrap.appendChild(delBtn);
                            return wrap;
                        }
                    }
                ];
            },
            applyGlobalFilter: function (table, text) {
                var t = (text || "").trim().toLowerCase();
                if (!t) {
                    table.clearFilter(true);
                    return;
                }
                table.setFilter(function (data) {
                    var hay = [
                        String(data.code ?? ""),
                        String(data.name ?? ""),
                        String(data.statusText ?? ""),
                        String(data.updateTimeText ?? ""),
                        String(data.updateByUserName ?? "")
                    ].join(" ").toLowerCase();
                    return hay.indexOf(t) !== -1;
                });
            },
            download: {
                filePrefix: "product_categories",
                sheetName: "ProductCategories",
                pdfTitle: "商品类别"
            }
        }
    };

    function columnVisibilityStorageKey(schemaKey, userId) {
        var uid = userId != null && userId !== "" ? String(userId) : "anon";
        return "evolver.colvis." + schemaKey + "." + uid;
    }

    function loadColumnVisibility(schemaKey, userId) {
        try {
            var raw = localStorage.getItem(columnVisibilityStorageKey(schemaKey, userId));
            if (!raw) return {};
            var parsed = JSON.parse(raw);
            return parsed && typeof parsed === "object" ? parsed : {};
        } catch (e) {
            return {};
        }
    }

    function saveColumnVisibility(schemaKey, userId, map) {
        try {
            localStorage.setItem(columnVisibilityStorageKey(schemaKey, userId), JSON.stringify(map || {}));
        } catch (e) {
            console.warn("saveColumnVisibility", e);
        }
    }

    function isToggleableColumnDef(col) {
        if (!col || !col.field || col.field === "_op") return false;
        if (col.formatter === "rowSelection" || col.titleFormatter === "rowSelection") return false;
        return true;
    }

    function applyStoredColumnVisibility(cols, schemaKey, userId) {
        if (!cols || !cols.length) return cols;
        var stored = loadColumnVisibility(schemaKey, userId);
        return cols.map(function (col) {
            if (!isToggleableColumnDef(col)) return col;
            if (stored[col.field] === false) {
                return Object.assign({}, col, { visible: false });
            }
            return col;
        });
    }

    function columnIsVisible(col) {
        if (!col) return true;
        try {
            if (typeof col.isVisible === "function") return !!col.isVisible();
            if (typeof col.getVisibility === "function") return !!col.getVisibility();
            var def = col.getDefinition && col.getDefinition();
            return !(def && def.visible === false);
        } catch (e) {
            return true;
        }
    }

    function buildToggleableColumnList(hostId, schemaKey) {
        schemaKey = schemaKey || (getInst(hostId) && getInst(hostId).schemaKey) || "users";
        var def = schemas[schemaKey];
        if (!def || typeof def.buildColumns !== "function") return [];

        var inst = getInst(hostId);
        var ctx = {
            hostId: hostId,
            dotNetRef: inst && inst.dotNetRef,
            currentUserId: inst && inst.currentUserId
        };
        var colDefs = def.buildColumns(ctx);
        var stored = loadColumnVisibility(schemaKey, ctx.currentUserId);
        var visibilityByField = Object.create(null);

        if (inst && inst.table && isTableUsable(inst.table)) {
            try {
                inst.table.getColumns().forEach(function (col) {
                    var d = col.getDefinition();
                    if (!isToggleableColumnDef(d)) return;
                    visibilityByField[d.field] = columnIsVisible(col);
                });
            } catch (e) {
                console.warn("buildToggleableColumnList", e);
            }
        }

        return colDefs.filter(isToggleableColumnDef).map(function (colDef) {
            var field = colDef.field;
            var visible = visibilityByField[field];
            if (visible === undefined) visible = stored[field] !== false;
            return {
                field: field,
                title: colDef.title || field,
                visible: !!visible
            };
        });
    }

    function createTable(hostId, el, rows, dnRef, schemaKey, currentUserId) {
        var def = schemas[schemaKey];
        if (!def) {
            console.warn("evolverDataTable: unknown schema", schemaKey);
            return;
        }

        var ctx = { hostId: hostId, dotNetRef: dnRef, currentUserId: currentUserId };
        var cols = applyStoredColumnVisibility(def.buildColumns(ctx), schemaKey, currentUserId);
        var opts = Object.assign({}, def.tabulatorOptions, {
            data: rows,
            columns: cols
        });

        var tab = new Tabulator(el, opts);

        if (typeof def.afterCreateTable === "function") {
            def.afterCreateTable(tab, ctx);
        }

        instances[hostId] = {
            table: tab,
            hostEl: el,
            dotNetRef: dnRef,
            schemaKey: schemaKey,
            schemaRevision: def.revision || 0,
            currentUserId: currentUserId,
            resizeObserver: null,
            _onWinResize: null
        };

        attachWindowResizeRedraw(hostId);
    }

    function isTableUsable(table) {
        if (!table) return false;
        try {
            // Tabulator destroyed/half-destroyed instances can keep an object but lose modules.
            return !!(table.modules && table.modules.rowManager);
        } catch (e) {
            return false;
        }
    }

    window.evolverDataTable = {
        /** 注册新业务的列与 Tabulator 默认项（可选，也可直接改本文件 schemas） */
        registerSchema: function (key, schemaDef) {
            if (!key || !schemaDef) return;
            schemas[key] = schemaDef;
        },

        sync: function (hostId, json, dnRef, schemaKey, currentUserId) {
            var el = typeof hostId === "string" ? document.getElementById(hostId) : hostId;
            if (!el) {
                console.warn("evolverDataTable: host not found", hostId);
                return false;
            }

            schemaKey = schemaKey || "users";

            var rows;
            try {
                rows = typeof json === "string" ? JSON.parse(json) : json;
            } catch (e) {
                console.error(e);
                return false;
            }

            var def = schemas[schemaKey];
            var schemaRevision = def && def.revision ? def.revision : 0;

            var inst = getInst(hostId);
            if (!inst) {
                createTable(hostId, el, rows, dnRef, schemaKey, currentUserId);
                return true;
            }
            if (inst.hostEl !== el) {
                window.evolverDataTable.destroy(hostId);
                createTable(hostId, el, rows, dnRef, schemaKey, currentUserId);
                return true;
            }

            inst.dotNetRef = dnRef;
            inst.currentUserId = currentUserId;
            inst.schemaKey = schemaKey;
            if (!isTableUsable(inst.table) || inst.schemaRevision !== schemaRevision) {
                window.evolverDataTable.destroy(hostId);
                createTable(hostId, el, rows, dnRef, schemaKey, currentUserId);
                return true;
            }
            try {
                inst.table.replaceData(rows);
            } catch (e) {
                console.warn("evolverDataTable sync replaceData failed, rebuilding", e);
                window.evolverDataTable.destroy(hostId);
                createTable(hostId, el, rows, dnRef, schemaKey, currentUserId);
                return true;
            }
            requestAnimationFrame(function () {
                redrawTable(hostId);
            });
            return true;
        },

        setGlobalFilter: function (hostId, text) {
            var inst = getInst(hostId);
            if (!inst || !inst.table) return;
            var def = schemas[inst.schemaKey];
            if (!def || typeof def.applyGlobalFilter !== "function") return;
            def.applyGlobalFilter(inst.table, text);
        },

        destroy: function (hostId) {
            var inst = getInst(hostId);
            if (!inst) return;
            disconnectHostResizeObserver(inst);
            detachWindowResizeRedraw(inst);
            if (inst.table) {
                try {
                    inst.table.destroy();
                } catch (e) {
                    console.warn("evolverDataTable destroy", e);
                }
                inst.table = null;
            }
            delete instances[hostId];
        },

        destroyAll: function () {
            Object.keys(instances).forEach(function (id) {
                window.evolverDataTable.destroy(id);
            });
        },

        download: function (hostId, format) {
            var inst = getInst(hostId);
            if (!inst || !inst.table) return;
            var def = schemas[inst.schemaKey];
            var dl = (def && def.download) || { filePrefix: "export", sheetName: "Sheet1", pdfTitle: "列表" };
            var stamp = new Date().toISOString().slice(0, 19).replace(/[-:T]/g, "");
            var prefix = dl.filePrefix || "export";

            if (format === "csv") {
                inst.table.download("csv", prefix + "_" + stamp + ".csv", { bom: true });
            } else if (format === "xlsx") {
                if (typeof XLSX === "undefined") {
                    alert("未加载 SheetJS，无法导出 Excel。请刷新页面重试。");
                    return;
                }
                inst.table.download("xlsx", prefix + "_" + stamp + ".xlsx", { sheetName: dl.sheetName || "Sheet1" });
            } else if (format === "pdf") {
                syncJsPdfGlobal();
                var pdfCtor = typeof window.jsPDF !== "undefined" ? window.jsPDF : window.jspdf && window.jspdf.jsPDF;
                if (typeof pdfCtor === "undefined") {
                    alert("未加载 jsPDF，无法导出 PDF。请刷新页面重试。");
                    return;
                }
                var pdfTitle = dl.pdfTitle || "列表";
                ensurePdfCjkFontBase64()
                    .then(function (b64) {
                        inst.table.download("pdf", prefix + "_" + stamp + ".pdf", {
                            orientation: "landscape",
                            autoTable: function (doc) {
                                registerPdfCjkFont(doc, b64);
                                return buildPdfAutoTableOptions(doc, pdfTitle);
                            }
                        });
                    })
                    .catch(function (e) {
                        console.error(e);
                        alert("导出 PDF 失败：" + (e && e.message ? e.message : String(e)));
                    });
            }
        },

        getToggleableColumns: function (hostId, schemaKey) {
            return buildToggleableColumnList(hostId, schemaKey);
        },

        setColumnVisible: function (hostId, field, visible) {
            var inst = getInst(hostId);
            if (!inst || !inst.table || !field) return false;
            try {
                if (visible) inst.table.showColumn(field);
                else inst.table.hideColumn(field);
                var map = loadColumnVisibility(inst.schemaKey, inst.currentUserId);
                map[field] = !!visible;
                saveColumnVisibility(inst.schemaKey, inst.currentUserId, map);
                return true;
            } catch (e) {
                console.warn("setColumnVisible", e);
                return false;
            }
        },

        setAllColumnsVisible: function (hostId, visible) {
            var inst = getInst(hostId);
            if (!inst || !inst.table) return [];
            var map = loadColumnVisibility(inst.schemaKey, inst.currentUserId);
            inst.table.getColumns().forEach(function (col) {
                var def = col.getDefinition();
                if (!isToggleableColumnDef(def)) return;
                if (visible) inst.table.showColumn(def.field);
                else inst.table.hideColumn(def.field);
                map[def.field] = !!visible;
            });
            saveColumnVisibility(inst.schemaKey, inst.currentUserId, map);
            return buildToggleableColumnList(hostId, inst.schemaKey);
        },

        expandTreeAll: function (hostId) {
            var inst = getInst(hostId);
            if (!inst || !inst.table) return;
            function expandDeep(row) {
                try {
                    if (typeof row.treeExpand === "function") row.treeExpand();
                    var ch = row.getTreeChildren();
                    if (ch && ch.length) {
                        ch.forEach(expandDeep);
                    }
                } catch (e) {
                    console.warn("expandTreeAll", e);
                }
            }
            inst.table.getRows().forEach(expandDeep);
            redrawTable(hostId);
        },

        collapseTreeAll: function (hostId) {
            var inst = getInst(hostId);
            if (!inst || !inst.table) return;
            function collapseDeep(row) {
                try {
                    var ch = row.getTreeChildren();
                    if (ch && ch.length) {
                        ch.forEach(collapseDeep);
                    }
                    if (typeof row.treeCollapse === "function") row.treeCollapse();
                } catch (e) {
                    console.warn("collapseTreeAll", e);
                }
            }
            inst.table.getRows().forEach(collapseDeep);
            redrawTable(hostId);
        }
    };
})();
