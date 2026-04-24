# Taskloom

<p align="center">
  <img src="Taskloom/Assets/Images/TaskloomBigLogo.png" alt="Taskloom logo" width="220" />
</p>

<p align="center">
  <b>Calendar-centered personal organizer for Windows</b><br/>
  Tasks, notes, events, day summaries, reminders, media attachments, themes, and tray mode in one local desktop app.
</p>

---

## ✨ What Taskloom Is

Taskloom is a Windows desktop organizer built around the idea that your day is more than a flat todo list.

Instead of forcing everything into one task model, Taskloom works with different dated record types:

- ✅ Tasks
- 📝 Notes
- 📅 Events
- 🌙 Day summaries

The app is designed as a local-first personal planner with a calendar-first workflow, fast daily navigation, and richer records with media and links.

## 🚀 Main Features

### 📆 Calendar-first workflow
- Main screen centered around a large calendar
- Fast date navigation
- Daily record list with filtering by record type
- Clear visual distinction between tasks, notes, events, and summaries

### 🧩 Multiple record types
- **Task**: title, details, completion state, reminder time
- **Note**: free-form dated note
- **Event**: time range, location, status, reminder before start
- **Day summary**: end-of-day recap tied to a date

### 🔔 Reminders and notifications
- Task reminders on the task date
- Event reminder before start
- Event start notification
- Windows notification center support via Windows App SDK
- Automatic fallback to tray balloon notifications if Windows App Runtime is unavailable
- Startup cleanup policies for old records with safe and aggressive modes

### 🖼️ Rich attachments
- Multiple image attachments per record
- Supported image formats:
  - `jpg`
  - `jpeg`
  - `png`
  - `bmp`
  - `gif`
- Image thumbnails in the record card
- Click to open the original file with the system viewer
- Per-record media folders under `%LocalAppData%\\Taskloom\\Records\\<record-id>`
- Sidecar `record.json` metadata file for local record resources

### 🎵 Audio attachments
- Multiple audio files per record
- Supported audio formats:
  - `mp3`
  - `wav`
  - `m4a`
  - `flac`
  - `wma`
  - `ogg`
- Embedded playlist inside the record card
- Play / pause / seek
- Track title from metadata when available
- File name fallback when metadata is missing
- Cover art support when present

### 🔗 Smart links in descriptions
Taskloom detects and makes links clickable directly inside record descriptions, including:

- `http://` and `https://`
- `ftp://`
- `tg://`
- `mailto:`
- `file://`
- plain domains like `youtube.com`
- IPv4 addresses like `192.168.1.10`
- email addresses

Links are opened through the system handler.

### 🎨 Theming and UX
- Built-in light, dark, OS-inspired, and custom themes
- Custom title bars styled to match the app
- Theme-aware controls
- Instant-apply settings for theme, language, and cleanup policy
- Adaptive record card layout
- Tray mode for background operation

### 🌍 Localization
- Russian: `ru-RU`
- English: `en-US`

## 🛠 Tech Stack

- **Language:** C#
- **Framework:** .NET 10
- **Desktop UI:** WPF
- **Architecture:** MVVM
- **MVVM Toolkit:** CommunityToolkit.Mvvm
- **Database:** SQLite
- **Data access:** Dapper
- **Audio metadata:** TagLibSharp
- **Windows notifications:** Windows App SDK

## 📦 Runtime Dependencies

To run Taskloom, the target machine needs:

- Windows 10 / 11
- .NET 10 Desktop Runtime
- Windows App Runtime 1.8 for notification center integration

If Windows App Runtime 1.8 is not installed or is incomplete, Taskloom still works, but notifications fall back to tray balloons instead of Windows notification center notifications.

## 🧱 Project Structure

The main application project is located in:

```text
Taskloom/
```

Important folders:

- `Assets` - icons, images, localization, themes
- `Common` - converters, helpers, shared text logic
- `Data` - SQLite models and SQL schema
- `Domain` - core record model
- `Infrastructure` - storage, repository, notifications, tray, media
- `Presentation` - WPF views and view models
- `Services` - application services and contracts
- `docs` - project documentation

## ▶️ How To Run

### Requirements
- Visual Studio 2022 / JetBrains Rider / .NET 10 SDK
- Windows machine with WPF support

### Build

From the repository root:

```powershell
dotnet build .\Taskloom.sln
```

### Run

```powershell
dotnet run --project .\Taskloom\Taskloom.csproj
```

If your local `dotnet` does not yet point to .NET 10, run the project from Rider or Visual Studio with the installed .NET 10 SDK.

## 💾 Local Data Storage

Taskloom stores user data under:

```text
%LocalAppData%\Taskloom
```

This includes:

- SQLite database
- settings file
- per-record folders with imported images
- per-record folders with imported audio files
- per-record folders with extracted audio covers
- per-record `record.json` metadata files
- diagnostic logs

## 📚 Documentation

Detailed project docs live in:

```text
Taskloom/docs
```

Key files:

- `ProjectOverview.md`
- `Architecture.md`
- `DevelopmentPlan.md`
- `Decisions.md`
- `WorkLog.md`
- `ContinuationGuide.md`

## 🌿 Development Notes

- Main working branch: `dev`
- Documentation is part of the project and should be updated with meaningful changes
- The app is intentionally local-first and desktop-focused

## 📌 Current State

Taskloom is already functional as a personal desktop organizer and continues to evolve.

The current focus is on:

- stability and edge-case hardening
- richer attachment UX
- continued refinement of the calendar-centered workflow
