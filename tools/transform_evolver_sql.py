"""
Transforms doc/3.design/Evolver.sql:
- PascalCase identifiers (no underscores)
- Plural-friendly PascalCase table names (explicit map)
- money + monetary columns -> decimal(19,4)
- FOREIGN KEY constraints inlined in CREATE TABLE
- UNIQUE on selected business alternate keys
- MS_Description for every table and column (existing Chinese preserved; gaps filled)
"""
from __future__ import annotations

import re
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "doc" / "3.design" / "Evolver.sql"

TABLE_MAP: dict[str, str] = {
    "Account_Payables": "AccountPayables",
    "Account_Receivables": "AccountReceivables",
    "Accounts": "Accounts",
    "Action_Logs": "ActionLogs",
    "Announcements": "Announcements",
    "Currencies": "Currencies",
    "Customer_Categories": "CustomerCategories",
    "Customer_Commissions": "CustomerCommissions",
    "Customer_Contacts": "CustomerContacts",
    "Customer_Notes": "CustomerNotes",
    "Customer_Relationships": "CustomerRelationships",
    "Customer_Settlement_Notes": "CustomerSettlementNotes",
    "Customer_Shares": "CustomerShares",
    "Customers": "Customers",
    "Departments": "Departments",
    "Dictionary_Table_Items": "DictionaryTableItems",
    "Dictionary_Tables": "DictionaryTables",
    "Error_Logs": "ErrorLogs",
    "Features": "Features",
    "Functions": "Functions",
    "Lookup_Table_Items": "LookupTableItems",
    "Lookup_Tables": "LookupTables",
    "Order_Action_Logs": "OrderActionLogs",
    "Order_Details": "OrderDetails",
    "Orders": "Orders",
    "Product_Categories": "ProductCategories",
    "Product_Components": "ProductComponents",
    "Product_List_Details": "ProductListDetails",
    "Product_Lists": "ProductLists",
    "Product_Prepare_Details": "ProductPrepareDetails",
    "Product_Prepares": "ProductPrepares",
    "Production_Details": "ProductionDetails",
    "Productions": "Productions",
    "Products": "Products",
    "Purchase_Details": "PurchaseDetails",
    "Purchases": "Purchases",
    "Quality_Control_Details": "QualityControlDetails",
    "Quality_Control_Template_Details": "QualityControlTemplateDetails",
    "Quality_Control_Templates": "QualityControlTemplates",
    "Quality_Controls": "QualityControls",
    "Question_Categories": "QuestionCategories",
    "Question_Values": "QuestionValues",
    "Questions": "Questions",
    "Repair_Service_Details": "RepairServiceDetails",
    "Repair_Service_Prechecks": "RepairServicePrechecks",
    "Repair_Services": "RepairServices",
    "Report_Parameters": "ReportParameters",
    "Reports": "Reports",
    "Role_Features": "RoleFeatures",
    "Role_Functions": "RoleFunctions",
    "Roles": "Roles",
    "Schema_Columns": "SchemaColumns",
    "Schema_Tables": "SchemaTables",
    "Service_Details": "ServiceDetails",
    "Service_Settlements": "ServiceSettlements",
    "Services": "Services",
    "Stock_In_Details": "StockInDetails",
    "Stock_Ins": "StockIns",
    "Stock_Out_Details": "StockOutDetails",
    "Stock_Outs": "StockOuts",
    "Supplier_Categories": "SupplierCategories",
    "Suppliers": "Suppliers",
    "System_Parameters": "SystemParameters",
    "Team_Users": "TeamUsers",
    "Teams": "Teams",
    "Units": "Units",
    "Users": "Users",
}

RE_CREATE = re.compile(
    r"CREATE TABLE \[dbo\]\.\[(?P<name>[^\]]+)\]\((?P<body>[\s\S]*?)^\)\s*ON \[PRIMARY\]\s*\r?\nGO",
    re.MULTILINE,
)


def underscore_to_pascal(raw: str) -> str:
    s = raw.replace("Detaill_Id", "Detail_Id").replace("Detaill", "Detail")
    parts = [p for p in s.split("_") if p != ""]
    out: list[str] = []
    for i, part in enumerate(parts):
        lp = part.lower()
        if lp == "id" and i > 0:
            out.append("Id")
            continue
        if part.isupper() and 2 <= len(part) <= 4 and part.isalpha():
            out.append(part if len(part) <= 3 else part[0] + part[1:].lower())
            continue
        if len(part) == 1:
            out.append(part.upper())
        else:
            out.append(part[0].upper() + part[1:].lower())
    return "".join(out)


def map_identifier(old: str) -> str:
    if old in TABLE_MAP:
        return TABLE_MAP[old]
    return underscore_to_pascal(old)


def is_monetary_column(pascal_name: str, sql_type: str) -> bool:
    t = sql_type.lower()
    if "money" in t:
        return True
    if "decimal" not in t:
        return False
    n = pascal_name
    if n == "Rate":
        return False
    if "Commission" in n and "Rate" in n:
        return True
    if n in (
        "TotalSettlement",
        "TotalAmount",
        "ExpectAmount",
        "ActualAmount",
        "InitialAmount",
        "EstimatedAmount",
        "ActualCost",
        "UnitPrice",
        "Amount",
        "Balance",
        "TotalTax",
        "TotalFreight",
        "InitialPrice",
        "SuggestedPrice",
        "CostPrice",
    ):
        return True
    for suf in ("Amount", "Balance", "Price", "Cost", "Settlement", "Freight", "Tax"):
        if n.endswith(suf):
            return True
    return False


def normalize_type(col_pascal: str, typ: str) -> str:
    t = typ.strip()
    if re.match(r"\[money\]", t, re.I):
        return "[decimal](19, 4)"
    if is_monetary_column(col_pascal, t) and re.search(r"\[decimal\]\s*\(\s*\d+\s*,\s*\d+\s*\)", t, re.I):
        return "[decimal](19, 4)"
    return t


COL_LINE_RE = re.compile(
    r"^\s*\[(?P<col>[^\]]+)\]\s*(?P<typ>\[[^\]]+\](?:\s*\([^)]*\))?)\s*(?P<rest>.*)$"
)


def parse_fk_alters(sql: str) -> list[dict[str, str]]:
    out: list[dict[str, str]] = []
    for m in re.finditer(
        r"ALTER TABLE \[dbo\]\.\[(?P<ft>[^\]]+)\]\s+WITH CHECK ADD\s+CONSTRAINT \[(?P<fk>[^\]]+)\] FOREIGN KEY\(\[(?P<fc>[^\]]+)\]\)\s*\n"
        r"REFERENCES \[dbo\]\.\[(?P<tt>[^\]]+)\] \(\[(?P<tc>[^\]]+)\]\)",
        sql,
        re.I,
    ):
        out.append(
            {
                "from_table": m.group("ft"),
                "name_old": m.group("fk"),
                "from_col": m.group("fc"),
                "to_table": m.group("tt"),
                "to_col": m.group("tc"),
            }
        )
    return out


def parse_extended_props(sql: str) -> dict[tuple[str, str | None], str]:
    desc_map: dict[tuple[str, str | None], str] = {}
    for m in re.finditer(
        r"EXEC sys\.sp_addextendedproperty @name=N'MS_Description',\s*@value=N'(?P<val>(?:[^']|'')*)'\s*,"
        r".*?@level1name=N'(?P<t>[^']+)'(?:\s*,\s*@level2type=N'COLUMN'\s*,\s*@level2name=N'(?P<c>[^']+)')?",
        sql,
        re.S,
    ):
        val = m.group("val").replace("''", "'")
        t_old = m.group("t")
        c_old = m.group("c")
        desc_map[(t_old, c_old)] = val
    return desc_map


def humanize_fallback(pascal: str) -> str:
    import re as _re

    s = _re.sub(r"([a-z])([A-Z])", r"\1 \2", pascal)
    return s.strip()


def split_table_inner(inner: str) -> tuple[list[str], str]:
    pk_match = re.search(r"CONSTRAINT \[PK_[^\]]+\] PRIMARY KEY CLUSTERED", inner, re.I)
    if not pk_match:
        raise RuntimeError("PRIMARY KEY not found")
    cols_part = inner[: pk_match.start()].strip().rstrip(",")
    pk_part = inner[pk_match.start() :].strip()
    lines = [ln.strip() for ln in cols_part.splitlines() if ln.strip()]
    return lines, pk_part


def parse_columns(lines: list[str]) -> list[dict[str, str]]:
    cols: list[dict[str, str]] = []
    buf = ""
    for ln in lines:
        if not buf:
            buf = ln
        else:
            buf += " " + ln.strip()
        if buf.rstrip().endswith(","):
            entry = buf.rstrip().rstrip(",")
            cols.append(parse_col_line(entry))
            buf = ""
    if buf.strip():
        cols.append(parse_col_line(buf.strip().rstrip(",")))
    return cols


def parse_col_line(line: str) -> dict[str, str]:
    m = COL_LINE_RE.match(line)
    if not m:
        raise RuntimeError(f"Bad column line: {line[:200]}")
    return {"old": m.group("col"), "type": m.group("typ"), "rest": m.group("rest").strip()}


def rebuild_pk_constraint(pk_part: str, new_table: str) -> str:
    """Remap PK constraint name and clustered key columns only (do not touch ON [PRIMARY])."""
    pk_name = f"PK{new_table}"
    pk_part = re.sub(r"CONSTRAINT \[PK_[^\]]+\]", f"CONSTRAINT [{pk_name}]", pk_part, flags=re.I)
    pk_part = re.sub(
        r"\[([^\]]+)\](\s+ASC)",
        lambda m: f"[{map_identifier(m.group(1))}]{m.group(2)}",
        pk_part,
        flags=re.I,
    )
    return pk_part


def extra_fk_candidates() -> list[tuple[str, str, str, str]]:
    """(from_table_old, from_col_old, to_table_old, to_col_old)."""
    rules: list[tuple[str, str, str, str]] = []

    def add(ft: str, fc: str, tt: str, tc: str) -> None:
        rules.append((ft, fc, tt, tc))

    for t in TABLE_MAP:
        if t == "Customers":
            continue
        if t == "Customer_Relationships":
            add(t, "Parent_Customer_Id", "Customers", "Customer_Id")
            add(t, "Customer_Id", "Customers", "Customer_Id")
            continue
        if t == "Customer_Commissions":
            add(t, "Customer_Id", "Customers", "Customer_Id")
            add(t, "Parent_Customer_Id", "Customers", "Customer_Id")
            continue
        add(t, "Customer_Id", "Customers", "Customer_Id")

    for t in TABLE_MAP:
        add(t, "Assigned_Team_id", "Teams", "Team_Id")

    product_tables = [
        "Account_Payables",
        "Account_Receivables",
        "Order_Details",
        "Product_Components",
        "Product_List_Details",
        "Product_Prepare_Details",
        "Production_Details",
        "Purchase_Details",
        "Quality_Control_Templates",
        "Quality_Controls",
        "Repair_Services",
        "Service_Details",
        "Stock_In_Details",
        "Stock_Out_Details",
    ]
    for t in product_tables:
        add(t, "Product_Code", "Products", "Product_Code")

    add("Product_Prepares", "Order_Code", "Orders", "Order_Code")

    user_cols = [
        "Assigned_User_Code",
        "Invoice_User_Code",
        "Settlement_User_Code",
        "Shipping_User_Code",
        "Receiving_User_Code",
        "Verification_User_Code",
        "Prepare_User_Code",
        "Apply_User_Code",
        "Assistant_User_Code",
        "Service_User_Code",
        "Check_User_Code",
        "Receive_User_Code",
        "Production_User_Code",
    ]
    for t in TABLE_MAP:
        for c in user_cols:
            add(t, c, "Users", "User_Code")

    for t in TABLE_MAP:
        add(t, "User_Code", "Users", "User_Code")

    add("Teams", "Parent_Id", "Teams", "Team_Id")

    # Account/AR customer links (legacy script omitted)
    return rules


def merge_fks(
    base: list[dict[str, str]],
    extras: list[tuple[str, str, str, str]],
    colsets: dict[str, set[str]],
) -> list[dict[str, str]]:
    seen: set[tuple[str, str, str, str]] = set()
    out: list[dict[str, str]] = []

    def push(ft: str, fc: str, tt: str, tc: str) -> None:
        if fc not in colsets.get(ft, set()):
            return
        if tt not in TABLE_MAP:
            return
        # Products.Product_Code: PK; skip self-reference
        if ft == "Products" and fc == "Product_Code":
            return
        if ft == tt and fc == tc:
            return
        key = (ft, fc, tt, tc)
        if key in seen:
            return
        seen.add(key)
        out.append({"from_table": ft, "from_col": fc, "to_table": tt, "to_col": tc})

    for fk in base:
        push(fk["from_table"], fk["from_col"], fk["to_table"], fk["to_col"])
    for ft, fc, tt, tc in extras:
        push(ft, fc, tt, tc)
    return out


def fk_constraint_name(from_tbl_new: str, from_col_new: str, idx: int) -> str:
    safe_tbl = re.sub(r"[^A-Za-z0-9_]", "_", from_tbl_new)
    safe_col = re.sub(r"[^A-Za-z0-9_]", "_", from_col_new)
    return f"FK_{safe_tbl}_{safe_col}_{idx}"


def unique_rules() -> list[tuple[str, str, str]]:
    return [
        ("Customers", "Shorthand_Code", "Customers_ShorthandCode"),
        ("Suppliers", "Shorthand_Code", "Suppliers_ShorthandCode"),
        ("Customers", "Membership_Number", "Customers_MembershipNumber"),
        ("Account_Payables", "Invoice_Number", "AccountPayables_InvoiceNumber"),
        ("Account_Receivables", "Invoice_Number", "AccountReceivables_InvoiceNumber"),
        ("Products", "Serial_Number", "Products_SerialNumber"),
    ]


def sql_escape(s: str) -> str:
    return s.replace("'", "''")


def emit_table(
    name_old: str,
    cols: list[dict[str, str]],
    pk_part: str,
    fks_for_table: list[dict[str, str]],
    uniques: list[tuple[str, str]],
) -> str:
    new_name = TABLE_MAP[name_old]
    col_lines: list[str] = []
    for c in cols:
        new_c = map_identifier(c["old"])
        typ = normalize_type(new_c, c["type"])
        rest = c["rest"]
        col_lines.append(f"\t[{new_c}] {typ} {rest}".rstrip())

    extras: list[str] = []
    for i, fk in enumerate(fks_for_table, start=1):
        fc = map_identifier(fk["from_col"])
        tt = TABLE_MAP[fk["to_table"]]
        tc = map_identifier(fk["to_col"])
        cname = fk_constraint_name(new_name, fc, i)
        extras.append(f"\tCONSTRAINT [{cname}] FOREIGN KEY ([{fc}]) REFERENCES [dbo].[{tt}]([{tc}])")
    for ucol, uname in uniques:
        uc = map_identifier(ucol)
        extras.append(f"\tCONSTRAINT [UQ_{uname}] UNIQUE ([{uc}])")

    pk_sql = rebuild_pk_constraint(pk_part, new_name)

    parts: list[str] = [f"CREATE TABLE [dbo].[{new_name}]("]
    if col_lines:
        for i, ln in enumerate(col_lines):
            comma = "," if i < len(col_lines) - 1 or pk_sql or extras else ""
            parts.append(ln + comma)
    parts.append(pk_sql.rstrip(","))
    for ex in extras:
        parts.append(",\n" + ex)
    parts.append(") ON [PRIMARY]")
    parts.append("GO")
    return "\n".join(parts) + "\n"


def build_descriptions(
    desc_map: dict[tuple[str, str | None], str],
    tables_meta: list[tuple[str, list[dict[str, str]]]],
) -> str:
    lines: list[str] = []
    for name_old, cols in tables_meta:
        t_new = TABLE_MAP[name_old]
        tbl_desc = desc_map.get((name_old, None))
        if not tbl_desc:
            # try new table name key from SSMS re-scripts
            tbl_desc = desc_map.get((t_new, None))
        if not tbl_desc:
            tbl_desc = f"{t_new} 表"
        lines.append(
            "EXEC sys.sp_addextendedproperty @name=N'MS_Description', "
            f"@value=N'{sql_escape(tbl_desc)}' , "
            "@level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',"
            f"@level1name=N'{t_new}'\nGO\n"
        )
        for c in cols:
            c_old = c["old"]
            c_new = map_identifier(c_old)
            d = desc_map.get((name_old, c_old))
            if not d:
                d = desc_map.get((t_new, c_old))
            if not d:
                d = desc_map.get((name_old, c_new))
            if not d:
                d = humanize_fallback(c_new)
            lines.append(
                "EXEC sys.sp_addextendedproperty @name=N'MS_Description', "
                f"@value=N'{sql_escape(d)}' , "
                "@level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',"
                f"@level1name=N'{t_new}', @level2type=N'COLUMN',@level2name=N'{c_new}'\nGO\n"
            )
    return "".join(lines)


def main() -> int:
    sql = SRC.read_text(encoding="utf-8")
    desc_map = parse_extended_props(sql)
    base_fks = parse_fk_alters(sql)

    creates = list(RE_CREATE.finditer(sql))
    if not creates:
        raise RuntimeError("No CREATE TABLE blocks found")

    colsets: dict[str, set[str]] = {}
    tables_meta: list[tuple[str, list[dict[str, str]], str]] = []

    for m in creates:
        name_old = m.group("name")
        inner = m.group("body")
        lines, pk_part = split_table_inner(inner)
        cols = parse_columns(lines)
        colsets[name_old] = {c["old"] for c in cols}
        tables_meta.append((name_old, cols, pk_part))

    extras = extra_fk_candidates()
    merged = merge_fks(base_fks, extras, colsets)
    fk_by_table: dict[str, list[dict[str, str]]] = {}
    for fk in merged:
        fk_by_table.setdefault(fk["from_table"], []).append(fk)

    out: list[str] = ["USE [Evolver]\nGO\n"]

    for name_old, cols, pk_part in tables_meta:
        uqs = [(c, s) for (t, c, s) in unique_rules() if t == name_old]
        out.append(
            f"/****** Object:  Table [dbo].[{TABLE_MAP[name_old]}] ******/\n"
            "SET ANSI_NULLS ON\nGO\nSET QUOTED_IDENTIFIER ON\nGO\n"
        )
        out.append(
            emit_table(
                name_old,
                cols,
                pk_part,
                fk_by_table.get(name_old, []),
                uqs,
            )
        )

    tables_meta_desc: list[tuple[str, list[dict[str, str]]]] = [(a, b) for a, b, _ in tables_meta]
    out.append(build_descriptions(desc_map, tables_meta_desc))
    DST = SRC
    text = "".join(out)
    DST.write_text(text, encoding="utf-8")
    print(f"Wrote {DST} ({len(text)} chars)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
