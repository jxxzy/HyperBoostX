# HyperBoostX - WPF Client

A modern C# Windows Presentation Foundation (WPF) client for HyperBoostX system optimization platform. This client communicates with the Python backend via REST API.

## Architecture

```
┌─────────────────────┐
│   WPF Client        │  (C# .NET 6.0)
│  - XAML UI          │  
│  - Page Navigation  │  
│  - Backend Client   │  
└──────────┬──────────┘
           │  HTTP REST API (JSON)
           ↓
┌─────────────────────┐
│ Python Flask Server │  (Python 3.8+)
│  - API Endpoints    │
│  - Services Layer   │
│  - System Access    │
└─────────────────────┘
```

## Prerequisites

- **Windows 10/11** (or Windows Server 2019+)
- **.NET 6.0 SDK** or **Visual Studio 2022** (.NET Desktop Development)
- **Newtonsoft.Json NuGet package** (will be auto-restored)
- **Python backend** running on localhost:5000 (see Python project README)

## Project Structure

```
wpf/
├── HyperBoostX.csproj              # Project file
├── App.xaml                         # Application root XAML
├── App.xaml.cs                      # Application code-behind
├── MainWindow.xaml                  # Main window UI definitions
├── MainWindow.xaml.cs               # Main window event handlers and logic
├── Services/
│   └── HyperBoostBackendClient.cs   # REST API client for backend
└── Properties/
    └── AssemblyInfo.cs              # Assembly metadata
```

## Key Features

### Pages Implemented
- **Dashboard**: Real-time CPU, Memory, Disk usage with booster profiles
- **System Info**: Complete system information (CPU, GPU, memory, disk, network, OS, temps)
- **Booster**: Apply gaming optimization profiles (FPS, Latency, Streaming, Balanced)
- **Drivers**: View installed drivers and check for updates
- **Repair**: System repair tools (SFC, DISM, Cleanup)
- **Tweaks**: Apply Windows optimization tweaks
- **Settings**: Backend configuration and connection testing

### UI Design
- Dark gaming theme matching Python PySide6 client
- Color scheme:
  - Background: `#1e1e1e` (Dark)
  - Panels: `#2c2c2c` (Darker gray)
  - Accent: `#2196F3` (Blue)
  - Success: `#4CAF50` (Green)
  - Warning: `#ff9800` (Orange)
  - Error: `#f44336` (Red)
- Sidebar navigation with 7 main sections
- Real-time stats cards with progress indicators
- Responsive layout

### API Client Features

The `HyperBoostBackendClient` class provides methods for all operations:

```csharp
// System Information
await client.GetSystemInfoAsync()       // Complete system info
await client.GetSystemStatsAsync()      // Real-time stats

// Booster Operations
await client.GetBoosterProfilesAsync()  // Available profiles
await client.ApplyBoosterAsync(profile) // Apply profile

// System Tools
await client.RunSfcAsync()              // Run SFC scan
await client.RunDismAsync()             // Run DISM repair
await client.CleanupAsync()             // Cleanup temp files

// Driver Management
await client.GetDriversAsync()          // List drivers
await client.CheckDriverUpdatesAsync()  // Check for updates

// Tweaks Management
await client.GetTweaksAsync()           // List available tweaks
await client.ApplyTweakAsync(tweakId)  // Apply specific tweak

// Network Tools
await client.TestDnsAsync()             // Test DNS resolution
await client.FlushDnsAsync()            // Flush DNS cache
await client.OptimizeTcpAsync()         // Optimize TCP settings

// Startup Management
await client.GetStartupItemsAsync()     // List startup items

// Connection Management
await client.HealthCheckAsync()         // Check backend status
```

## Building the Project

### Option 1: Visual Studio 2022
1. Open `HyperBoostX.csproj` in Visual Studio 2022
2. Build → Build Solution (Ctrl+Shift+B)
3. Run → Start Debugging (F5)

### Option 2: Command Line (.NET CLI)
```bash
# Navigate to wpf directory
cd wpf

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Option 3: Publish as Executable
```bash
cd wpf

# Publish as self-contained executable
dotnet publish -c Release --self-contained

# Binary location: bin/Release/net6.0-windows/publish/HyperBoostX.exe
```

## Running the Application

### Prerequisites
1. **Start Python Backend** (from Python project directory):
   ```bash
   python -m app.backend_server
   # Server will start on http://127.0.0.1:5000
   ```

2. **Run WPF Client**:
   ```bash
   dotnet run
   # Or double-click HyperBoostX.exe if published
   ```

### Backend Configuration
In the **Settings** page:
- View current backend URL (default: `http://127.0.0.1:5000`)
- Change backend URL if running on different host/port
- Click "Test Backend Connection" to verify connectivity
- Backend status indicator shows real-time connection status:
  - 🟢 Green: Connected
  - 🔴 Red: Disconnected

## Error Handling

The client includes comprehensive error handling:

### Connection Errors
- Timeout: 10 seconds per request
- Retry: Manual via UI buttons
- Status: Visual indicator in Settings page

### API Errors
- All operations wrapped in try/catch
- Error details displayed in MessageBox dialogs
- Graceful fallback if backend is offline

### Data Parsing Errors
- JSON deserialization errors logged
- Alternative display format available
- Detailed error messages shown to user

## API Communication Details

### Request Format
```json
POST /api/booster/apply
Content-Type: application/json
{
    "profile": "fps"
}
```

### Response Format
```json
HTTP/1.1 200 OK
Content-Type: application/json
{
    "success": true,
    "message": "Booster profile applied successfully",
    "details": {
        "profile": "fps",
        "changes_applied": 12,
        "duration_ms": 345
    }
}
```

### Error Response
```json
HTTP/1.1 500 Internal Server Error
Content-Type: application/json
{
    "success": false,
    "error": "Error message",
    "details": "Stack trace or additional info"
}
```

## Dependencies

### NuGet Packages
- **Newtonsoft.Json 13.0.3**: JSON serialization/deserialization
  - Handles complex JSON responses from backend
  - Provides `FormatJson()` for pretty-printing

### .NET Framework
- **System.Net.Http**: HTTP client support
- **System.Threading.Tasks**: Async/await support
- **System.Windows**: WPF framework

## Development Guidelines

### Adding New Features

1. **Add API Method to Backend Client**:
   ```csharp
   public async Task<dynamic> NewFeatureAsync()
   {
       try
       {
           var response = await _httpClient.GetAsync($"{_baseUrl}/api/new-feature");
           response.EnsureSuccessStatusCode();
           var json = await response.Content.ReadAsStringAsync();
           return JsonConvert.DeserializeObject(json);
       }
       catch (Exception ex)
       {
           throw new InvalidOperationException($"Failed to call new feature: {ex.Message}", ex);
       }
   }
   ```

2. **Add UI Page** (new StackPanel in MainWindow.xaml):
   ```xaml
   <StackPanel Name="NewFeatureContent" Visibility="Collapsed">
       <TextBlock Text="Feature Title" FontSize="16" Foreground="#2196F3" FontWeight="Bold"/>
       <!-- UI elements here -->
   </StackPanel>
   ```

3. **Add Navigation Button** (in MainWindow.xaml):
   ```xaml
   <Button Name="NewFeatureBtn" 
           Content="New Feature" 
           Click="NewFeatureBtn_Click"
           Style="{StaticResource NavButtonStyle}"/>
   ```

4. **Add Event Handler** (in MainWindow.xaml.cs):
   ```csharp
   private void NewFeatureBtn_Click(object sender, RoutedEventArgs e) 
       => ShowPage("NewFeature", NewFeatureBtn);
   ```

## Performance Considerations

- **UI Thread**: All long-running operations use async/await to prevent freezing
- **Background Tasks**: HTTP requests run on thread pool
- **Memory**: Client is lightweight (~5MB when running)
- **Network**: JSON serialization optimized for bandwidth

## Troubleshooting

### Backend Connection Issues
**Problem**: "Backend: Disconnected" status
- Verify Python backend is running: `python -m app.backend_server`
- Check backend URL in Settings (default: localhost:5000)
- Ensure firewall allows localhost connections

### JSON Parsing Errors
**Problem**: "Error loading system info: Could not parse JSON response"
- Check Python backend logs for API errors
- Verify backend version matches expected API format
- Test connection with `curl http://localhost:5000/api/health`

### Missing Newtonsoft.Json
**Problem**: Runtime error about missing Newtonsoft.Json
- Run `dotnet restore` to install NuGet packages
- Check Internet connection for package download
- Try `dotnet nuget locals all --clear` if issues persist

## Security Notes

⚠️ **Important Considerations**:

1. **Local Network Only**: By default client connects to `127.0.0.1:5000`
   - For remote backend: Update URL in Settings
   - Consider VPN or firewall rules for security

2. **Admin Privileges**: Backend requires admin rights for sensitive operations
   - Display "Allow" dialog when triggered
   - Verify operations before confirming

3. **No Authentication**: Current implementation has no auth
   - Deploy backend behind firewall only
   - Consider adding API key authentication for production

## Version Information

- **Client Version**: 1.3.0
- **.NET Version**: 6.0 (NET6.0-Windows)
- **C# Version**: 10.0+
- **Target OS**: Windows 10/11, Windows Server 2019+

## Related Projects

- **Python Backend**: `../app/` directory
  - Flask REST API server
  - Service implementations
  - System access layer

- **Python PySide6 Client (legacy/dev only)**: `../app/dev_client.py`
  - Alternative Python-based UI for development and testing
  - Same backend API compatibility

## License

Proprietary - HyperBoostX by Mr.4NONY

## Support

For issues with the WPF client:
1. Check troubleshooting section above
2. Review Python backend logs
3. Verify all prerequisites are installed
4. Test with sample curl commands to backend API

## Future Enhancements

- [ ] Charts and graphs (WPF Toolkit)
- [ ] Real-time monitoring overlay
- [ ] Profile management UI
- [ ] Batch operations
- [ ] API authentication
- [ ] Dark/Light theme switcher
- [ ] Localization support
