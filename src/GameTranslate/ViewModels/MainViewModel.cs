using GameTranslate.Services;
using GameTranslate.Models;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameTranslate.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly ITranslationService _translationService;
    private readonly IScreenCaptureService _screenCaptureService;
    private IOcrService _ocrService;
    private readonly DispatcherTimer _captureTimer;
    private readonly RelayCommand _loadModelCommand;
    private readonly RelayCommand _translateCommand;
    private readonly RelayCommand _startMonitoringCommand;
    private readonly RelayCommand _stopMonitoringCommand;
    private string _modelPath;
    private string _sourceText = "hello, team. the boss is spawning near the bridge.";
    private string _translatedText = string.Empty;
    private string _statusText = "模型未加载";
    private string _captureStatusText = "截图监控未开始";
    private CaptureSelection _captureSelection = new(default, default, 1, 1);
    private OcrBackendChoice _selectedOcrBackendChoice = OcrBackendCatalog.DefaultChoice;
    private ImageSource? _latestCaptureImage;
    private ImageSource? _latestOcrImage;
    private ImageFingerprint? _lastFingerprint;
    private string _lastOcrText = string.Empty;
    private int _captureCount;
    private int _changedFrameCount;
    private bool _isModelLoaded;
    private bool _isMonitoring;
    private bool _isCapturing;

    public MainViewModel()
        : this(new CudaLlamaTranslationService(), new ScreenCaptureService(), OcrServiceFactory.Create(OcrBackendCatalog.DefaultBackend))
    {
    }

    public MainViewModel(
        ITranslationService translationService,
        IScreenCaptureService screenCaptureService,
        IOcrService ocrService)
    {
        _translationService = translationService;
        _screenCaptureService = screenCaptureService;
        _ocrService = ocrService;
        _captureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _captureTimer.Tick += async (_, _) => await CaptureTickAsync();
        _modelPath = ModelPathResolver.GetDefaultModelPath(AppContext.BaseDirectory);
        _loadModelCommand = new RelayCommand(LoadModelAsync, () => !_isModelLoaded);
        _translateCommand = new RelayCommand(TranslateAsync, () => _isModelLoaded && !string.IsNullOrWhiteSpace(SourceText));
        _startMonitoringCommand = new RelayCommand(StartMonitoringAsync, () => !IsMonitoring && !CaptureSelection.IsEmpty);
        _stopMonitoringCommand = new RelayCommand(StopMonitoringAsync, () => IsMonitoring);
    }

    public string ModelPath
    {
        get => _modelPath;
        set => SetProperty(ref _modelPath, value);
    }

    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (SetProperty(ref _sourceText, value))
            {
                _translateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string TranslatedText
    {
        get => _translatedText;
        set => SetProperty(ref _translatedText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string CaptureStatusText
    {
        get => _captureStatusText;
        set => SetProperty(ref _captureStatusText, value);
    }

    public CaptureSelection CaptureSelection
    {
        get => _captureSelection;
        private set
        {
            if (SetProperty(ref _captureSelection, value))
            {
                OnPropertyChanged(nameof(CaptureRegionText));
                _startMonitoringCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CaptureRegionText => CaptureSelection.ToDisplayText();

    public ImageSource? LatestCaptureImage
    {
        get => _latestCaptureImage;
        set => SetProperty(ref _latestCaptureImage, value);
    }

    public ImageSource? LatestOcrImage
    {
        get => _latestOcrImage;
        set => SetProperty(ref _latestOcrImage, value);
    }

    public int CaptureCount
    {
        get => _captureCount;
        set
        {
            if (SetProperty(ref _captureCount, value))
            {
                OnPropertyChanged(nameof(CaptureMetricsText));
            }
        }
    }

    public int ChangedFrameCount
    {
        get => _changedFrameCount;
        set
        {
            if (SetProperty(ref _changedFrameCount, value))
            {
                OnPropertyChanged(nameof(CaptureMetricsText));
            }
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                _startMonitoringCommand.RaiseCanExecuteChanged();
                _stopMonitoringCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(IsOcrBackendSelectionEnabled));
            }
        }
    }

    public string CaptureMetricsText => $"截图 {CaptureCount} 次，变化 {ChangedFrameCount} 次";

    public IReadOnlyList<OcrBackendChoice> OcrBackendChoices => OcrBackendCatalog.Choices;

    public OcrBackendChoice SelectedOcrBackendChoice
    {
        get => _selectedOcrBackendChoice;
        set
        {
            if (IsMonitoring || _isCapturing)
            {
                CaptureStatusText = "请先停止监控，再切换 OCR 后端";
                OnPropertyChanged(nameof(SelectedOcrBackendChoice));
                return;
            }

            if (value is null || !SetProperty(ref _selectedOcrBackendChoice, value))
            {
                return;
            }

            ReplaceOcrService(value.Backend);
        }
    }

    public bool IsOcrBackendSelectionEnabled => !IsMonitoring && !_isCapturing;

    public RelayCommand LoadModelCommand => _loadModelCommand;

    public RelayCommand TranslateCommand => _translateCommand;

    public RelayCommand StartMonitoringCommand => _startMonitoringCommand;

    public RelayCommand StopMonitoringCommand => _stopMonitoringCommand;

    public void SetCaptureSelection(CaptureSelection captureSelection)
    {
        if (CaptureSelection == captureSelection)
        {
            return;
        }

        CaptureSelection = captureSelection;
        _lastFingerprint = null;
        _lastOcrText = string.Empty;
        StatusText = "已选择截图区域";
        CaptureStatusText = "截图区域已更新";
    }

    private async Task LoadModelAsync()
    {
        try
        {
            StatusText = "正在加载 CUDA 模型...";
            await _translationService.LoadAsync(TranslationOptions.CreateDefault(ModelPath));
            _isModelLoaded = true;
            StatusText = "CUDA 模型已加载";
        }
        catch (Exception ex)
        {
            _isModelLoaded = false;
            StatusText = "模型加载失败";
            TranslatedText = ExceptionFormatter.Format(ex) + Environment.NewLine + Environment.NewLine + NativeDependencyDiagnostics.CreateReport(AppContext.BaseDirectory);
        }
        finally
        {
            _loadModelCommand.RaiseCanExecuteChanged();
            _translateCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task TranslateAsync()
    {
        try
        {
            StatusText = "正在翻译...";
            TranslatedText = await _translationService.TranslateToChineseAsync(SourceText);
            StatusText = "翻译完成";
        }
        catch (Exception ex)
        {
            StatusText = "翻译失败";
            TranslatedText = ExceptionFormatter.Format(ex);
        }
    }

    private async Task StartMonitoringAsync()
    {
        if (CaptureSelection.IsEmpty)
        {
            CaptureStatusText = "请先选择截图区域";
            return;
        }

        _lastFingerprint = null;
        _lastOcrText = string.Empty;
        CaptureCount = 0;
        ChangedFrameCount = 0;
        IsMonitoring = true;
        CaptureStatusText = "正在监控截图区域";
        _captureTimer.Start();
        await CaptureTickAsync();
    }

    private Task StopMonitoringAsync()
    {
        _captureTimer.Stop();
        IsMonitoring = false;
        CaptureStatusText = "截图监控已停止";
        return Task.CompletedTask;
    }

    private async Task CaptureTickAsync()
    {
        if (!IsMonitoring || _isCapturing)
        {
            return;
        }

        _isCapturing = true;
        try
        {
            var frame = await Task.Run(() => _screenCaptureService.Capture(CaptureSelection.PixelRegion));
            var fingerprint = ImageChangeDetector.CreateFingerprint(frame.BgraPixels, frame.Width, frame.Height, frame.Stride);
            var changed = ImageChangeDetector.HasMeaningfulChange(_lastFingerprint, fingerprint);

            _lastFingerprint = fingerprint;
            LatestCaptureImage = frame.Preview;
            LatestOcrImage = OcrPreviewBuilder.CreatePreview(frame);
            CaptureCount++;

            if (changed)
            {
                ChangedFrameCount++;
                await RunOcrAsync(frame);
            }
            else
            {
                CaptureStatusText = "区域无变化，等待";
            }
        }
        catch (Exception ex)
        {
            _captureTimer.Stop();
            IsMonitoring = false;
            CaptureStatusText = ExceptionFormatter.Format(ex);
        }
        finally
        {
            _isCapturing = false;
        }
    }

    private async Task RunOcrAsync(CapturedFrame frame)
    {
        CaptureStatusText = $"检测到区域变化，正在 OCR：{SelectedOcrBackendChoice.DisplayName}";
        var ocrText = await _ocrService.RecognizeTextAsync(frame);

        if (string.IsNullOrWhiteSpace(ocrText))
        {
            CaptureStatusText = "OCR 未识别到文字";
            return;
        }

        if (string.Equals(_lastOcrText, ocrText, StringComparison.Ordinal))
        {
            CaptureStatusText = "OCR 文字未变化";
            return;
        }

        _lastOcrText = ocrText;
        SourceText = ocrText;

        if (!_isModelLoaded)
        {
            CaptureStatusText = "OCR 已更新英文文本，等待模型加载";
            return;
        }

        await TranslateOcrTextAsync(ocrText);
    }

    private async Task TranslateOcrTextAsync(string ocrText)
    {
        try
        {
            CaptureStatusText = "OCR 已更新，正在自动翻译";
            StatusText = "正在自动翻译...";
            TranslatedText = await _translationService.TranslateToChineseAsync(ocrText);
            StatusText = "自动翻译完成";
            CaptureStatusText = "OCR 文本已自动翻译";
        }
        catch (Exception ex)
        {
            StatusText = "自动翻译失败";
            TranslatedText = ExceptionFormatter.Format(ex);
            CaptureStatusText = "OCR 已更新，自动翻译失败";
        }
    }

    public void Dispose()
    {
        _captureTimer.Stop();
        if (_translationService is IDisposable disposable)
        {
            disposable.Dispose();
        }

        DisposeOcrService(_ocrService);
    }

    private void ReplaceOcrService(OcrBackend backend)
    {
        if (_isCapturing)
        {
            CaptureStatusText = "请先停止监控，再切换 OCR 后端";
            OnPropertyChanged(nameof(IsOcrBackendSelectionEnabled));
            return;
        }

        var previous = _ocrService;
        _ocrService = OcrServiceFactory.Create(backend);
        DisposeOcrService(previous);

        _lastFingerprint = null;
        _lastOcrText = string.Empty;
        CaptureStatusText = $"OCR 后端已切换：{SelectedOcrBackendChoice.DisplayName}";
    }

    private static void DisposeOcrService(IOcrService service)
    {
        if (service is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
