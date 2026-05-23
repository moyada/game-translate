# Game Translate Architecture

## V1 Scope

- Platform: Windows 10 / Windows 11 x64 only.
- UI: WPF on .NET 8.
- Model: `unsloth/Qwen3-1.7B-GGUF:UD-Q4_K_XL`.
- Model loading: lazy-loaded on first manual or automatic translation.
- Runtime: LLamaSharp in-process inference.
- Backend: `LLamaSharp.Backend.Cuda12.Windows`.
- OCR: PaddleSharp/PaddleOCR only. It uses the original color capture without color preprocessing.
- Glossary: persisted MapleStory terms in `Resources/Glossary/maplestory-glossary.json`, seeded from MapleStory Wiki and mxdzlk pages.
- CPU fallback: not supported.

## Pipeline

### Monitoring

1. User places a draggable capture rectangle over the chat area.
2. The app captures the selected region every 200 ms.
3. If the image or OCR text is unchanged, the app waits.
4. When content changes, PaddleOCR extracts source text from the original color capture.
5. `CudaLlamaTranslationService` lazy-loads the local GGUF model if needed, then translates English text to Simplified Chinese.
6. The WPF overlay/main window shows the latest translated result.

### Translate Button

1. User clicks translate after a capture rectangle exists.
2. The app captures the selected region once.
3. PaddleOCR extracts source text once.
4. `TranslationSourceNormalizer` removes player-name prefixes such as `Steam :`, OCR-damaged `Steam ;`, `Steam `, and `@Steam `.
5. `TranslationPromptBuilder` injects only glossary terms that match the current OCR text.
6. `CudaLlamaTranslationService` lazy-loads the local GGUF model if needed, then translates the OCR text once.
7. The displayed result is cleaned to remove Qwen thinking blocks such as `<think>...</think>` and chat stop tokens.

Stopping monitoring cancels the active monitoring session. If PaddleOCR is already inside a native predictor run, the app waits for that run to return before allowing a new monitoring session. PaddleOCR calls are serialized, and the OCR engine is recreated after each recognition so repeated manual translations do not reuse stale native predictor state.

The selection overlay is a topmost transparent WPF window, but its native hit test returns transparent outside the rectangle and resize handles. That keeps the selection box above the game while allowing normal clicks outside the box to pass through to the underlying app.

The selection overlay owns the enabled state of monitoring and translation. With no selected region both buttons are visible but disabled. Closing the selection overlay stops monitoring, clears the selected region, and returns both buttons to disabled state.

Glossary entries are stored as project data, not hard-coded in the prompt. The runtime keeps prompts small by selecting only entries whose English term or alias appears in the OCR text. The initial seed contains monsters, maps, NPCs, jobs, and common chat terms gathered from the specified MapleStory reference pages.

When a single chat line has a detected player prefix, the prefix is kept out of the LLM prompt and restored after generation as `用户名：译文`. This avoids translating names while still showing who spoke.

## Current Build

The current implementation completes lazy local model loading, single-shot selected-region translation, draggable region selection, selected-region screenshot capture, 200 ms image change detection, PaddleOCR text extraction, and automatic LLM translation. Result overlay presentation and OCR tuning are the next modules to add.

## Model Placement

During development, place the GGUF file at:

```text
src/GameTranslate/Models/Qwen3-1.7B-UD-Q4_K_XL.gguf
```

At runtime, the app chooses the first `*.gguf` file in the `Models` folder by file name. If none exists, it falls back to `Qwen3-1.7B-UD-Q4_K_XL.gguf`.

The file is intentionally ignored by Git.
