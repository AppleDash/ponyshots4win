PonyShots4Win
=============

This is a very simple Windows client for PonyShots, written in C# with .NET framework 4.5.

## Configuration

Upon first run, you will be presented with a dialog giving you the path of your configuration file. This must be edited with your PonyShots API URL, image root, username, and API key before use.

## Usage

At the moment, it simply runs in the background as a tray icon and allows you to press a few shortcuts to take screenshots in various ways:


* CTRL-SHIFT-2 - Current window
* CTRL-SHIFT-3 - Entire monitor
* CTRL-SHIFT-4 - Selected area
* CTRL-SHIFT-5 - Image in clipboard

The screenshot is then saved to `C:\Users\<your username>\My Pictures\PonyShots\` and uploaded to PonyShots.

## Known bugs
* Bad HiDPI/4K support
* Bad multimonitor support
* Upload notifications don't seem to work outside of VS debug mode

