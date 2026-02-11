# TPMPass

**TPMPass** is a secure, passwordless password manager for Windows. It leverages the Windows Data Protection API (DPAPI) to ensure that your identity is the only key needed to access your data.

---

## 🚀 Key Features

* **Passwordless Security:** No master password to remember or lose. Encryption is bound to your Windows user account and physical machine.
* **Memory Protection:** Uses a custom `SecureBuffer` to encrypt sensitive data in RAM and lock memory pages to prevent swapping to disk.
* **Proactive Defense:** Includes anti-debugging and anti-dumping measures. It scans the calling process using Windows Defender to prevent automation by malicious software.
* **Dual Interface:** Full interactive console dashboard and a streamlined command-line interface for scripting.
* **Auto-Clear Clipboard:** Automatically clears your clipboard after a configurable duration to prevent password leakage.

---

## 📸 Visuals

> [!TIP]
> **Showcase your project:** Insert screenshots of the Main Menu, Encryption Flow, and Settings here to give users a first look.

---

## 🛠 How It Works

TPMPass uses a multi-layered encryption approach to keep your data safe:

1.  **Master Identity:** Generates a unique 32-byte high-entropy key stored in `%AppData%`.
2.  **OS Binding:** This identity is protected via **Windows DPAPI**, ensuring it cannot be used if copied to another machine or accessed by another user.
3.  **File Encryption:** Each `.tpmPassword` file uses its own unique salt, combined with the master identity, to derive a specific **AES-256** key.

---

## 💻 Usage

### Interactive Mode
Simply run the executable to open the dashboard and follow the menu prompts:
```bash
TPMPass.exe
```

### CLI Mode
You can use TPMPass in batch files or automated scripts:
* **Encrypt:** `TPMPass.exe --set "your_password" "filename.tpmPassword"`
* **Decrypt & Print:** `TPMPass.exe --get "filename.tpmPassword"`
* **Decrypt to Clipboard:** `TPMPass.exe "filename.tpmPassword" --noUI`

---

## 📊 Technical Specifications

| Component | Technology |
| :--- | :--- |
| **Runtime** | .NET 10 (Windows) |
| **Encryption** | AES-256 / ProtectedData (DPAPI) |
| **Hashing** | SHA-256 |
| **RAM Security** | `CryptProtectMemory` & `VirtualLock` |
| **Deployment** | Single-file executable |

---

## 📥 Installation

You can find two versions of the application in the release section:

1.  **TPMPass - Framework Independent.exe:** Everything is included. You can run this directly on any modern Windows 10/11 system without installing any extra software.
2.  **TPMPass.exe:** A smaller file size version. This requires the **.NET 10 Runtime** to be installed on your system.

---

## 🏗 Build and Compilation

If you want to build the project from the source, follow these steps:

**Prerequisites:**
* .NET 10 SDK
* Windows OS (Required for DPAPI and Win32 APIs)

**Steps:**
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/TPMPass.git
   ```
2. Navigate to the project folder:
   ```bash
   cd TPMPass
   ```
3. To publish a **Self-Contained** (Framework Independent) single file:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
   ```
4. To build a standard version:
   ```bash
   dotnet build -c Release
   ```

---

> [!CAUTION]
> **CRITICAL WARNING:** Your data is strictly tied to your Windows installation. Reinstalling Windows, changing your user SID, or deleting the `user_master.dat` file will make your encrypted files **mathematically impossible** to recover.

---

*Developed with security in mind. Contributions are welcome!*