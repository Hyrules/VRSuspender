# VRSuspender
Suspend / Kill Process that interfere with SteamVR. 

Most of the time theses programs are softwares that control LED like motherboard or keyboard and mouse.

Work in progress. 

Require to run as administrator to modify the processes states.

Automatically detect SteamVR (vrserver.exe) and suspend process that causes spikes in SteamVR. You will have to specify the programs you want to close or suspend as the application does not detect this automatically. Some known programs are already setup in the app but they might not all be there.

Start this application and then start SteamVR. If you have any of theses process they will be suspended. They will automatically resume after you exit SteamVR.
You can either Suspend, Kill or keep Running the process.

Actions : 

- Kill : Kill the process and will try it's best to restart it afterward.
- Suspend : Suspend the process. (you might see the window become greyed with at bar at the top and become unresponsive this is normal)

Note: If you suspend a process with this app you won't be able to resume it from the task manager. There is no way in the task manager to suspend or resume process that have been suspended

TODO:
- Add option to save process list

WARNING:
I do not offer any kind of warranty with this app and user is solely responsible for using the application and situations that might arise.
I will not be responsible if you crash your computer or someone else because you killed or suspended a process you shouldn't have.
Make sure to investigate the processes before suspending or killing them.
