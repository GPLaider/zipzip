# ZipZip 개발 빌드

Windows x64, .NET SDK 8, 7z.exe와 7z.dll이 필요하다. 검증 환경은 SDK 8.0.425 / 7-Zip 24.09다.
WinUI CLI 빌드에 필요한 PRI 작업은 앱의 Microsoft.Windows.SDK.BuildTools.MSIX 패키지 참조로 제공한다.

저장소 루트에서 PowerShell로 실행한다. 출력과 NuGet 캐시를 여유 공간이 있는 드라이브에 둘 수 있다.

```powershell
$env:NUGET_PACKAGES = 'D:\ZipZip-build\packages'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-ZipZip.ps1 -Dotnet 'D:\ZipZip-build\sdk\dotnet.exe' -ArtifactsRoot 'D:\ZipZip-build\verified' -SevenZipDirectory 'C:\Program Files\7-Zip'
```

PATH에 SDK가 있으면 -Dotnet을 생략한다. -SevenZipDirectory는 7-Zip 실행 파일과 DLL이 있는 폴더로 바꾼다.
스크립트는 앱·탐색기 보조 프로그램 빌드, 기존 테스트 21개, 실제 압축 엔진 검사 28개, 아이콘 검사 후 실행 폴더를 만든다.
성공 시 `D:\ZipZip-build\verified\run\ZipZip.App.exe`를 실행한다. 설치·파일 연결 등록은 수행하지 않는다.
테스트 입력과 출력은 ArtifactsRoot\smoke 아래 고유 폴더에 보존한다.

SDK 8에서는 .slnx 대신 개별 .csproj 또는 위 스크립트를 사용한다.
이 결과는 개발용 실행 빌드다. 서명된 배포 설치 파일은 별도 검증이 필요하며, 7-Zip을 재배포할 때는 해당 라이선스도 포함해야 한다.

아이콘 원본은 `tools/assets/ZipZip-master.png`다. 변경한 뒤 아래 명령으로 앱과 파일 형식별 PNG·ICO를 함께 갱신한다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Generate-ZipZipIcons.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Test-ZipZipIcons.ps1
```

## 설치 파일 빌드

.NET SDK 8과 Inno Setup 6으로 Release 앱·검사·설치 파일을 한 번에 만든다. Visual Studio 고정 경로는 필요 없다.

```powershell
$env:NUGET_PACKAGES = 'D:\ZipZip-build\packages'
$env:NUGET_HTTP_CACHE_PATH = 'D:\ZipZip-build\nuget-http'
./tools/Build-ZipZipInstaller.ps1 -Version '0.2.0-preview' -Dotnet 'D:\ZipZip-build\sdk\dotnet.exe' -ArtifactsRoot 'D:\ZipZip-build\release' -SevenZipDirectory 'C:\Program Files\7-Zip' -Iscc 'D:\ZipZip-build\inno\ISCC.exe'
```

`dist`에 설치 파일과 `SHA256SUMS.txt`를 만든다. 7-Zip License.txt·History.txt를 포함하며 서명은 `-SignThumbprint`를 지정한 경우만 수행한다. Windows App SDK는 파일 선택창 포커스 복구 수정이 포함된 1.8.11을 사용한다.
