# TPMPass

**TPMPass** is a secure, passwordless password manager for Windows. It leverages the Windows Data Protection API (DPAPI) to ensure that your identity is the only key needed to access your data.

> **The Motto:** This project isn't about "ultimate" paranoid-level security. The goal is to provide **reasonable security with maximum speed and convenience.** Access your files with a single click without having to remember a master password.

---

## 🚀 Key Features

* **Passwordless Security:** No master password to remember or lose. Encryption is bound to your Windows user account and physical machine.
* **Memory Protection:** Uses a custom ¨SecureBuffer¨ to encrypt sensitive data in RAM and lock memory pages to prevent swapping to disk.
* **Proactive Defense:** Includes anti-debugging and anti-dumping measures. It scans the calling process using Windows Defender to prevent automation by malicious software.
* **Dual Interface:** Full interactive console dashboard and a streamlined command-line interface for scripting.
* **Auto-Clear Clipboard:** Automatically clears your clipboard after a configurable duration to prevent password leakage.

---

## 📸 Visuals

<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/3ff2c5d9-3e31-4f0a-9385-cfbcb07d538a" />
<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/f8a68ff9-a87e-42fc-824e-9735abc40be9" />

**This is all you need to enter, you don't need to remember a password:**
<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/f90d7d61-80f5-4bc3-b780-8ba47338f6bb" />

<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/7e50364b-b367-4083-bf99-bd787f95aef4" />
<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/c1534c99-6e16-4b29-82aa-bac18788f7d4" />
<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/b4efed0e-d8df-4f49-ae9b-2f402e729783" />

Clipboard will be cleared after 30 minutes (if you don't close the app)
<img width="1483" height="762" alt="image" src="https://github.com/user-attachments/assets/7bd8d2fa-bdc1-44a1-9948-c56ca814a852" />

---

## 🛡️ Security Reality Check

Is this program 100% bulletproof? **No.** No security software is. Here is what you need to know:

### Why Use Windows Hello?
To truly leverage the "TPM" in TPMPass, you **must** set up a **Windows Hello PIN**. 
* **Standard Windows Passwords:** Are stored as hashes within the OS software (obfuscated and hashed).
* **Windows Hello PIN:** Unlike a regular password, the PIN is tied to your **TPM hardware**. The PIN acts as a local authorization to release keys sealed inside the TPM chip. By using a PIN, you move your identity from a software-based check to a hardware-backed security gate.

**Important Considerations:**
* **TPM Module:** If your computer lacks a TPM module, the program still works using Windows DPAPI (User and Machine binding), though it won't have the hardware-backed protection layer.
* **Physical Access:** If someone gains physical access to your PC and can log into your Windows session (or if you leave your PC unlocked), they can access your vault. Your Windows login is the gatekeeper.
* **RAT / Malware:** If your system is infected with a Remote Access Trojan (RAT), the attacker can act as "you" and potentially retrieve your data.
* **Windows Login Security:**
    * **Windows Pro users:** We recommend lowering the "Account Lockout Threshold" in group policies to prevent brute-force attacks on your Windows password.
    * **Windows Home users:** Since you have fewer policy options, ensure you have a strong, complex Windows password.
* **The Good News:** Windows already has built-in protections against rapid-fire password guessing. Since TPMPass relies on the OS's own security subsystem (DPAPI), it inherits these protections, making it highly effective for everyday use.

---

## 🛠 How It Works

TPMPass uses a multi-layered encryption approach to keep your data safe:

1.  **Master Identity:** Generates a unique 32-byte high-entropy key stored in ¨%AppData%¨.
2.  **OS Binding:** This identity is protected via **Windows DPAPI**, ensuring it cannot be used if copied to another machine or accessed by another user.
3.  **File Encryption:** Each ¨.tpmPassword¨ file uses its own unique salt, combined with the master identity, to derive a specific **AES-256** key.

---

## 💻 Usage

### Interactive Mode
Simply run the executable to open the dashboard and follow the menu prompts:
¨¨¨bash
TPMPass.exe
¨¨¨

### CLI Mode
You can use TPMPass in batch files or automated scripts:
* **Encrypt:** ¨TPMPass.exe --set "your_password" "filename.tpmPassword"¨
* **Decrypt & Print:** ¨TPMPass.exe --get "filename.tpmPassword"¨
* **Decrypt to Clipboard:** ¨TPMPass.exe "filename.tpmPassword" --noUI¨

---

## 📊 Technical Specifications

| Component | Technology |
| :--- | :--- |
| **Runtime** | .NET 10 (Windows) |
| **Encryption** | AES-256 / ProtectedData (DPAPI) |
| **Hashing** | SHA-256 |
| **RAM Security** | ¨CryptProtectMemory¨ & ¨VirtualLock¨ |
| **Deployment** | Single-file executable |

---

## 📥 Installation

You can find two versions of the application in the release section:

1.  **TPMPass - Framework Independent.exe:** Everything is included. You can run this directly on any modern Windows system.
2.  **TPMPass.exe:** A smaller file size version that requires the runtime to be present.

---

## 🏗 Build and Compilation

To build the project from source:

**Prerequisites:**
* .NET 10 SDK
* Windows OS

**Steps:**
1. Clone the repository:
   ¨¨¨bash
   git clone https://github.com/yourusername/TPMPass.git
   ¨¨¨
2. Navigate to the folder:
   ¨¨¨bash
   cd TPMPass
   ¨¨¨
3. To publish a **Framework Independent** single file:
   ¨¨¨bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
   ¨¨¨
4. To build a standard version:
   ¨¨¨bash
   dotnet build -c Release
   ¨¨¨

---

> [!CAUTION]
> **CRITICAL WARNING:** Your data is strictly tied to your Windows installation. Reinstalling Windows, changing your user SID, or deleting the ¨user_master.dat¨ file will make your encrypted files **mathematically impossible** to recover.

---

*Developed with efficiency in mind. Contributions are welcome!*
