using GameTranslate.Services;

namespace GameTranslate.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly ITranslationService _translationService;
    private readonly RelayCommand _loadModelCommand;
    private readonly RelayCommand _translateCommand;
    private string _modelPath;
    private string _sourceText = "hello, team. the boss is spawning near the bridge.";
    private string _translatedText = string.Empty;
    private string _statusText = "模型未加载";
    private bool _isModelLoaded;

    public MainViewModel()
        : this(new CudaLlamaTranslationService())
    {
    }

    public MainViewModel(ITranslationService translationService)
    {
        _translationService = translationService;
        _modelPath = ModelPathResolver.GetDefaultModelPath(AppContext.BaseDirectory);
        _loadModelCommand = new RelayCommand(LoadModelAsync, () => !_isModelLoaded);
        _translateCommand = new RelayCommand(TranslateAsync, () => _isModelLoaded && !string.IsNullOrWhiteSpace(SourceText));
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

    public RelayCommand LoadModelCommand => _loadModelCommand;

    public RelayCommand TranslateCommand => _translateCommand;

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
            TranslatedText = ex.Message;
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
            TranslatedText = ex.Message;
        }
    }

    public void Dispose()
    {
        if (_translationService is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

