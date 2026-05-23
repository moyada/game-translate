# Game Translate

Windows 10 / Windows 11 x64 WPF app for translating selected game chat text with a local GGUF model.

## Current Decisions

- UI: WPF
- Target framework: .NET 8, `net8.0-windows10.0.19041.0`
- Model: `unsloth/Qwen3-1.7B-GGUF:UD-Q4_K_XL`
- Runtime: LLamaSharp in-process inference
- Backend: `LLamaSharp.Backend.Cuda12.Windows`
- CUDA runtime libraries: `NtvLibs.cuda12.cublas.runtime.win-x64`
- OCR: PaddleOCR through PaddleSharp
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

Manual download URL:

```text
https://huggingface.co/unsloth/Qwen3-1.7B-GGUF/resolve/main/Qwen3-1.7B-UD-Q4_K_XL.gguf?download=true
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

## Native CUDA Diagnostics

If model loading shows `The type initializer for 'LLama.Native.NativeApi' threw an exception.`, the real cause is usually in the inner exception. The app prints the full inner exception chain and lists the LLamaSharp native DLLs found in the output folder.

Check the output folder:

```powershell
dir src\GameTranslate\bin\x64\Release\net8.0-windows10.0.19041.0 -Recurse -Filter *.dll | findstr /i "llama ggml cuda"
```

Expected files include `runtimes\win-x64\native\cuda12\llama.dll`, `ggml.dll`, `ggml-base.dll`, `ggml-cuda.dll`, `cudart64_12.dll`, `cublas64_12.dll`, and `cublasLt64_12.dll`. The app explicitly loads the CUDA12 `llama.dll`; CPU fallback is not enabled.

## Test

```powershell
dotnet run --project tests/GameTranslate.Tests/GameTranslate.Tests.csproj -c Release -p:Platform=x64
```

## Manual Translation Check

After the model is loaded, test with short sentences first:

```text
Hello.
```

Expected output should be close to:

```text
你好。
```

Then test:

```text
I am ready.
```

Expected output should be close to:

```text
我准备好了。
```

## Publish

```powershell
dotnet publish src/GameTranslate/GameTranslate.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
```

## V1 Behavior

The current build is the first integration slice:

- Loads the local GGUF model through LLamaSharp.
- Forces CUDA backend selection and disables native backend fallback.
- Uses a full-screen draggable selection box directly over the chat area.
- Keeps the selection box visible while selecting; pressing Confirm or Enter saves the region and closes the topmost selection window so it does not block the game/app.
- Captures the selected screen region every 200 ms and detects image changes.
- Converts WPF selection coordinates to physical screen pixels to support Windows display scaling.
- Uses PaddleOCR on the original color capture without color preprocessing, then upscales it before recognition.
- Shows the raw capture preview and the PaddleOCR input preview for tuning.
- Runs PaddleOCR when the selected region changes, then writes recognized text into the source text box.
- Automatically translates changed OCR text when the CUDA model is loaded.
- Provides manual English input and Chinese translation output in WPF.
- Shows model load failures directly in the UI.

The next implementation slice should improve overlay/result presentation and tune OCR/change-detection behavior for real game chat.
