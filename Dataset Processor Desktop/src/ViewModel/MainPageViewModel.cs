﻿using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;
using Dataset_Processor_Desktop.src.Views;

using SmartData.Lib.Interfaces;

using System.ComponentModel;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class MainPageViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IImageProcessorService _imageProcessorService;
        private readonly IAutoTaggerService _autoTaggerService;
        private readonly ITagProcessorService _tagProcessorService;
        private readonly ILoggerService _loggerService;

        #region Definition of App Views.
        private View _dynamicContentView;

        private View _welcomePage;
        private View _datasetSortView;
        private View _resizeImagesView;
        private View _tagGenerationView;
        private View _tagProcessingView;
        private View _tagEditorView;
        private View _settingsView;
        #endregion

        public string LatestLogMessage
        {
            get => _loggerService.LatestLogMessage;
        }

        public View DynamicContentView
        {
            get => _dynamicContentView;
            set
            {
                _dynamicContentView = value;
                OnPropertyChanged("DynamicContentView");
            }
        }

        public RelayCommand NavigateToWelcomePageCommand { get; private set; }
        public RelayCommand NavigateToDatasetSortCommand { get; private set; }
        public RelayCommand NavigateToResizeImagesCommand { get; private set; }
        public RelayCommand NavigateToTagGenerationCommand { get; private set; }
        public RelayCommand NavigateToTagProcessingCommand { get; private set; }
        public RelayCommand NavigateToTagEditorCommand { get; private set; }
        public RelayCommand NavigateToSettingsCommand { get; private set; }

        public MainPageViewModel()
        {
            _folderPickerService = Application.Current.Handler.MauiContext.Services.GetService<IFolderPickerService>();
            _fileManipulatorService = Application.Current.Handler.MauiContext.Services.GetService<IFileManipulatorService>();
            _imageProcessorService = Application.Current.Handler.MauiContext.Services.GetService<IImageProcessorService>();
            _autoTaggerService = Application.Current.Handler.MauiContext.Services.GetService<IAutoTaggerService>();
            _tagProcessorService = Application.Current.Handler.MauiContext.Services.GetService<ITagProcessorService>();
            _loggerService = Application.Current.Handler.MauiContext.Services.GetService<ILoggerService>();

            ((INotifyPropertyChanged)_loggerService).PropertyChanged += OnLoggerServicePropertyChanged;

            _welcomePage = new WelcomeView();
            _dynamicContentView = _welcomePage;
            NavigateToWelcomePageCommand = new RelayCommand(NavigateToWelcomeView);
            NavigateToDatasetSortCommand = new RelayCommand(NavigateToDatasetSortView);
            NavigateToResizeImagesCommand = new RelayCommand(NavigateToResizeImagesView);
            NavigateToTagGenerationCommand = new RelayCommand(NavigateToTagGenerationView);
            NavigateToTagProcessingCommand = new RelayCommand(NavigateToTagProcessingView);
            NavigateToTagEditorCommand = new RelayCommand(NavigateToTagEditorView);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsView);
        }

        public void NavigateToWelcomeView()
        {
            if (_welcomePage == null)
            {
                _welcomePage = new WelcomeView();
            }
            DynamicContentView = _welcomePage;
        }

        public void NavigateToDatasetSortView()
        {
            if (_datasetSortView == null)
            {
                _datasetSortView = new DatasetSortView(_folderPickerService, _fileManipulatorService, _loggerService);
            }
            DynamicContentView = _datasetSortView;
        }

        public void NavigateToResizeImagesView()
        {
            if (_resizeImagesView == null)
            {
                _resizeImagesView = new ResizeImagesView(_folderPickerService, _fileManipulatorService, _imageProcessorService, _loggerService);
            }
            DynamicContentView = _resizeImagesView;
        }

        public void NavigateToTagGenerationView()
        {
            if (_tagGenerationView == null)
            {
                _tagGenerationView = new TagGenerationView(_folderPickerService, _fileManipulatorService, _autoTaggerService, _loggerService);
            }
            DynamicContentView = _tagGenerationView;
        }

        public void NavigateToTagProcessingView()
        {
            if (_tagProcessingView == null)
            {
                _tagProcessingView = new TagProcessingView(_folderPickerService, _tagProcessorService, _fileManipulatorService, _loggerService);
            }
            DynamicContentView = _tagProcessingView;
        }

        public void NavigateToTagEditorView()
        {
            if (_tagEditorView == null)
            {
                _tagEditorView = new TagEditorView(_folderPickerService, _fileManipulatorService, _imageProcessorService, _loggerService);
            }
            DynamicContentView = _tagEditorView;
        }

        public void NavigateToSettingsView()
        {
            if (_settingsView == null)
            {
                _settingsView = new SettingsView();
            }
            DynamicContentView = _settingsView;
        }

        private void OnLoggerServicePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ILoggerService.LatestLogMessage))
            {
                OnPropertyChanged(nameof(LatestLogMessage));
            }
        }
    }
}