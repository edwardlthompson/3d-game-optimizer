import os
import sqlite3

db = os.path.join(os.environ["LOCALAPPDATA"], "3d-game-optimizer", "library.db")
cache = os.path.join(os.environ["LOCALAPPDATA"], "3d-game-optimizer", "cache", "covers")
print("db exists:", os.path.exists(db))
print("cache files:", len(list(os.listdir(cache))) if os.path.isdir(cache) else 0)

if os.path.exists(db):
    con = sqlite3.connect(db)
    c = con.cursor()
    c.execute("SELECT COUNT(*) FROM games")
    print("games total:", c.fetchone()[0])
    c.execute(
        "SELECT COUNT(*) FROM games WHERE cover_cache_path IS NOT NULL AND LENGTH(cover_cache_path) > 0"
    )
    print("games with cover path:", c.fetchone()[0])
    c.execute(
        "SELECT steam_app_id, title, cover_cache_path FROM games WHERE steam_app_id IN (1091500, 1086940, 570)"
    )
    print("samples:")
    for row in c.fetchall():
        print(" ", row)
    con.close()
