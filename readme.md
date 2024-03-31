# C0der23's Ultimate(ly bad) Dithering Program™
## Introduction
Hey! I made this program mostly for myself, originally written in python in a day an awfully slow, and wanted to make it faster by using unity compute shaders, I haven't worked with them a lot, but the performance gain is already incredible compared to python.

## Usage
I've tried making using the program as simple as possible a task as possible, and here's a step by step:

1. After opening the app, set the Load Path and Save Path to your desired location:
   If the Load Path is a single file, this file will be dithered.
   If it is a folder, every image in this folder will be dithered.

   The save Path is the folder where the dithered images are stored.
   **IF THE SAVE FOLDER IS THE SAME FOLDER AS THE FILE LOCATION, THE FILL WILL BE OVERWRITTEN**

2. Choose what type of Color palette you want to use:
   - auto: This setting will generate a number of colors based on an image, if the Save Path points to a single file, this image will be used, if it points to a folder, it will use the Palette Frame setting to determine which image to generate a palette from. Setting this setting to 0 uses the first image found.
   - custom: This setting allows you to use a custom palette, colors inputted should be in HEX format, begin with a #, and separated with commas, ex: `#3a3a3a, #f035a2`. Accepted formats are: #RGB, #RRGGBB, #RGBA, #RRGGBBAA

3. Generate the color palette.
   Pressing the "Generate Palette" button will initialize the palette using the chosen settings, and show a preview of the palette beneath it.

## The settings



## How does it work?
