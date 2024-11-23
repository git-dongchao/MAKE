# MAKE
一个为Visual Studio 2019提供的插件，旨在简化Linux程序开发。
# 特性
- 支持Linux开发：在Visual Studio 2019中直接开发Linux程序。
- 支持Linux运行和调试：在Visual Studio 2019中直接进行Linux程序的运行和调试。
- 支持Window和Linux跨平台编译：配合CMAKE支持跨平台编译。
- 集成终端：直接在指定目录启动Window和Linux终端。
# 编译
## 前提条件
- Visual Studio 2019
- Windows操作系统（建议版本：Windows 10及以上）
- 安装有Linux开发工作负载（通过Visual Studio Installer进行安装）
## 编译步骤
1. 使用Visual Studio 2019打开MAKE.sln文件。
2. 添加liblinux引用  
   文件目录在"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\PrivateAssemblies\liblinux.dll"
3. 添加Microsoft.MIDebugEngine引用  
文件目录在"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\MDD\Debugger\Microsoft.MIDebugEngine.dll"
4. 编译
# 安装
## 安装步骤
1. 编译MAKE项目输出MAKE.vsix文件。
2. 运行MAKE.vsix进行安装。
# 使用
## 创建项目目录
1. 本地新建一个目录dev。
2. 在dev目录右键，使用"Visual Studio"打开。
## 编写代码
```cpp
// test.cpp
#include <iostream>

int main() {
    std::cout << "Hello, Linux from Visual Studio!" << std::endl;
    return 0;
}
```
## 编写CMakeLists.txt文件
```cmake
project(test)
cmake_minimum_required(VERSION 2.6)

add_executable(test ${PROJECT_SOURCE_DIR}/test.cpp)
```
## 编写make.sh脚本
```bash
cmake -DCMAKE_BUILD_TYPE=Debug -S . -B build_linux
cd build_linux && make
```
## 配置
1. 打开"工具" - "选项" - "跨平台" - "连接管理器" 页面。
2. 添加远程SSH连接。
   - 设置主机地址。
   - 设置端口。
   - 设置用户名。
   - 设置密码。
4. 下载远程系统标头文件。
5. 打开"扩展" - "MAKE" - "设置" 页面。
   - 设置远程主机。
   - 设置远程工作目录。
   - 设置终端类型。
## 编译
在Visual Studio 2019中选中make.sh文件，右键"生成"，开始编译。
## 运行
在Visual Studio 2019中选中test文件，右键"运行"，开始运行。
## 调试
在Visual Studio 2019中选中test文件，右键"调试"，开始调试。
# 许可证
该项目采用 MIT License，具体内容请查看LICENSE文件。
