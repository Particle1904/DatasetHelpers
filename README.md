# Dataset Processor - All-in-one Tools
### [You don't need to Clone the repository or build to use it! Download in the releases page](https://github.com/Particle1904/DatasetHelpers/releases)
### [Please, check the Wiki for how to install and use the software!](https://github.com/Particle1904/DatasetHelpers/wiki)

Dataset Processor Tools is a versatile toolkit designed to streamline the processing of image-text datasets for machine learning applications. It empowers users with a range of powerful functionalities to enhance their datasets effortlessly.

Efficiently manage image datasets with Dataset Processor Tools. It supports editing both .txt and .caption files, allowing users to customize and fine-tune dataset annotations easily. Update tags, refine descriptions, and add contextual information with simplicity and seamless organization.

One standout feature is the automatic tag generation using the WD 1.4 SwinV2 Tagger V2 model. This pre-trained model analyzes image content and generates descriptive booru style tags, eliminating the need for manual tagging. Save time and enrich the dataset with detailed image descriptions.

The toolkit also offers advanced content-aware smart cropping. Leveraging the YoloV4 model for object detection, it intelligently identifies images with people and performs automatic cropping. Custom implementation ensures precise cropping, resulting in optimized images ready for further processing. Output dimensions are 512x512, 640x640, or 768x768, compatible with popular machine learning frameworks.

![Alt Text](https://github.com/Particle1904/DatasetHelpers/blob/master/showcase_gif2.gif?raw=true)

## Features
- Gallery Viewer: Easily select and discard multiple images at once.
- Bulk Image Processing: Discard low-resolution images or create backups of datasets effortlessly.
- Automatic Content-Aware Crop: Utilize the YoloV4 model to automatically crop images with people.
- Manual Crop: Drag and drop a rectangle over images to manually define cropping areas.
- Image Resizing: Resize images while maintaining aspect ratio, with the option to conditionally apply sharpening to downscaled images.
- AI-based Auto Tagging: Generate tags using four different AI models, append outputs to existing .txt files, enabling tag generation with multiple models.
- Text File Processing: Mass process .txt files, including options to add, remove, replace, or emphasize tags, rename files, remove redundancy, and consolidate similar tags.
- Text Editor: Edit .caption and .txt files with image navigation buttons, word highlighting, keyword filtering, and keyboard shortcuts (see the [Wiki](https://github.com/Particle1904/DatasetHelpers/wiki/Editor-Page) for keyboard shortcuts).
- Subset Extraction: Extract a subset of a larger dataset by searching for keywords/tags in .txt and/or .caption files.
- Prompt Generation: Generate prompts for model testing using tags/keywords found in the dataset.
- Metadata Viewer: View metadata for .png files (Windows only due to Linux distribution differences in Drag and Drop operations).

## Getting Started
To get started with the Dataset Processor Tools, [download the latest provided release](https://github.com/Particle1904/DatasetHelpers/releases) or build yourself.
To build clone this repository and open the project in Visual Studio 2022, Visual Studio Code with C# extensions or the terminal. You can then build and run the project.

Remember to download the model files: https://github.com/Particle1904/DatasetHelpers/releases/tag/v0.0.0 - follow the instructions in the release page to install them!

Use these commands to build it as a self-contained application:
In Visual Studio Community 2022; Right-click the DatasetProcessorDesktop and click "Open in Terminal" then use the command:

FOR WINDOWS x64: ```dotnet build /restore /t:build /p:TargetFramework=net8.0 /p:Configuration=Release /p:Platform=x64 /p:PublishSingleFile=true /p:PublishTrimmed=false /p:RuntimeIdentifier=win-x64```

FOR WINDOWS x86 (GPU): ```dotnet build /restore /t:build /p:TargetFramework=net8.0 /p:Configuration=Release /p:Platform=x86 /p:PublishSingleFile=true /p:PublishTrimmed=false /p:RuntimeIdentifier=win-x86```

FOR LINUX:
```dotnet build /restore /t:build /p:TargetFramework=net8.0 /p:Configuration=Release /p:Platform=x64 /p:PublishSingleFile=true /p:PublishTrimmed=false /p:RuntimeIdentifier=linux-x64```

FOR MAC:
[Follow the instructions from this issue](https://github.com/Particle1904/DatasetHelpers/issues/6)

## Requirements
This software requires two runtimes:
- [.NET Desktop Runtime 8 or newer](https://dotnet.microsoft.com/pt-br/download/dotnet/8.0)
- [Visual C++ Redistributable for Visual Studio 2019 for running the Model](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)

## Technologies
- Dataset Processor Tools is built using Avalonia in its second version, an open-source and cross-platform UI framework for building native cross platform applications.
- ML.NET to run the WD 1.4 SwinV2 v2, WD SwinV2 v3, JoyTag, Z3DE621 AI Models.
- ML.NET to run the YoloV4 model for content aware automatic crop.
- Dataset Processor Tools was built using .NET MAUI 7.0 in its first version (but it was abandoned in favor of Avalonia for better cross-platform support), an open-source and cross-platform UI framework for building native applications.

## Contributing
Contributions to the Dataset Processor Tools are welcome. If you would like to contribute, fork this repository, make your changes, and create a pull request.

## License
The Dataset Processor Tools is licensed under the MIT License. See the LICENSE file for more information.

## Acknowledgements
The Dataset Processor Tools use the pre-trained model [WD 1.4 SwinV2 Tagger V2 by SmilingWolf](https://huggingface.co/SmilingWolf/wd-v1-4-swinv2-tagger-v2), the pre-trained model [YoloV4](https://github.com/AlexeyAB/darknet) and is built with [Avalonia](https://avaloniaui.net), an open-source and cross-platform UI framework for building native applications.
