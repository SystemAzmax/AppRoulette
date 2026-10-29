# AppRoulette

A Windows desktop app that spins a roulette wheel to pick one item from the items you register. Built with WinUI 3 and .NET 10, it is available as a Microsoft Store version and as an exe version that needs no installation.

## Features

- **Multiple Lists**: Starts with 9 lists (Roulette1 to Roulette9); you can add, delete, duplicate, reorder, and rename lists
- **Item Editing**: Register up to 30 items per list. Edit them in a table on the "詳細" (Details) tab, or type or paste one item per line on the "テキスト" (Text) tab
- **Weights**: Give each item a weight from 1 to 5; items with larger weights are more likely to be picked
- **Excluding Items**: Unchecked items are left out of the draw. With "当たったら除外" (exclude on win) turned on, the picked item is excluded automatically
- **Animated Roulette Wheel**: A wheel that slows down before it stops, drawn with Win2D
- **Automatic Saving**: Changes are saved automatically to a SQLite database
- **CSV Import and Export**: Read and write the items of the selected list as a CSV file
- **Backup and Restore**: Save all lists and items to one file, and restore them from it
- **OneDrive Sync**: On PCs that use OneDrive, sync lists with your other PCs through the OneDrive folder
- **Window**: Opens with the previous size and position, and can stay on top of other windows
- **Error Notifications**: Save failures and unexpected errors are shown at the top of the window and recorded in a log file

## Technology Stack

- **Framework**: .NET 10
- **UI Framework**: WinUI 3 (Windows App SDK)
- **Rendering**: Win2D
- **Data Persistence**: SQLite (Microsoft.Data.Sqlite)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Community Toolkit**: MVVM Toolkit for property binding and commands
- **Testing**: xUnit
- **Distribution**: MSIX package (Microsoft Store), and an exe that includes .NET and the Windows App SDK

## Project Structure

```
AppRoulette.slnx                  # Solution
AppRoulette/
├── AppRoulette/                  # The app (WinUI 3)
│   ├── App.xaml(.cs)             # Startup and handling of unexpected exceptions
│   ├── MainWindow.xaml(.cs)      # Main window
│   ├── Services/                 # Dialogs, window position and size, app information
│   ├── Views/                    # Roulette wheel rendering (Win2D)
│   └── ThirdPartyNotices.txt     # Licenses of the software the app uses
├── AppRoulette.Core/             # Code that does not depend on WinUI
│   ├── Models/                   # Lists, items, and data for sync and backup
│   ├── Services/                 # SQLite storage, OneDrive sync, backup, CSV, logging
│   └── ViewModels/               # Screen state and logic, spin animation calculations
├── AppRoulette.Tests/            # Unit tests
└── AppRoulette (Package)/        # MSIX package for the Store (manifest and package images)
docs/                             # Developer documents (build guide, how OneDrive sync works)
tools/                            # Scripts that create the icon, package images, and license notices
```

## Key Components

### Models (AppRoulette.Core)
- **RouletteGroup**: A list, with a display name, up to 30 items, and the exclude-on-win setting
- **RouletteItem**: An item, with a name, a weight, and whether it is enabled

### ViewModels (AppRoulette.Core)
- **MainViewModel**: Manages lists and items, and handles drawing, saving, syncing, and error notifications
- **SpinAnimationViewModel**: Holds the spin animation state and calculates where the wheel stops

### Services (AppRoulette.Core)
- **SqliteGroupRepository / SqliteItemRepository / SqliteAppSettingsRepository**: Read and write SQLite
- **DatabaseInitializer**: Creates the database and its tables
- **OneDriveFolderSyncService / SyncTimestampStore / SyncConflictResolver**: Sync through the OneDrive folder
- **BackupSerializer / SqliteBackupRepository**: Create and restore backups
- **CsvFileEncoding**: Detects and converts the text encoding of CSV files
- **ApplicationLogger / ErrorLogFile**: Error log
- **RandomService**: Random numbers for the draw

### Views (AppRoulette)
- **MainWindow**: Main window
- **RouletteRenderer**: Draws the roulette wheel with Win2D
- **ContentDialogService**: Confirmation and message dialogs
- **WindowPositionService / WindowSizeCalculator**: Save and calculate the window position and size

## Getting Started

### Prerequisites
- Windows 10 version 1809 or later, or Windows 11
- .NET 10 SDK
- Visual Studio 2026 or later (Community Edition supported)

### Building and Running

1. Clone the repository:
   ```bash
   git clone https://github.com/SystemAzmax/AppRoulette.git
   ```

2. Open `AppRoulette.slnx` in Visual Studio

3. Set **AppRoulette (Package)** as the startup project and press F5

- The build output of the `AppRoulette` project (`bin\...\AppRoulette.exe`) is meant for the package, so it crashes if you start it directly.
- To publish the exe version or create a package for the Store, see the [build guide](docs/BuildGuide.md) (in Japanese).

## Usage

1. **Choose a list**: Select the list to use in the combo box at the top right
2. **Register items**: Add items with **アイテム追加** (Add Item) on the "詳細" tab, then enter the name and weight. On the "テキスト" tab, type or paste one item per line as "name,weight" (the weight is 1 if omitted)
3. **Spin the wheel**: Press **スタート** (Start), click the wheel, or press Space or Enter (except while typing text)
4. **Exclude picked items**: To leave picked items out of later draws, turn on **当たったら除外**
5. **Saving**: Changes are saved automatically

## Architecture Details

### MVVM Pattern
- **Model**: `RouletteGroup` and `RouletteItem` represent the data
- **View**: `MainWindow.xaml` defines the screen and binds to the ViewModel with `x:Bind`
- **ViewModel**: `MainViewModel` holds the screen state and logic

### Project Separation
- Code that does not depend on WinUI (ViewModels, storage, sync, and so on) is in `AppRoulette.Core` and is tested by `AppRoulette.Tests`
- Code that depends on the UI or Windows features (dialogs, the window, rendering) is in `AppRoulette`

### Passing Services
- `MainWindow` creates the services and passes them to the `MainViewModel` constructor (no DI container is used)
- Services are received through interfaces, so tests replace them with fakes (`AppRoulette.Tests/Fakes`)

## Code Standards

See [.github/copilot-instructions.md](.github/copilot-instructions.md) (in Japanese) for details.

- **Comments**: XML documentation comments for all methods
- **Naming**: PascalCase for classes/methods, camelCase for variables
- **Constants**: UPPER_SNAKE_CASE for constant values
- **Private Fields**: Prefixed with underscore (`_fieldName`)
- **Formatting**: 80-character line limit, space-based indentation

## Testing

The project includes unit tests in the `AppRoulette.Tests` project. Run tests with:

```bash
dotnet test AppRoulette/AppRoulette.Tests/AppRoulette.Tests.csproj -c Release -p:Platform=x64
```

GitHub Actions (`.github/workflows/build.yml`) also runs the tests and publishes the exe version, for example on pushes to the `main` and `product*` branches.

## Data and Files

### Storage Locations

| | Store version | exe version |
| --- | --- | --- |
| Data (SQLite) | `%LOCALAPPDATA%\Packages\<package name>\LocalCache\Local\AppRoulette\roulette.db` | `%LOCALAPPDATA%\AppRoulette\roulette.db` |
| Error log | `%LOCALAPPDATA%\Packages\<package name>\LocalState\logs\error.log` | `%LOCALAPPDATA%\AppRoulette\logs\error.log` |
| OneDrive sync | `%OneDrive%\Apps\AppRoulette\groups\<list ID>.json` | Same as the Store version |

### File Formats
- **CSV**: The first line is a header (`アイテム名,Weight`), followed by one item per line. Files are written in UTF-8 with a BOM; UTF-8, UTF-16, and Shift_JIS files can be read
- **Backup**: A JSON file that contains all lists and items
- **OneDrive sync**: One JSON file per list (see [how OneDrive folder sync works](docs/OneDriveSyncSetup.md), in Japanese)

### Icons
- Executable and window icon: `AppRoulette/AppRoulette/Copilot_20260614_202528.ico` (ICO with sizes from 16 to 256 px)
- Store package images: `AppRoulette/AppRoulette (Package)/Images/`
- Source image: `docs/Copilot_20260614_202528.png` (see the [build guide](docs/BuildGuide.md) for how to recreate the icons)

## Performance Considerations

- **Roulette Animation**: Drawn at about 60 fps (every 16 ms)
- **Automatic Saving**: Changes made in quick succession are saved together in one write
- **Distribution Size**: Only the Windows App SDK components the app uses are referenced, so DLLs for unused features are not included

## Security Considerations

- **Network**: The app does not communicate over the network. OneDrive sync only reads and writes files in the OneDrive folder on the PC; the OneDrive client syncs them with the cloud
- **Data Location**: Data is stored in a per-user folder (`%LOCALAPPDATA%`)
- **Database**: Values entered by the user are passed to SQLite through parameterized queries

## Contributing

Contributions are welcome! Please follow the coding standards in [.github/copilot-instructions.md](.github/copilot-instructions.md).

## License

[Add your license information here]

For the licenses of the software the app uses, see `AppRoulette/AppRoulette/ThirdPartyNotices.txt` (also opened by the "ライセンス情報" (License Information) button in the app).

## Support

For issues, questions, or suggestions, please visit the [GitHub Issues](https://github.com/SystemAzmax/AppRoulette/issues) page.

## Roadmap

- [ ] Multi-language support (i18n)
- [ ] Custom color themes for lists
- [x] Import/Export functionality (CSV, backup)
- [ ] Animation customization options
- [ ] Sound effects during spin

---

Built with ❤️ using .NET 10 and WinUI 3
