# VRSuspender

![image](https://user-images.githubusercontent.com/3161577/148846714-8cc1e2a4-778b-4b62-85b3-c171eeb88904.png)

Suspend / Kill Process that interfere with SteamVR. 

Most of the time theses programs are softwares that monitor hardware onboard or control LEDs.

<b> Requirements </b>

Microsoft .Net 6.0 Desktop Runtimes

Download the Run Desktop Apps (I suggest you download both x86 and x64)

https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime

<b> Already contains a few profiles </b>

iCue, EK-Connect, msedge, SamsungMagician.

More will come as people suggest them.

This app is currently a work in progress. It might crash and you might find bugs.

Require to run as administrator to modify the processes states.

Automatically detect SteamVR (vrmonitor.exe) and suspend/kill process that causes spikes in SteamVR. You will have to specify the programs you want to close or suspend as the application does not detect this automatically.

<b> How to use : </b>

1. Download the latest release 
2. Start this application
3. Start monitoring for SteamVR 
4. Start SteamVR. 
 
If you have any monitored process in your list they will be suspended or killed. They will automatically resume after you exit SteamVR.
You can either Suspend, Kill or keep Running the process.

<b>Actions :</b>

- Kill : Kill the process and will try it's best to restart it afterward. Note that this is not a gracefull kill and is recommended only when suspend is not possible or does not work properly.
- Suspend : Suspend the process. You might see the window become greyed with at bar at the top and become unresponsive this is normal behavior. You will not be able to use the application while it is suspended.
- Keep Running : You can also have the process keep running if you want.

Note: If you suspend a process with this app you won't be able to resume it from the task manager. There is no way in the task manager to suspend or resume process that has been suspended.

<b>Licence : </b>

[Creative Commons Attribution Non-Commercial License V2.0](https://creativecommons.org/licenses/by-nc/2.0/)

The creator of this projet cannot be held responsible for any problem that might arise for the use of the software.

You are free to:

    Share — copy and redistribute the material in any medium or format
    Adapt — remix, transform, and build upon the material 

http://creativecommons.org/licenses/by-nc/2.0/ca/
