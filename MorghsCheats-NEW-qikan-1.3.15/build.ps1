# MorghsCheats 构建脚本
# 用法: .\build.ps1
# 编译成功后，可直接复制 _build\MorghsCheats\ 到游戏 Modules 目录

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DllSource = Join-Path $ProjectDir "bin\Win64_Shipping_Client\MorghsCheats.dll"
$BuildDir = Join-Path $ProjectDir "_build\MorghsCheats"
$BuildBin = Join-Path $BuildDir "bin\Win64_Shipping_Client"

Write-Host "=== MorghsCheats 构建 ===" -ForegroundColor Green

# 构建
Push-Location $ProjectDir
dotnet build -c Release -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败！" -ForegroundColor Red
    Pop-Location
    exit $LASTEXITCODE
}
Pop-Location

Write-Host "构建成功！" -ForegroundColor Green

# 生成发布文件到 _build 目录
if (-not (Test-Path $BuildBin)) {
    New-Item -ItemType Directory -Path $BuildBin -Force | Out-Null
}
Copy-Item $DllSource (Join-Path $BuildBin "MorghsCheats.dll") -Force
Copy-Item (Join-Path $ProjectDir "SubModule.xml") (Join-Path $BuildDir "SubModule.xml") -Force

Write-Host "已生成发布文件到: $BuildDir" -ForegroundColor Green

# 可选：直接部署到游戏目录
$GameModules = "D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\MorghsCheats"
if (Test-Path $GameModules) {
    Copy-Item $DllSource (Join-Path $GameModules "MorghsCheats.dll") -Force
    Copy-Item (Join-Path $ProjectDir "SubModule.xml") (Join-Path $GameModules "SubModule.xml") -Force
    Write-Host "已自动部署到: $GameModules" -ForegroundColor Green
}
else {
    Write-Host "手动部署: 复制 _build\MorghsCheats\ → 游戏 Modules 目录" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "完成！复制 _build\MorghsCheats\ 到游戏目录即可使用" -ForegroundColor Green
