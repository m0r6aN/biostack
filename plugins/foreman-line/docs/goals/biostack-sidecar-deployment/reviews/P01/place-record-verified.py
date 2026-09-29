"""P01 R02 — RECORD-verified offline placement of hash-pinned tool wheels.

Context: the workflow pins 28 tool packages with --require-hashes. The local
uv cache holds hash-verifiable wheel BODIES for 17 of them (installed via
`uv pip install --offline --require-hashes`) but only UNPACKED archives for
the remaining 11. Wheel-level re-hashing is impossible offline for those 11,
so this script verifies them at the next strongest level: every file listed
in the wheel's own dist-info/RECORD is re-hashed (sha256, urlsafe-b64
unpadded, per the RECORD format) and size-checked, then copied bit-for-bit
into the target venv. Any mismatch aborts before any copy.

Usage: place-record-verified.py <venv-site-packages> <spec>=<archive-dir> ...
Each <archive-dir> is the uv-cache unpacked wheel root containing the
package dir(s) plus <dist>-<ver>.dist-info/RECORD.
"""

import base64
import csv
import hashlib
import os
import shutil
import sys


def record_hash_ok(path, expected):
    h = hashlib.sha256()
    size = 0
    with open(path, "rb") as fh:
        while True:
            chunk = fh.read(1048576)
            if not chunk:
                break
            size += len(chunk)
            h.update(chunk)
    digest = base64.urlsafe_b64encode(h.digest()).decode("ascii").rstrip("=")
    return digest == expected, size


def main():
    dest_root = sys.argv[1]
    total_files = 0
    for spec in sys.argv[2:]:
        name_ver, archive = spec.rsplit("=", 1)
        dist_info = None
        for entry in sorted(os.listdir(archive)):
            if entry.endswith(".dist-info"):
                dist_info = entry
                break
        if dist_info is None:
            print("FAIL %s: no .dist-info in %s" % (name_ver, archive))
            return 1
        record_path = os.path.join(archive, dist_info, "RECORD")
        checked = 0
        with open(record_path, newline="", encoding="utf-8") as fh:
            for rel, hash_field, size_field in csv.reader(fh):
                if not hash_field:
                    continue  # RECORD entry itself / unsigned file
                if not hash_field.startswith("sha256="):
                    print("FAIL %s: unsupported hash %r" % (name_ver, hash_field))
                    return 1
                src = os.path.join(archive, *rel.split("/"))
                if not os.path.isfile(src):
                    print("FAIL %s: listed file absent %s" % (name_ver, rel))
                    return 1
                ok, size = record_hash_ok(src, hash_field[len("sha256="):])
                if not ok or (size_field and int(size_field) != size):
                    print("FAIL %s: hash/size mismatch %s" % (name_ver, rel))
                    return 1
                checked += 1
        # Copy exactly the RECORD-listed tree: top-level package dirs + dist-info.
        tops = set()
        with open(record_path, newline="", encoding="utf-8") as fh:
            for rel, hash_field, size_field in csv.reader(fh):
                top = rel.split("/")[0]
                if top and top != ".." and not rel.startswith(".."):
                    tops.add(top)
        for top in sorted(tops):
            src = os.path.join(archive, top)
            dst = os.path.join(dest_root, top)
            if os.path.isdir(src):
                if os.path.isdir(dst):
                    shutil.rmtree(dst)
                shutil.copytree(src, dst)
            elif os.path.isfile(src):
                shutil.copy2(src, dst)
            else:
                print("FAIL %s: cannot copy %s" % (name_ver, top))
                return 1
        # Re-verify the placed copy.
        with open(record_path, newline="", encoding="utf-8") as fh:
            for rel, hash_field, size_field in csv.reader(fh):
                if not hash_field or not hash_field.startswith("sha256="):
                    continue
                dst = os.path.join(dest_root, *rel.split("/"))
                ok, size = record_hash_ok(dst, hash_field[len("sha256="):])
                if not ok or (size_field and int(size_field) != size):
                    print("FAIL %s: placed-copy mismatch %s" % (name_ver, rel))
                    return 1
        print("OK %s: %d files RECORD-verified at source and destination" % (name_ver, checked))
        total_files += checked
    print("PLACED total_files=%d" % total_files)


if __name__ == "__main__":
    main()
