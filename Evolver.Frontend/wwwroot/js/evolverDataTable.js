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
                        String(data.rolesText ?? ""),
                        String(data.statusText ?? "")
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

                return [
                    {
                        formatter: "rowSelection",
                        titleFormatter: "rowSelection",
                        hozAlign: "center",
                        headerSort: false,
                        width: 42
                    },
                    { title: "租户 Id", field: "id", width: 90, sorter: "number" },
                    { title: "租户名称", field: "name", minWidth: 140, sorter: "string" },
                    { title: "根组织名称", field: "rootOrgName", minWidth: 120, sorter: "string" },
                    { title: "管理员用户名", field: "adminUserName", minWidth: 120, sorter: "string" },
                    { title: "管理员邮箱", field: "adminEmail", minWidth: 160, sorter: "string" },
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
                        String(data.id ?? ""),
                        String(data.name ?? ""),
                        String(data.rootOrgName ?? ""),
                        String(data.adminUserName ?? ""),
                        String(data.adminEmail ?? "")
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
            tabulatorOptions: {
                layout: "fitColumns",
                placeholder: "暂无组织数据",
                maxHeight: "calc(100vh - 220px)",
                pagination: false,
                dataTree: true,
                dataTreeChildField: "_children",
                dataTreeElementColumn: "name",
                dataTreeStartExpanded: true,
                dataTreeChildIndent: 20,
                selectable: false,
                initialSort: [{ column: "id", dir: "asc" }]
            },
            buildColumns: function (ctx) {
                var dotNetRef = ctx.dotNetRef;

                return [
                    {
                        title: "组织名称",
                        field: "name",
                        minWidth: 220,
                        headerSort: false,
                        widthGrow: 2
                    },
                    { title: "类型", field: "orgType", minWidth: 100, sorter: "string" },
                    { title: "Id", field: "id", width: 90, sorter: "number" },
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
                                dotNetRef.invokeMethodAsync("OnOrgTableEdit", id).catch(function (err) {
                                    console.error("OnOrgTableEdit", err);
                                });
                            });

                            var addBtn = document.createElement("button");
                            addBtn.type = "button";
                            addBtn.className = "btn btn-outline-secondary";
                            addBtn.textContent = "新增子级";
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
                    var hay = [String(data.id ?? ""), String(data.name ?? ""), String(data.orgType ?? "")]
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
        }
    };

    function createTable(hostId, el, rows, dnRef, schemaKey, currentUserId) {
        var def = schemas[schemaKey];
        if (!def) {
            console.warn("evolverDataTable: unknown schema", schemaKey);
            return;
        }

        var ctx = { hostId: hostId, dotNetRef: dnRef, currentUserId: currentUserId };
        var cols = def.buildColumns(ctx);
        var opts = Object.assign({}, def.tabulatorOptions, {
            data: rows,
            columns: cols
        });

        var tab = new Tabulator(el, opts);

        instances[hostId] = {
            table: tab,
            hostEl: el,
            dotNetRef: dnRef,
            schemaKey: schemaKey,
            currentUserId: currentUserId,
            resizeObserver: null,
            _onWinResize: null
        };

        attachWindowResizeRedraw(hostId);
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
                return;
            }

            schemaKey = schemaKey || "users";

            var rows;
            try {
                rows = typeof json === "string" ? JSON.parse(json) : json;
            } catch (e) {
                console.error(e);
                return;
            }

            var inst = getInst(hostId);
            if (!inst) {
                createTable(hostId, el, rows, dnRef, schemaKey, currentUserId);
                return;
            }
            if (inst.hostEl !== el) {
                window.evolverDataTable.destroy(hostId);
                createTable(hostId, el, rows, dnRef, schemaKey, currentUserId);
                return;
            }

            inst.dotNetRef = dnRef;
            inst.currentUserId = currentUserId;
            inst.schemaKey = schemaKey;
            inst.table.replaceData(rows);
            requestAnimationFrame(function () {
                redrawTable(hostId);
            });
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
                inst.table.destroy();
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
