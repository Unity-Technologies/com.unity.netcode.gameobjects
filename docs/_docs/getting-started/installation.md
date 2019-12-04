---
title: Installation
permalink: /wiki/installation/
---

### Installer
To get started with the MLAPI. You need to install the library. The easiest way is to use the Editor installer. Simply download the MLAPI_Installer Unity package from [here](https://github.com/MidLevel/MLAPI/releases), double click it to import it into Unity, and once that's done select Window > MLAPI from Unity's top menu bar. Once in the MLAPI window, select the version you wish to use and press install.


![Video showing the install process](https://i.imgur.com/zN63DlJ.gif)


Once imported into the Unity Engine, you will be able to use the components that it offers. To get started, you need a GameObject with the NetworkingManager component. Once you have that, use the Initializing the library articles to continue.


### Files
The MLAPI comes with 3 main components
##### MLAPI.dll
This DLL's is the runtime portion. The actual library. This file is thus **required**. It comes in two variants. A "Lite" and a normal version. Most people will do fine with the full version. The Lite version has less features. At the time of writing the only difference is that it does not include encryption which adds better support on certain platforms. Note that the lite version might not be as stable as the full version and could contain additional bugs.
##### MLAPI-Editor.unitypackage
This unitypackage includes the source files for all the Editor scripts. The UnityPackage will automatically place these source files in the Editor folder to avoid it being included in a build. **This is required**.
##### MLAPI-Installer.unitypackage
This unitypackage includes the source file for the installer. This component is totally optional. The Installer can help you manage versions. If you don't want to use the installer, you can simply place the MLAPI.dll and the Editor source files in your project and it will work just as well.
