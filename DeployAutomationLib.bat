@echo off
REM NodeLib.Automation 编译和部署脚本
echo ============================================
echo NodeLib.Automation 编译和部署工具
echo ============================================
echo.

REM 编译项目
echo [1/3] 正在编译 NodeLib.Automation...
dotnet build NodeLib.Automation\NodeLib.Automation.csproj --configuration Release
if %errorlevel% neq 0 (
    echo 编译失败！
    pause
    exit /b 1
)
echo 编译成功！
echo.

REM 创建目标目录
echo [2/3] 创建部署目录...
set TARGET_DIR=%USERPROFILE%\Documents\XNode\NodeLib
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
echo 目标目录: %TARGET_DIR%
echo.

REM 复制文件
echo [3/3] 复制文件到节点库目录...
xcopy /Y /I NodeLib.Automation\bin\Release\net8.0-windows\*.dll "%TARGET_DIR%\"
xcopy /Y /I NodeLib.Automation\bin\Release\net8.0-windows\*.json "%TARGET_DIR%\"
xcopy /Y /I NodeLib.Automation\bin\Release\net8.0-windows\*.pdb "%TARGET_DIR%\"
echo.

echo ============================================
echo 部署完成！
echo ============================================
echo.
echo 节点库已部署到: %TARGET_DIR%
echo.
echo 请重启 XNode 应用程序以加载新的节点库。
echo 在节点库面板中查找"自动化"分类。
echo.
pause
