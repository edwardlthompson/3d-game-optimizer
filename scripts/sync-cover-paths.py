"""One-off: sync disk cover cache paths into library.db (same logic as CoverArtCacheSync)."""
import os
import sqlite3

root = os.path.join(os.environ["LOCALAPPDATA"], "3d-game-optimizer")
db_path = os.path.join(root, "library.db")
cache_dir = os.path.join(root, "cache", "covers")
external = {"epic", "gog", "ubisoft", "local"}

if not os.path.exists(db_path):
    raise SystemExit(f"Missing {db_path}")

con = sqlite3.connect(db_path)
cur = con.cursor()
cur.execute("SELECT steam_app_id, review_descriptor, cover_cache_path FROM games")
updated = 0
for app_id, descriptor, cover_path in cur.fetchall():
    if app_id <= 0:
        continue
    if descriptor and descriptor.lower() in external:
        continue
    if cover_path and os.path.isfile(cover_path):
        continue
    cached = os.path.join(cache_dir, f"{app_id}.jpg")
    if os.path.isfile(cached):
        cur.execute(
            "UPDATE games SET cover_cache_path = ? WHERE steam_app_id = ?",
            (cached, app_id),
        )
        updated += 1

con.commit()
cur.execute(
    "SELECT steam_app_id, title, cover_cache_path FROM games WHERE steam_app_id IN (1091500, 1086940, 570)"
)
print("updated rows:", updated)
for row in cur.fetchall():
    print(" ", row)
con.close()
