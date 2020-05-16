# UTheremin

Playable theremin in VRChat Udon, programmed in [UdonSharp](https://github.com/Merlin-san/UdonSharp).

The interesting class is `Assets/UTheremin/UTheremin.cs`. It synthesizes the waveform in realtime;
Unfortunately, udon computation is very slow, so the most you can synthesize is about 256 samples
every frame, so the max sampling rate for realtime audio is practically limited to <6000Hz.

# Cloning

You'll need to install the VRCSDK3 and UdonSharp unitypackages manually.
