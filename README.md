# Dataset Processor - All-in-one Tools
Dataset Processor Tools is a comprehensive set of tools designed for processing image datasets for machine learning. These tools provide various functionalities, including discarding images with low resolution, resizing images while preserving their aspect ratio, generating tags using a pre-trained model, mass editing .txt files with tags, and a simple editor to manually edit .txt files.

![Alt Text](https://github.com/LeonardoFer/DatasetHelpers/blob/master/showcase_gif.gif?raw=true)

## Features
- Discard images with low resolution
- Resize images while preserving aspect ratio
- Generate tags using the pre-trained WD 1.4 Tagger model
- Mass edit .txt files with tags
- Manually edit tags for individual files

## Getting Started
To get started with the Dataset Processor Tools, download the provided release or build yourself.
To build clone this repository and open the project in Visual Studio 2022 or later. You can then build and run the project.

Use these commands to build it as a self-contained .exe application (only windows supported at this time):
Right-click the Solution and click "Open in Terminal" then use the command:

```dotnet build /restore /t:build /p:TargetFramework=net7.0-windows10.0.19041.0 /p:configuration=release /p:WindowsAppSDKSelfContained=true /p:Platform=x64 /p:WindowsPackageType=None -p:BuildWindowsOnly=true```

## Requirements
This software requires two runtimes:
- [.NET Desktop Runtime 7.0.4](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Visual C++ Redistributable for Visual Studio 2019 for running the Model](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)

## Usage
Dataset Processor Tools can be used to process image datasets for machine learning, allowing you to perform various tasks, such as discarding images with low resolution, resizing images while preserving their aspect ratio, generating tags using a pre-trained model, and mass editing .txt files with tags.

## Technologies
- Dataset Processor Tools is built using .NET MAUI 7.0, an open-source and cross-platform UI framework for building native applications.
- ML.NET to run the WD 1.4 SwinV2 Tagger V2 Model.

## Contributing
Contributions to the Dataset Processor Tools are welcome. If you would like to contribute, fork this repository, make your changes, and create a pull request.

## License
The Dataset Processor Tools is licensed under the MIT License. See the LICENSE file for more information.

## Acknowledgements
The Dataset Processor Tools uses the pre-trained model [WD 1.4 SwinV2 Tagger V2 by SmilingWolf](https://huggingface.co/SmilingWolf/wd-v1-4-swinv2-tagger-v2) and is built with .NET MAUI 7.0, an open-source and cross-platform UI framework for building native applications.
