# Game Translate Architecture

## V1 Scope

- Platform: Windows 10 / Windows 11 x64 only.
- UI: WPF on .NET 8.
- Model: `unsloth/Qwen3-1.7B-GGUF:UD-Q4_K_XL`.
- Runtime: LLamaSharp in-process inference.
- Backend: `LLamaSharp.Backend.Cuda12.Windows`.
- OCR: PaddleSharp/PaddleOCR only. It uses the original color capture without color preprocessing.
- CPU fallback: not supported.

## Pipeline

1. User places a draggable capture rectangle over the chat area.
2. The app captures the selected region every 200 ms.
3. If the image or OCR text is unchanged, the app waits.
4. When content changes, PaddleOCR extracts source text from the original color capture.
5. `CudaLlamaTranslationService` translates English text to Simplified Chinese.
6. The WPF overlay/main window shows the latest translated result.

## Current Build

The current implementation completes the local model loading, manual text translation path, draggable region selection, selected-region screenshot capture, 200 ms image change detection, PaddleOCR text extraction, and automatic LLM translation when the model is loaded. Result overlay presentation and OCR tuning are the next modules to add.

## Model Placement

During development, place the GGUF file at:

```text
src/GameTranslate/Models/Qwen3-1.7B-UD-Q4_K_XL.gguf
```

At runtime, the app chooses the first `*.gguf` file in the `Models` folder by file name. If none exists, it falls back to `Qwen3-1.7B-UD-Q4_K_XL.gguf`.

The file is intentionally ignored by Git.
