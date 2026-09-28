"""Build an empty SASL database from sasl_schema_v2.sql.

Usage (from the ai/ folder):
    python database/create_db.py

Creates data/raw/sasl.db. The .db file is gitignored, so it is never committed.
"""
import sqlite3
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
SCHEMA = HERE / "sasl_schema_v2.sql"
OUT = HERE.parent / "data" / "raw" / "sasl.db"


def main() -> None:
    if OUT.exists():
        sys.exit(f"{OUT} already exists. Delete it first if you want a fresh one.")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(OUT)
    conn.executescript(SCHEMA.read_text(encoding="utf-8"))
    conn.commit()
    signs = conn.execute("SELECT COUNT(*) FROM Signs").fetchone()[0]
    joints = conn.execute("SELECT COUNT(*) FROM HandJoints").fetchone()[0]
    conn.close()
    print(f"Created {OUT} with {signs} signs and {joints} joints.")


if __name__ == "__main__":
    main()
