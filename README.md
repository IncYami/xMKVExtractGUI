# xMKVExtractGUI

A modern, cross-platform Avalonia UI front-end for `mkvextract` (part of [MKVToolNix](https://mkvtoolnix.download/)).

**xMKVExtractGUI** is a modern alternative and spiritual successor to the classic gMKVExtractGUI. It leverages the complete, battle-tested backend of the original gMKVExtractGUI, entirely rewritten and ported from .NET Framework 4 to the cutting-edge **.NET 10**. Paired with a brand-new interface built on **Avalonia UI**, it brings a native, high-performance, and dark-themed experience (inspired by OBS) to modern operating systems.

## ✨ Features

* **Modern Interface:** Built with Avalonia UI, featuring a clean, responsive, and dark-mode-first design.
* **Full Backend Power:** Retains 100% of the reliable extraction logic from the original gMKVExtractGUI.
* **Batch Processing:** Extract tracks, chapters, attachments, and tags from multiple Matroska (`.mkv`, `.mka`, etc.) files simultaneously.
* **Portable & Self-Contained:** Can be compiled into a single executable without requiring a system-wide .NET installation.
* **Cross-Platform Support:** Official release targets for Windows, Linux, and macOS.

## 🛠️ Prerequisites

To run or extract files using this application, you must have **MKVToolNix** installed on your system or placed in a known directory, as this app acts as a GUI for `mkvextract` and `mkvmerge`.

If you intend to edit the code or build the project yourself, you will need:
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* An IDE such as Visual Studio 2022, JetBrains Rider, or VS Code.

## 💻 How to Edit and Run Locally

To contribute or test the application on your own machine:

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/priestess/xMKVExtractGUI.git](https://github.com/priestess/xMKVExtractGUI.git)
   cd xMKVExtractGUI
   ```
   
2. **Restore dependencies:**
    ```bash
    dotnet restore
    ```

3. **Run the project:**
    Navigate to the main project folder and run:
    ```bash
    dotnet run --project xMKVExtractGUI/xMKVExtractGUI.csproj
    ```
*(Note: Adjust the path to the `.csproj` file depending on your exact folder structure).*

## 📦 How to Build for Release

The application can be compiled as a **Single File, Self-Contained** executable, meaning users won't need to install the .NET 10 runtime to use it.

Run the following commands in the terminal at the root of the project to build for the officially supported platforms:

### Windows (x64)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

```

### Linux (x64)

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

```

### macOS (x64)

```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

```

Once compiled, the final portable executables will be located in the `bin/Release/net10.0/<target-os>/publish/` directory.

## 📜 License

This project is licensed under the [GPLv2 License](https://www.google.com/search?q=LICENSE) - see the https://www.google.com/search?q=LICENSE file for details.
