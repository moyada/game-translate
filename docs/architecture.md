# Game Translate Architecture

## V1 Scope

- Platform: Windows 10 / Windows 11 x64 only.
- UI: WPF on .NET 8.
- Model: `unsloth/Qwen3-1.7B-GGUF:UD-Q4_K_XL`.
- Model loading: lazy-loaded on first manual or automatic translation.
- Runtime: LLamaSharp in-process inference.
- Backend: `LLamaSharp.Backend.Cuda12.Windows`.
- OCR: PaddleSharp/PaddleOCR only. It uses the original color capture, then applies upscaling plus light contrast enhancement before recognition.
- Glossary: persisted MapleStory terms in `Resources/Glossary/maplestory-glossary.json`, seeded from MapleStory Wiki and mxdzlk pages.
- CPU fallback: not supported.

## Pipeline

### Monitoring

1. User places a draggable capture rectangle over the chat area.
2. The app captures the selected region on a randomized 400-600 ms interval.
3. `ImageChangeDetector` compares 16x16 sampled luminance points and treats the frame as changed when at least 3% of samples differ.
4. If the image or OCR text is unchanged, the app waits.
5. After 5 consecutive unchanged frames, monitoring forces one OCR + translation pass to catch missed low-contrast updates.
6. When content changes or the force rule fires, PaddleOCR extracts source text from the original color capture after upscaling and light contrast enhancement.
7. If the OCR source text is the same as the previous successfully translated source text, the LLM call is skipped.
8. `CudaLlamaTranslationService` lazy-loads the local GGUF model if needed, then translates English text to Simplified Chinese.
9. The WPF overlay/main window shows the latest translated result.

### Translate Button

1. User clicks translate after a capture rectangle exists.
2. The app captures the selected region once.
3. PaddleOCR extracts source text once.
4. `TranslationSourceNormalizer` removes player-name prefixes only when a real colon is present, such as `Steam :`, `Steam：`, or `@Steam :`.
5. `TranslationPromptBuilder` injects only glossary terms that match the current OCR text.
6. `CudaLlamaTranslationService` lazy-loads the local GGUF model if needed, then translates the OCR text once.
7. The displayed result is cleaned to remove Qwen thinking blocks such as `<think>...</think>` and chat stop tokens.

Stopping monitoring cancels the active monitoring session. If PaddleOCR is already inside a native predictor run, the app waits for that run to return before allowing a new monitoring session. PaddleOCR calls are serialized, and the OCR engine is recreated after each recognition so repeated manual translations do not reuse stale native predictor state.

The selection overlay is a topmost transparent WPF window, but its native hit test returns transparent outside the rectangle and resize handles. That keeps the selection box above the game while allowing normal clicks outside the box to pass through to the underlying app.

The selection overlay owns the enabled state of monitoring and translation. With no selected region both buttons are visible but disabled. Closing the selection overlay stops monitoring, clears the selected region, and returns both buttons to disabled state.

Glossary entries are stored as project data, not hard-coded in the prompt. The runtime keeps prompts small by selecting only entries whose English term or alias appears in the OCR text. The initial seed contains monsters, maps, NPCs, jobs, and common chat terms gathered from the specified MapleStory reference pages.

When a single chat line has a detected player prefix, the prefix is kept out of the LLM prompt and restored after generation as `用户名：译文`. This avoids translating names while still showing who spoke.

Username detection deliberately requires a real colon. Semicolon-like OCR output or a single space after a name is left as normal message text; the OCR preprocessing path is responsible for improving colon visibility instead of guessing separators.

## Current Build

The current implementation completes lazy local model loading, single-shot selected-region translation, draggable region selection, selected-region screenshot capture, randomized 400-600 ms image change detection, PaddleOCR text extraction, and automatic LLM translation. Result overlay presentation and OCR tuning are the next modules to add.

## Model Placement

During development, place the GGUF file at:

```text
src/GameTranslate/Models/Qwen3-1.7B-UD-Q4_K_XL.gguf
```

At runtime, the app chooses the first `*.gguf` file in the `Models` folder by file name. If none exists, it falls back to `Qwen3-1.7B-UD-Q4_K_XL.gguf`.

The file is intentionally ignored by Git.
