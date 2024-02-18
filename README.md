# Dataset Processor - All-in-one Tools
### [You don't need to Clone the repository or build to use it! Download in the releases page](https://github.com/Particle1904/DatasetHelpers/releases)
### [Please, check the Wiki for how to install and use the software!](https://github.com/Particle1904/DatasetHelpers/wiki)

Dataset Processor Tools is a versatile toolkit designed to streamline the processing of image-text datasets for machine learning applications. It empowers users with a range of powerful functionalities to enhance their datasets effortlessly.

Efficiently manage image datasets with Dataset Processor Tools. It supports editing both .txt and .caption files, allowing users to customize and fine-tune dataset annotations easily. Update tags, refine descriptions, and add contextual information with simplicity and seamless organization.

One standout feature is the automatic tag generation using the WD 1.4 SwinV2 Tagger V2 model. This pre-trained model analyzes image content and generates descriptive booru style tags, eliminating the need for manual tagging. Save time and enrich the dataset with detailed image descriptions.

The toolkit also offers advanced content-aware smart cropping. Leveraging the YoloV4 model for object detection, it intelligently identifies images with people and performs automatic cropping. Custom implementation ensures precise cropping, resulting in optimized images ready for further processing. Output dimensions are 512x512, 640x640, or 768x768, compatible with popular machine learning frameworks.

![Alt Text](https://github.com/Particle1904/DatasetHelpers/blob/master/showcase_gif2.gif?raw=true)

## Features
- Discard images with low resolution
- Resize images while preserving aspect ratio
- Generate tags using the updated WD 1.4 Tagger model (now using int8 instead of float32)
- Mass edit .txt files with tags
- Automatic Content Aware Crop for images with a person using YoloV4 model
- Editor page for editing .caption and .txt files, image navigation buttons, word highlight and more
- Mass replacing tags and renaming files in the "Process tags" tab

## Getting Started
To get started with the Dataset Processor Tools, download the provided release or build yourself.
To build clone this repository and open the project in Visual Studio 2022, Visual Studio Code with C# extensions or the terminal. You can then build and run the project.

Remember to download the model files: https://github.com/Particle1904/DatasetHelpers/releases/tag/v0.0.0 - follow the instructions in the release page to install them!

Use these commands to build it as a self-contained application:
In Visual Studio Community 2022; Right-click the DatasetProcessor.Desktop and click "Open in Terminal" then use the command:

FOR WINDOWS:
```dotnet build /restore /t:build /p:TargetFramework=net7.0 /p:Configuration=Release /p:Platform=x64 /p:PublishSingleFile=true /p:PublishTrimmed=false /p:RuntimeIdentifier=win-x64```

FOR LINUX:
```dotnet build /restore /t:build /p:TargetFramework=net7.0 /p:Configuration=Release /p:Platform=x64 /p:PublishSingleFile=true /p:PublishTrimmed=false /p:RuntimeIdentifier=linux-x64```

FOR MAC:
[Follow the instructions from this issue](https://github.com/Particle1904/DatasetHelpers/issues/6)

## Requirements
This software requires two runtimes:
- [.NET Desktop Runtime 7.0.4](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Visual C++ Redistributable for Visual Studio 2019 for running the Model](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)

## Usage
Dataset Processor Tools can be used to process image datasets for machine learning, allowing you to perform various tasks such as discarding images with low resolution, resizing images while preserving aspect ratio, generating tags using the updated WD 1.4 Tagger model, and mass editing .txt files with tags. The tools also include automatic content-aware cropping, advanced features in the Editor page, and options for mass replacing tags and renaming files.

## Technologies
- Dataset Processor Tools is built using .NET MAUI 7.0, an open-source and cross-platform UI framework for building native applications.
- ML.NET to run the WD 1.4 SwinV2 Tagger V2 Model.
- ML.NET to run the YoloV4 model for content aware automatic crop.

## Contributing
Contributions to the Dataset Processor Tools are welcome. If you would like to contribute, fork this repository, make your changes, and create a pull request.

## License
The Dataset Processor Tools is licensed under the MIT License. See the LICENSE file for more information.

## Acknowledgements
The Dataset Processor Tools use the pre-trained model [WD 1.4 SwinV2 Tagger V2 by SmilingWolf](https://huggingface.co/SmilingWolf/wd-v1-4-swinv2-tagger-v2), the pre-trained model [YoloV4](https://github.com/AlexeyAB/darknet) and is built with .NET MAUI 7.0, an open-source and cross-platform UI framework for building native applications.
