"""Post-fix Evolver.sql after transform: PK names, ON [PRIMARY] casing, AssignedUserCode FK gaps."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SQL = ROOT / "doc" / "3.design" / "Evolver.sql"


def fix_primary_casing(text: str) -> str:
    return text.replace("ON [Primary]", "ON [PRIMARY]")


def fix_pk_names(text: str) -> str:
    lines = text.splitlines()
    out: list[str] = []
    cur: str | None = None
    for line in lines:
        m = re.match(r"^CREATE TABLE \[dbo\]\.\[([^\]]+)\]\(", line.strip())
        if m:
            cur = m.group(1)
        if cur and re.match(r"^\s*CONSTRAINT \[PK", line):
            line = re.sub(r"CONSTRAINT \[PK[^\]]+\]", f"CONSTRAINT [PK{cur}]", line, count=1, flags=re.I)
        out.append(line)
    nl = "\n" if text.endswith("\n") else ""
    return "\n".join(out) + nl


def add_assigned_user_fks(text: str) -> str:
    out: list[str] = []
    pos = 0
    for m in re.finditer(r"CREATE TABLE \[dbo\]\.\[([^\]]+)\]\(", text):
        tbl = m.group(1)
        after_open = m.end()
        tail = text[after_open:]
        end_m = re.search(r"\n\)\s*ON\s*\[PRIMARY\]\s*\r?\nGO", tail)
        if not end_m:
            continue
        start = m.start()
        end = after_open + end_m.end()
        inner = tail[: end_m.start()]
        chunk = inner
        if "[AssignedUserCode]" in chunk and not re.search(r"FOREIGN KEY \(\[AssignedUserCode\]\)", chunk):
            nums = [int(x) for x in re.findall(rf"CONSTRAINT \[FK_{re.escape(tbl)}_[^\]]+_(\d+)\]", chunk)]
            nxt = max(nums, default=0) + 1
            fk = (
                f",\n\tCONSTRAINT [FK_{tbl}_AssignedUserCode_{nxt}] FOREIGN KEY ([AssignedUserCode]) "
                f"REFERENCES [dbo].[Users]([UserCode])"
            )
            if re.search(r"CONSTRAINT \[UQ_", chunk):
                chunk = re.sub(r"(\n\s*,\s*\n\s*CONSTRAINT \[UQ_)", fk + r"\1", chunk, count=1)
            else:
                chunk = chunk + fk
        out.append(text[pos:start])
        out.append(f"CREATE TABLE [dbo].[{tbl}]({chunk}) ON [PRIMARY]\nGO")
        pos = end
    out.append(text[pos:])
    return "".join(out)


def main() -> int:
    text = SQL.read_text(encoding="utf-8")
    text = fix_primary_casing(text)
    text = fix_pk_names(text)
    text = add_assigned_user_fks(text)
    SQL.write_text(text, encoding="utf-8")
    print(f"Patched {SQL}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
