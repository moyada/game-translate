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
- Model loading: lazy-loaded on the first manual or automatic translation
- Monitoring: continuous capture -> PaddleOCR -> translate loop for the selected region
- Translate button: one-shot capture -> PaddleOCR -> translate for the selected region
- Translation prompt: MapleStory-focused chat prompt with a persisted glossary under `Resources/Glossary`
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

- Loads the local GGUF model through LLamaSharp only when translation is first requested.
- Hides the GGUF model path from the UI and lazy-loads the model on the first translation.
- Forces CUDA backend selection and disables native backend fallback.
- Uses a topmost draggable selection box directly over the chat area.
- The selection box has no confirm/cancel buttons. Region changes sync live while dragging.
- Mouse operations outside the selection box pass through to the game/app below, so the topmost overlay does not block normal screen interaction.
- Captures the selected screen region every 200 ms and detects image changes.
- Treats a frame as changed when at least 3% of sampled points differ from the previous frame.
- Forces one OCR + translation pass after 5 consecutive unchanged frames.
- Skips LLM translation when the OCR source text is identical to the previous successfully translated source text.
- Converts WPF selection coordinates to physical screen pixels to support Windows display scaling.
- Uses PaddleOCR on the original color capture, then upscales it before recognition.
- Applies stronger light contrast enhancement and sharpening after upscaling to improve low-contrast punctuation such as chat-name colons.
- Recreates the PaddleOCR engine after each recognition to avoid stale native predictor state on repeated manual translations.
- Cancels the active monitoring session on stop and waits for any in-flight PaddleOCR run to finish before allowing a new monitoring session.
- Shows the raw capture preview and the PaddleOCR input preview for tuning.
- Monitoring runs a continuous loop over the selected region: capture, skip unchanged images/text, PaddleOCR, then translate changed OCR text.
- The translate button runs a single pass over the selected region: capture once, PaddleOCR once, then translate once.
- Writes recognized OCR text into the source text box.
- Strips player-name prefixes only when OCR keeps a real colon, such as `Steam : message`, `Steam：message`, or `@Steam : message`, then restores the original name as `Steam：译文`. Semicolons and single spaces are not treated as username separators.
- Loads relevant MapleStory glossary entries from `Resources/Glossary/maplestory-glossary.json` and injects only matching terms into the prompt.
- Automatically lazy-loads the CUDA model before the first manual or monitoring translation.
- Removes Qwen thinking blocks such as `<think>...</think>` and chat stop tokens from the displayed translation result.
- Uses one Start/Stop monitoring button. The monitoring and translation buttons stay visible but are disabled until a capture region exists.
- Closing the selection overlay stops monitoring and clears the capture region so monitoring/translation become unavailable again.
- Provides manual English input and Chinese translation output in WPF.
- Shows model load failures directly in the UI.

The next implementation slice should improve overlay/result presentation and tune OCR/change-detection behavior for real game chat.
