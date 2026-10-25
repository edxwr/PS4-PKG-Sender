# 🎮 PS4 PKG Sender (Avalonia UI)

![.NET](https://img.shields.io/badge/.NET-C%23-512BD4?logo=dotnet)
![Avalonia UI](https://img.shields.io/badge/Avalonia_UI-Cross--Platform-007ACC?logo=avaloniaui)
![License](https://img.shields.io/badge/License-MIT-green)

A lightweight desktop application built with **C#** and **Avalonia UI** (MVVM) for sending and installing `.pkg` files directly to a jailbroken **PlayStation 4** over the local network (LAN).

Instead of copying large game files onto USB drives, **PS4 PKG Sender** injects a network installer payload via GoldHEN's BinLoader and streams the `.pkg` files straight to the console over HTTP.

---

## ✨ Features

* **📦 Automatic PKG Metadata Extraction**: Reads `.pkg` headers and displays:
  * Game Icon (`icon0.png` rendered directly in the UI)
  * Title & Title ID (`CUSAxxxxx`)
  * Package Type (`Game`, `Update`, `DLC`)
  * Application Version, File Name, and Size in GB
* **🔀 Automatic PKG Sorting**: Sorts all `.pkg` files by their type for proper order of installations:
  1. Games
  2. Updates
  3. DLCs
* **🚀 Direct Payload Injection**: Sends the installation payload directly to GoldHEN's BinLoader server on port `9090`.
* **🌐 Built-in HTTP Range Server**: Streams `.pkg` files using byte-range requests (`206 Partial Content`), properly handling PS4 header probes and connection closures.
* **⏸️ Native PS4 Download Control**: Supports **Pause** and **Resume** directly from the PS4 Notifications / Downloads menu.
* **📊 Real-Time Progress Bar**: Tracks transferred gigabytes (`GB`) versus total package size for each item in the queue.

---

## 🛠️ Requirements

### On your PS4:
1. A jailbroken PS4 running **GoldHEN**.
2. **BinLoader Server** enabled in GoldHEN settings (listening exclusively on port **`9090`**).
3. Both your PC and PS4 connected to the same local network (LAN).

---

## 🚀 How to Use

1. **Enable BinLoader**: On your PS4, open GoldHEN Settings and make sure the **BinLoader Server** is enabled (port `9090`).
2. **Launch PS4 PKG Sender**: Open the application on your PC (your local `PC IP` is detected automatically).
3. **Enter PS4 IP**: Type your console's IP address in the **PS4 IP** box.
4. **Queue Packages**: Click **Add PKG** to select one or more `.pkg` files from your computer. You can remove individual items using the **Remove PKG** button or clear the entire list with **Clear PKGs**.
5. **Start Transfer**: Click **Send to PS4**. The app will inject the payload over port `9090` and begin streaming the queued packages to your console.

---

## 🏗️ Built With

* **[Avalonia UI](https://avaloniaui.net/)** — Cross-platform XAML user interface.
* **[CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)** — MVVM architecture (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).

---

## 👏 Credits

* **[marcussacana](https://github.com/marcussacana)** — Creator of the PS4 payload used by this application to handle network package transfers.
* **[SiSTR0](https://github.com/SiSTR0)** — For GoldHEN and the BinLoader server implementation.

---

## 🔧 Building from Source

```bash
git clone https://github.com/edxwr/PS4-PKG-Sender.git
cd Ps4PkgSender
dotnet run
```

---

## 📄 License

This project is open-source and available under the **MIT License**.