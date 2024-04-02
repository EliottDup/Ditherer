# C0der23's Ultimate(ly bad) Dithering Program™
## Introduction
Hey! I made this program mostly for myself, originally written in python in a day an awfully slow, and wanted to make it faster by using unity compute shaders, I haven't worked with them a lot, but the performance gain is already incredible compared to python.

## Usage
I've tried making using the program as simple as possible a task as possible, and here's a step by step:

1. After opening the app, set the Load Path and Save Path to your desired location:
   If the Load Path is a single file, this file will be dithered.
   If it is a folder, every image in this folder will be dithered.

   The save Path is the folder where the dithered images are stored.
   **IF THE SAVE FOLDER IS THE SAME FOLDER AS THE FILE LOCATION, THE FILE WILL BE OVERWRITTEN**

2. Choose what type of Color palette you want to use:
   - auto: This setting will generate a number of colors based on an image, if the Save Path points to a single file, this image will be used, if it points to a folder, it will use the Palette Frame setting to determine which image to generate a palette from. Setting this setting to 0 uses the first image found.
   - custom: This setting allows you to use a custom palette, colors inputted should be in HEX format, begin with a #, and separated with commas, ex: `#3a3a3a, #f035a2`. Accepted formats are: #RGB, #RRGGBB, #RGBA, #RRGGBBAA

3. Generate the color palette.
   Pressing the "Generate Palette" button will initialize the palette using the chosen settings, and show a preview of the palette beneath it.

4. Run the ditherer
   Pressing the "Run Ditherer" button will start dithering your file(s) and save them to the save folder, with the same name as their original counterparts. **if no palette has been generated, a palette with two colors will be generated from the image first image**.

## The settings
| Setting | Description |
| ----------- | ----------- |
| Load Path | The path to the file/folder to dither |
| Save Path | The path to the folder where dithered images are saved|
| Palette Type | (`auto`/`custom`) `auto`: generates a palette from a given frame. `custom`: allows the user to specify colors. |
| Color Count | This setting is only used when Palette Type is set to `auto`, and specifies the amount of colors the generator will generate, setting many colors on a low-contrast image may result in wierd results. |
| Iteration Count | This setting is only used when Palette Type is set to `auto`, and specifies the amount of iterations taken by the palette generator to spread the colors as evenly as possible. |
| Palette Frame | This setting is only used when Palette Type is set to `auto` and the Load Path shows a folder. It specifies the image that will be used to generate the palette, if this number is zero, the first image will be used, if it is higher than the amount of images in the folder, the last image will be used.|
| Colors | This setting is only used when Palette Type is set to `custom`. It allows the user to enter custom colors. Colors must be in hexadecimal format, and follow one of the following formats: "#RGB, #RRGGBB, #RGBA, #RRGGBBAA", ex: `#3a3a3a, #f035a2, #000, #fafafaf0`|
| Matrix Size | The size of the dithering matrix applied to the image before dithering, where 1 results in no dithering at all and 2 gives very little dithering. **THE PROGRAM WILL NOT RUN IF THIS VALUE IS NOT A POWER OF 2 (1, 2, 4, 8...)**|

## Inner Workings

### The Palette Generator
The palette generator uses an image, and applies a [k-means clustering](https://en.wikipedia.org/wiki/K-means_clustering) algorithm to all the colors it uses, and iterates through it as many times as the Iteration Count setting, with the Color Count setting defining k. The colors are initiated randomly, and with each iteration, should approach one region of colors in the image with each iteration.

### The Ditherer
The ditherer uses an [ordered dithering](https://en.wikipedia.org/wiki/Ordered_dithering) algorithm, that makes use of a Bayer Matrix, the size of which is given by the Matrix Size setting.
Here are the Matrices for sizes 1, 2, 4 and 8

```math
\mathbf{M_0} = \frac{1}{1} \times
\begin{bmatrix}
0
\end{bmatrix}
```

```math
\mathbf{M_2} = \frac{1}{4} \times
\begin{bmatrix}
0 & 2 \\
3 & 1
\end{bmatrix}
```

```math
\mathbf{M_4} = \frac{1}{16} \times
\begin{bmatrix}
 0 &  8 &  2 & 10 \\
12 &  4 & 14 &  6 \\
 3 & 11 &  1 &  9 \\
15 &  7 & 13 &  5
\end{bmatrix}
```

```math
\mathbf{M_8} = \frac{1}{64} \times
\begin{bmatrix}
 0 & 32 &  8 & 40 &  2 & 34 & 10 & 42 \\
48 & 16 & 56 & 24 & 50 & 18 & 58 & 26 \\
12 & 44 &  4 & 36 & 14 & 46 &  6 & 38 \\
60 & 28 & 52 & 20 & 62 & 30 & 54 & 22 \\
 3 & 35 & 11 & 43 &  1 & 33 &  9 & 41 \\
51 & 19 & 59 & 27 & 49 & 17 & 57 & 25 \\
15 & 47 &  7 & 39 & 13 & 45 &  5 & 37 \\
63 & 31 & 55 & 23 & 61 & 29 & 53 & 21
\end{bmatrix}
```

This matrix is overlayed onto the image, every pixel's color is the "rounded" to the nearest color in the palette.
