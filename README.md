
# Multimodal Hand Tracking for Extended Reality Musical Instruments (Unity Implementation)

## Overview

This repository contains the Unity implementation of the multimodal XR Hand Tracking system for Extended Reality Musical Instruments (XRMIs), as described in our research paper "Combining Vision and EMG-Based Hand Tracking for Extended Reality Musical Instruments". This system integrates vision-based hand tracking with a deep learning model based on EMG data to provide enhanced hand tracking in XR environments, specifically designed for musical applications.
This project consists of three parts:
1. This repo - the Unity implementation. It acts as a receiver of sEMG data from a Python application and is responsible for processing and rendering finger joint angle data.
2. A "Unity bridge" app for streaming sEMG data to Unity. This Python app receives sEMG data from the Myo armband, preprocesses it and sends it to Unity.
3. A Python project containing the training data and code that produces the deep learning model used in this project.

Note: The Unity bridge app for streaming sEMG data to Unity can be found [here](https://github.com/maxgraf96/sEMG-unity-bridge).

Note 2: The Python project for training the deep learning model can be found [here](https://github.com/maxgraf96/sEMG-myo-python).

## Features

- **Multimodal Hand Tracking**: Combines vision-based tracking with sEMG data for hand pose estimation in self-occlusion scenarios.
- **Real-Time Performance**: Built to run in real time.

## Getting Started

### Prerequisites

- Windows 10 (11 works, but there have been many reported issues with Oculus Link)
- Unity 2020.3 LTS or later
- Meta Quest 2, 3 or Pro
- Myo Armband

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/maxgraf96/myo-emg-vis.git
   ```

2. Open the project in Unity.

3. Configure your XR environment and sEMG hardware settings in the project.

### Usage

(Optional): If you want to train the model yourself follow the instructions to set up the [Python model project](https://github.com/maxgraf96/sEMG-myo-python).

- Set up the Python local sEMG streaming app (see https://github.com/maxgraf96/sEMG-unity-bridge).
- Open the `QuestPythonReceiverScene`.
- In the scene outline, select the `Scripts` GameObject and under the `ZMQ Server` MonoBehaviour set the Mode to `Inference`.
- Put on your Myo armband, connect it to your PC and run the Unity bridge app.
- Make sure your XR headset is connected to your PC and the Oculus app is open.
- Press play in the Unity Editor.

## Contributing

Contributions are welcome! If you want to extend the project fork it and send me a pull request.

## License

This project is licensed under the [MIT License](https://choosealicense.com/licenses/mit/).

## Citations

If you use this work, please cite
   ```
   @misc{graf2023combining,
      title={Combining Vision and EMG-Based Hand Tracking for Extended Reality Musical Instruments}, 
      author={Max Graf and Mathieu Barthet},
      year={2023},
      eprint={2307.10203},
      archivePrefix={arXiv},
      primaryClass={cs.CV}
  }
   ```
