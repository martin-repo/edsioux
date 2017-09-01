# edsioux
Elite: Dangerous - Statistical Information Overlay User Experience

## What is it

EdSioux will present statistical information as overlays while playing the game Elite: Dangerous. It monitors the journal files written to by the game to present new data in almost real-time.

<img src="https://github.com/mbedatpro/edsioux/raw/master/Images/online.png" width="128"> <img src="https://github.com/mbedatpro/edsioux/raw/master/Images/start-jump.png" width="128"> <img src="https://github.com/mbedatpro/edsioux/raw/master/Images/fsd-jump.png" width="128"> <img src="https://github.com/mbedatpro/edsioux/raw/master/Images/supercruise-exit.png" width="128"> <img src="https://github.com/mbedatpro/edsioux/raw/master/Images/docking-granted.png" width="128"> <img src="https://github.com/mbedatpro/edsioux/raw/master/Images/docked.png" width="128">

## Requirements

* Elite: Dangerous needs to run in windowed mode and full-screen
* Elite: Dangerous should run on the main display (if multi-display setup)
* Screen resolution of 1920x1080 is supported

## Install

1. Download the zip-file from the [latest release](https://github.com/mbedatpro/edsioux/releases/latest)
2. Unzip to where you want the program
3. Run EdSioux.exe

## Uninstall

1. Delete the folder where you unzipped the program
2. Delete the folder "%LOCALAPPDATA%\EdSioux"

## SiouxData

On first run, EdSioux will write two files into "%LOCALAPPDATA%\EdSioux":

* SiouxData.txt (this file will *NOT* be overwritten on subsequent runs)
* SiouxData.Default.txt (this file *WILL* be overwritten on subsequent runs)

In coming releases there will be a graphical editor for the SiouxData.txt file. But for now you can edit it manually.

Use the reference files for information (in the same folder):

* TokenReference.txt
* ColorReference.png

### Global settings

* allowAnonymousErrorFeedback = true to send anonymous feedback on journal parse errors
* filterOnCurrentCommander = true to filter statistics on current commander (if you've reset your commander at any time)
* defaultDisplayDuration = default time, in seconds, to display a message (can be overriden per event type)
* defaultTextColorType = default color of static text
* defaultTokenColorType = default color of tokens, ie. variables that will be replaced with statistics

### Event settings

Example event:
```
{
    "type": "Docked",
    "image": "docked-anchor.png",
    "format": "Docking #{count:friendly} at {stationName:name}",
}
```

* type = event type, see TokenReference.txt for all events
* image = (NOT YET SUPPORTED) 16x16px icon displayed left of event name
* format = text format

### Text format

Enter text that should be shown. Write tokens (that should be replaced) within curly brackets. See TokenReference.txt for a list of tokens that are available for all events as well as tokens available per event.

To override the color of a token, write the color name (see ColorReference.png for possible names) right of the token name, separated by a colon. Eg. "{tokenName:colorName}".

### Statistics calculation

The following will show the total number of deaths:
```
{
    "type": "Died",
    "format": "Died {count}",
}
```

The following will show the number of deaths in the current star system:
```
{
    "type": "Died",
    "format": "Died {count} in {starSystem}",
}
```

The following will show the number of deaths in the current star system by the specific enemy (in his/her current ship type) while in the current ship type:
```
{
    "type": "Died",
    "format": "Died {count} in {starSystem} while flying a {shipType} by {killerName} flying a {killerShip}",
}
```

The counts for these three examples might be very different. Experiment as you wish!

## Legal

MIT License

Copyright (c) 2017 Martin Amareld

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
