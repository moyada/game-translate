# Game Translate

Windows 10 / Windows 11 x64 WPF app for translating selected game chat text with a local GGUF model.

## Current Decisions

- UI: WPF
- Target framework: .NET 8, `net8.0-windows`
- Model: `unsloth/Qwen3-1.7B-GGUF:UD-Q4_K_XL`
- Runtime: LLamaSharp in-process inference
- Backend: `LLamaSharp.Backend.Cuda12.Windows`
- CPU fallback: not supported in v1

## Requirements

- Windows 10 or Windows 11 x64
- NVIDIA GPU with a CUDA12-compatible driver
- .NET 8 SDK for development
- Visual Studio 2022 with `.NET desktop development`

## Model File

Download the `UD-Q4_K_XL` GGUF file from `unsloth/Qwen3-1.7B-GGUF`, then place it here:

```text
src/GameTranslate/Models/Qwen3-1.7B-UD-Q4_K_XL.gguf
```

At runtime, the app chooses the first `*.gguf` file in the `Models` folder by file name. If no GGUF file exists, it falls back to this expected path:

```text
Models/Qwen3-1.7B-UD-Q4_K_XL.gguf
```

For an installed/published app, put the model in the app folder:

```text
GameTranslate.exe
Models/Qwen3-1.7B-UD-Q4_K_XL.gguf
```

## Target Win11 Environment

Known target machine:

- GPU: RTX 4070 Ti Super 16GB
- Driver: 595.79
- Reported CUDA version: 13.2
- Installed runtime: .NET 8 runtime present
- Required install before build: .NET 8 SDK and Visual Studio 2022 `.NET desktop development`

## Build

```powershell
dotnet restore
dotnet build src/GameTranslate/GameTranslate.csproj -c Release -p:Platform=x64
```

## Test

```powershell
dotnet run --project tests/GameTranslate.Tests/GameTranslate.Tests.csproj -c Release -p:Platform=x64
```

## Publish

```powershell
dotnet publish src/GameTranslate/GameTranslate.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
```

## V1 Behavior

The current build is the first integration slice:

- Loads the local GGUF model through LLamaSharp.
- Forces CUDA backend selection and disables native backend fallback.
- Provides manual English input and Chinese translation output in WPF.
- Shows model load failures directly in the UI.

The next implementation slice should add target-window selection, draggable capture rectangle, screen capture, OCR, and 200 ms change detection.
