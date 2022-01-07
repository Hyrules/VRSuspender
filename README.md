# VRSuspender
Suspend / Kill Process that interfere with SteamVR. 

Most of the time theses programs are softwares that monitor hardware onboard or control LEDs.

Already contains a few profiles :

iCue, EK-Connect, msedge, SamsungMagician.

More will come as people suggest them.

This app is currently a Work in progress. It might crash and you might find bugs.

Require to run as administrator to modify the processes states.

Automatically detect SteamVR (vrmonitor.exe) and suspend/kill process that causes spikes in SteamVR. You will have to specify the programs you want to close or suspend as the application does not detect this automatically.

How to use :

1. Start this application
2. Start monitoring for SteamVR 
3. Start SteamVR. 
 
If you have any monitored process in your list they will be suspended or killed. They will automatically resume after you exit SteamVR.
You can either Suspend, Kill or keep Running the process.

Actions : 

- Kill : Kill the process and will try it's best to restart it afterward. Note that this is not a gracefull kill and is recommended only when suspend is not possible or does not work properly.
- Suspend : Suspend the process. You might see the window become greyed with at bar at the top and become unresponsive this is normal behavior. You will not be able to use the application while it is suspended.
- Keep Running : You can also have the process keep running if you want.

Note: If you suspend a process with this app you won't be able to resume it from the task manager. There is no way in the task manager to suspend or resume process that has been suspended.

TODO:
- Add option to save process list

WARNING:
I do not offer any kind of warranty with this app and user is solely responsible for using the application and situations that might arise.
I will not be responsible if you crash your computer or someone else because you killed or suspended a process you shouldn't have.
Make sure to investigate the processes before suspending or killing them.
