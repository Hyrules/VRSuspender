# VRSuspender
Suspend / Kill Process that interfere with SteamVR

Work in progress. 

Require to run as administrator to modify the processes states.

Automatically detect SteamVR (vrserver.exe) and suspend process like iCUE, msedge, SamsungMagician, EK-Connect that causes spikes in
SteamVR.

Start this application and then start SteamVR. If you have any of theses process they will be suspended. They will automatically resume after you exit SteamVR.
You can either Suspend, Close, Kill the process.

Actions : 

- Close : Try to close the process gracefully but might not work if the app is programmed to go in the tray.
- Kill : Kill the process and will try it's best to restart it afterward.
- Suspend : Suspend the process. (you might see the window become greyed with at bar at the top)

TODO:
- Add button to manage process
- Add option to save process list

I do not offer any kind of garanty with this and user is solely responsible for using the application and situations that might arise.
I will not be responsible if you crash your computer because you kill or suspended a process you shouldn't have.
Make sure to investigate the processes before suspending or killing them.
