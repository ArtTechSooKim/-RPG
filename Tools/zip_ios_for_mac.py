# 아이폰 빌드(Xcode 프로젝트)를 Mac으로 옮길 zip으로 묶는다.
# Windows에서 그냥 압축하면 실행 권한이 사라져 Mac의 Xcode 빌드 단계(process_symbols.sh, il2cpp 도구)가
# 'Permission denied'로 실패한다 → 실행 파일(Mach-O, #! 스크립트, .sh)에 Unix 권한 755를 넣어 묶는다.
#
# 사용: python Tools/zip_ios_for_mac.py [Xcode 프로젝트 폴더 (기본 Builds/iOS)]
#   → Builds/WordRPG-iOS-{버전}-build{빌드 번호}.zip  (맨 위에 Docs/아이폰출시방법.txt 포함)
import os
import plistlib
import re
import sys
import time
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOP = "WordRPG-iOS"
README = os.path.join(ROOT, "Docs", "아이폰출시방법.txt")
MACHO = {b"\xcf\xfa\xed\xfe", b"\xce\xfa\xed\xfe", b"\xca\xfe\xba\xbe", b"\xfe\xed\xfa\xcf", b"\xbe\xba\xfe\xca"}


def is_executable(path):
    if path.endswith((".sh", ".command", ".py", ".pl")):
        return True
    with open(path, "rb") as f:
        head = f.read(4)
    return head in MACHO or head[:2] == b"#!"


def version_of(src):
    # Unity가 만든 Info.plist: CFBundleShortVersionString(0.1), CFBundleVersion(빌드 번호)
    with open(os.path.join(src, "Info.plist"), "rb") as f:
        data = f.read()
    try:
        plist = plistlib.loads(data)
        return plist.get("CFBundleShortVersionString", "0"), plist.get("CFBundleVersion", "0")
    except Exception:
        text = data.decode("utf-8", "replace")
        def find(key):
            m = re.search(rf"<key>{key}</key>\s*<string>([^<]*)</string>", text)
            return m.group(1) if m else "0"
        return find("CFBundleShortVersionString"), find("CFBundleVersion")


def add(z, path, arc):
    info = zipfile.ZipInfo(arc, time.localtime(os.path.getmtime(path))[:6])
    info.create_system = 3  # Unix: Mac이 권한 비트를 읽는다
    executable = is_executable(path)
    info.external_attr = (0o100755 if executable else 0o100644) << 16
    info.compress_type = zipfile.ZIP_DEFLATED
    with open(path, "rb") as f:
        z.writestr(info, f.read())
    return executable


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Builds", "iOS")
    if not os.path.isfile(os.path.join(src, "Info.plist")):
        sys.exit(f"Xcode 프로젝트가 없어요: {src}  (Unity 메뉴 WordRPG > Build > iOS Xcode 프로젝트를 먼저)")
    version, build = version_of(src)
    out = os.path.join(ROOT, "Builds", f"WordRPG-iOS-{version}-build{build}.zip")
    files = executables = 0
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        add(z, README, f"{TOP}/Mac에서_읽어주세요.txt")
        for folder, dirs, names in os.walk(src):
            dirs.sort()
            for name in sorted(names):
                full = os.path.join(folder, name)
                rel = os.path.relpath(full, src).replace("\\", "/")
                executables += add(z, full, f"{TOP}/{rel}")
                files += 1
    print(f"{out}\n파일 {files}개 (실행 파일 {executables}개), {os.path.getsize(out) / 1024 / 1024:.0f} MB")


if __name__ == "__main__":
    main()
