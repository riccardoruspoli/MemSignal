<div align="center">

![MemSignal](src/MemSignal.App.Wpf/Assets/app-icon.png)

# MemSignal

**An experimental memory-pressure signal for Windows.**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge)
![License](https://img.shields.io/badge/license-mit-green?style=for-the-badge)

</div>

MemSignal is a small project built out of curiosity, inspired by the concept of memory pressure in macOS: high RAM usage does not always mean that a system is under pressure, so could Windows expose a more useful signal at a glance?

MemSignal combines several Windows memory metrics into an approximate memory-pressure estimate and keeps the current state visible in the system tray.

## 🧭 What it shows

MemSignal considers committed memory, available physical memory, paging activity, hard page reads, and pagefile usage when available. It reports an estimated pressure percentage together with a **Normal**, **Warning**, or **Critical** state.

The Details panel exposes the values behind the estimate.

## 🧮 How the estimate works

Each available metric is normalized to a value between `0` and `1`, where higher values indicate greater pressure. The normalized values are combined using fixed weights, then smoothed over time to reduce short-lived jumps.

The final value is displayed as a percentage from `0%` to `100%`. This percentage is a normalized score, not the amount of RAM in use or a probability that the system will run out of memory.

## 📄 License

MemSignal is available under the [MIT License](LICENSE).
