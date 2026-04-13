# ZipZip Windows 탐색기 메뉴

## 등록되는 메뉴

- 일반 파일/폴더 우클릭
  - `ZipZip으로 압축하기...`
  - `ZIP으로 압축하기`
  - `7Z로 압축하기`
- 압축 파일 우클릭
  - `ZipZip으로 열기`
  - `여기에 풀기`
  - `새 폴더에 풀기`

압축 파일로 인식하는 확장자:

- `.zip`, `.7z`, `.rar`, `.tar`, `.gz`, `.bz2`, `.xz`, `.lz`, `.lzma`, `.zst`
- `.tgz`, `.tbz`, `.tbz2`, `.txz`
- `.cab`, `.iso`, `.wim`, `.arj`, `.cpio`, `.z`, `.lzh`

## 등록

PowerShell에서 실행:

```powershell
& "C:\Users\A\Documents\codex\zipzip\scripts\Register-ZipZipShellIntegration.ps1"
```

수동 등록이 필요하면:

```powershell
& "C:\Users\A\Documents\codex\zipzip\src\ZipZip.ShellExtension\bin\x64\Debug\net8.0-windows10.0.19041.0\ZipZip.ShellExtension.exe" register "C:\Users\A\Documents\codex\zipzip\src\ZipZip.App\bin\x64\Debug\net8.0-windows10.0.19041.0\ZipZip.App.exe"
```

## 해제

```powershell
& "C:\Users\A\Documents\codex\zipzip\scripts\Unregister-ZipZipShellIntegration.ps1"
```

## 확인 항목

- `ZIP으로 압축하기`가 바로 생성되는지
- `7Z로 압축하기`가 바로 생성되는지
- `여기에 풀기`가 정상 동작하는지
- `새 폴더에 풀기`가 정상 동작하는지
- 같은 이름 폴더가 이미 있으면 `demo (2)`처럼 충돌을 피하는지

## 메모

- 탐색기 메뉴가 바로 안 보이면 탐색기 창을 닫았다가 다시 열어보세요.
- Windows 11에서는 필요할 때 `추가 옵션 표시` 아래에 나올 수 있습니다.
- 메뉴 수정 후에는 최신 셸 도우미를 다시 빌드하고 재등록해야 합니다.
