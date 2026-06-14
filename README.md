# AppRoulette

A modern Windows desktop application for managing and running roulette selections. Built with WinUI 3 and .NET 8, AppRoulette provides an intuitive interface for creating roulette groups, adding items, and spinning the roulette wheel to make random selections.

## Features

- **Multiple Roulette Groups**: Create and manage up to 3 roulette groups independently
- **Item Management**: Add up to 30 items per group with an intuitive UI
- **Animated Roulette Wheel**: Smooth spinning animation with realistic deceleration effects
- **Persistent Storage**: Automatically saves your groups and items using JSON serialization
- **MVVM Architecture**: Clean separation of concerns with ViewModel-based architecture
- **Cross-Platform Icons**: Generate dynamic icon files for application branding
- **Window State Management**: Remembers window size and position between sessions

## Technology Stack

- **Framework**: .NET 8
- **UI Framework**: WinUI 3 (Windows App SDK)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Data Persistence**: JSON-based file storage
- **Community Toolkit**: MVVM Toolkit for simplified property binding

## Project Structure

```
AppRoulette/
├── Models/
│   ├── RouletteItem.cs       # Represents a single roulette item
│   └── RouletteGroup.cs      # Represents a roulette group (max 30 items)
├── ViewModels/
│   └── MainViewModel.cs      # Main application logic and state management
├── Services/
│   ├── IDataPersistenceService.cs        # Data persistence interface
│   ├── JsonDataPersistenceService.cs    # JSON-based storage implementation
│   ├── IRandomService.cs                # Random number generation interface
│   ├── RandomService.cs                 # Standard random service
│   ├── IWindowPositionService.cs        # Window state persistence
│   ├── WindowPositionService.cs         # Window state implementation
│   └── AppIconService.cs                # Application icon generation
├── Views/
│   └── RouletteRenderer.cs   # Roulette wheel rendering logic
├── MainWindow.xaml          # Main application UI
├── MainWindow.xaml.cs       # Main window code-behind
└── App.xaml                 # Application resource definitions
```

## Key Components

### Models
- **RouletteItem**: Represents a single item in a roulette group with a display name
- **RouletteGroup**: Contains up to 30 RouletteItems with a group ID and display name (max 3 groups)

### ViewModels
- **MainViewModel**: Handles group/item management, roulette selection logic, and state persistence

### Services
- **JsonDataPersistenceService**: Loads/saves groups and items in JSON format
- **RandomService**: Provides random number generation for fair selections
- **WindowPositionService**: Manages application window size and position
- **AppIconService**: Generates application icons in ICO format

### Views
- **RouletteRenderer**: Renders the animated roulette wheel using graphics APIs

## Getting Started

### Prerequisites
- Windows 10/11 or later
- .NET 8 SDK
- Visual Studio 2022 or later (Community Edition supported)

### Building

1. Clone the repository:
   ```bash
   git clone https://github.com/SystemAzmax/AppRoulette.git
   ```

2. Open the solution file in Visual Studio:
   ```bash
   AppRoulette.slnx
   ```

3. Build the solution:
   ```bash
   dotnet build
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

## Usage

1. **Create Groups**: The application comes with 3 pre-defined roulette groups
2. **Add Items**: Select a group and add items using the input field
3. **Remove Items**: Select an item and remove it using the delete button
4. **Spin the Wheel**: Click the spin button to randomly select an item from the active group
5. **Data Persistence**: All changes are automatically saved to local storage

## Architecture Details

### MVVM Pattern
The application follows the MVVM pattern where:
- **Model**: `RouletteItem` and `RouletteGroup` classes manage data
- **View**: XAML UI defined in `MainWindow.xaml`
- **ViewModel**: `MainViewModel` handles all business logic and state management

### Two-way Data Binding
- Views communicate with ViewModels through data binding
- ViewModels update the View automatically when data changes
- User input in the View triggers ViewModel methods

### Service Injection
- Dependency injection is used to manage services
- Interfaces enable testing and flexibility
- Services handle cross-cutting concerns (persistence, randomization, etc.)

## Code Standards

- **Comments**: XML documentation comments for all public methods
- **Naming**: PascalCase for classes/methods, camelCase for variables
- **Constants**: UPPER_SNAKE_CASE for constant values
- **Private Fields**: Prefixed with underscore (_fieldName)
- **Formatting**: 80-character line limit, space-based indentation

## Testing

The project includes unit tests in the `AppRoulette.Tests` project. Run tests with:

```bash
dotnet test
```

## File Formats

### Data Storage
- Format: JSON
- Location: Local application directory
- Content: Serialized `RouletteGroup` objects with their items

### Icons
- Format: ICO (Windows Icon)
- Generated dynamically using `AppIconService`
- Used for application branding

## Performance Considerations

- **Roulette Animation**: Optimized with 60fps rendering
- **Data Persistence**: Lazy-loaded and cached for performance
- **Memory Management**: Efficient list usage with max item/group limits

## Security Considerations

- **File Access**: Uses standard Windows file APIs with appropriate permissions
- **User Input**: Items text is validated before storage
- **No External Dependencies**: Minimal dependency footprint reduces attack surface

## Contributing

Contributions are welcome! Please follow the coding standards outlined in the project's copilot-instructions.md file.

## License

[Add your license information here]

## Support

For issues, questions, or suggestions, please visit the [GitHub Issues](https://github.com/SystemAzmax/AppRoulette/issues) page.

## Roadmap

- [ ] Multi-language support (i18n)
- [ ] Custom color themes for roulette groups
- [ ] Import/Export functionality
- [ ] Animation customization options
- [ ] Sound effects during spin

---

Built with ❤️ using .NET 8 and WinUI 3
