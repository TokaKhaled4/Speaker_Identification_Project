# Speaker Identification System

This project implements a **Speaker Identification System** that identifies a person from their voice characteristics using Dynamic Time Warping (DTW) with and without pruning. It features a GUI for enrollment and identification.

## Problem Definition

The goal is to identify **who is speaking** (speaker ID), not **what is being said** (speech recognition), by analyzing acoustic features unique to each person’s voice. The system performs:

- **Enrollment**: Record a speaker’s voice, extract features, and store them as a template.
- **Identification**: Compare a new voice sample against enrolled templates to identify the speaker.

##  Features

**Graphical User Interface (GUI)**  
**Audio Recording, Saving, Loading, and Playback**  
**Silence Removal** to improve feature extraction  
**Feature Extraction** using MFCCs (Mel-Frequency Cepstral Coefficients)  
**Sequence Matching** with:
  - DTW without pruning 
  - DTW with pruning by limiting search paths  

## Development Environment

- **IDE**: Visual Studio 2019  
- **Language**: C#  
- **Framework**: .NET Framework 4.0  
- **MATLAB Compiler Runtime (MCR)**: 2012a or compatible version  

## How to Run

1. Install the **MATLAB Compiler Runtime (MCR)** (2012a version recommended).
2. Restart your computer.
3. Clone or download the project source code.
4. Open `Recorder.sln` in **Visual Studio 2019**.
5. Build the solution.
6. Launch the GUI

## Team Members

- Toka Khaled  
- Jana Essam  
- Jana Hani  
- Fatma Atef  
- Roaa Hussein  
- Rawan Mohamed  

