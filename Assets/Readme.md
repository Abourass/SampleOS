# Terminal Simulator

## Project Organization

```txt
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Terminal/
│   │   │   ├── TerminalController.cs
│   │   │   ├── TerminalInputHandler.cs
│   │   │   ├── TerminalOutputHandler.cs
│   │   │   └── TerminalHistory.cs
│   │   ├── CommandSystem/
│   │   │   ├── CommandProcessor.cs
│   │   │   ├── ICommand.cs
│   │   │   └── Commands/
│   │   │       ├── LsCommand.cs
│   │   │       ├── CdCommand.cs
│   │   │       ├── MkdirCommand.cs
│   │   │       └── ... (other commands)
│   │   ├── FileSystem/
│   │   │   ├── VirtualFileSystem.cs
│   │   │   ├── VirtualNode.cs
│   │   │   ├── FileSystemFactory.cs
│   │   │   └── IFileSystemVisitor.cs
│   │   └── Networking/
│   │       ├── VirtualNetwork.cs
│   │       ├── RemoteSystem.cs
│   │       └── NetworkedFileSystem.cs
│   ├── Utilities/
│   │   ├── StringParser.cs
│   │   ├── PathUtility.cs
│   │   └── ColorUtility.cs
│   └── Config/
│       ├── TerminalConfig.cs
│       └── SystemConfig.cs
├── Prefabs/
│   ├── Terminal.prefab
│   └── RemoteSystem.prefab
├── UI/
│   ├── Themes/
│   └── Fonts/
└── Resources/
    └── DefaultFileSystem.json
```
