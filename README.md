# POE2loadingPainFix
Path of Exile 2, CPU Limiting, to prevent PC full freeze while loaging maps

If you found your way here, this is what you get here:
<ul>
      <li>while loading a level in Path of Exile 2, your PC freezes complete</li>
      <li>only a hard reset is working</li>
      <li>you used other tools to limit the CPU while loading a level in POE2, but its anoying to set affinity (what logical processors are used by POE2) or CPU limiting like 
            <a href="https://mion.yosei.fi/BES/">BES – Battle Encoder Shirasé</a>
  manually </li>
</ul>


Currently there is no patch from the game, or windows.
It occures on Amd ***X3D, Windows 11 24H2, and some other constellations

<h1>
$${\color{green}Here \space is \space your \space new \space friend \space : \space POE2loadingPainFix}$$
</h1>

<H2>
      This App sets "Limiting" to the POE2 Process.            
</H2>
<H3>Depending on entries of the POE2 logfile. Limit is only done while starting POE2 and loading levels</H3>
<H4>Limiting Options:</H4>
<ul>
      <li>Limiting threads. This is mostly like 
            <a href="https://mion.yosei.fi/BES/">BES – Battle Encoder Shirasé</a> is doing. Getting every thread of a process, then suspend it, wait, then resume it (since V0.9 and now default). </li>
      <li>Limiting via affinity (not recommended)</li>
</ul>

<H3>Supported versions:</H3>
<ul>
      <li>POE2-Steam</li>
      <li>POE2-Standalone</li>
</ul>


*functions explained on bottom*
<BR>
-----------------

<H1>Current Version: 0.9
 .</H1>

<h4>Current TODO / upcoming features:</h4>
<ul>
      <li>add Elegato StreamDeck support</li>
      <li>verify thread limiting with BES</li>
</ul>


<H1>
<a href="https://github.com/CrimsonED1/POE2loadingPainFix/releases">
  Releases
</a>
            </H1>
<br><br><br>
<p>
  <a href="https://www.paypal.me/crimsoned">
      <img src="https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif" alt="paypal">
  </a>
</p>
(if you like it, maybe you wanne support this project)
<br><br>

<H1>Easy/Expert</H1>
<img src="https://github.com/user-attachments/assets/56dc8b14-e44f-48c9-b79e-a3d16466946f">
<H2>Easy is designed to users that are not well known with computers and the background this app does. NOTE! Switching from expert to easy will restore default settings!</H2>

<H1>Main Screen (Easy Mode)</H1>
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/EasyMode.png?raw=true">

<H1>Main Screen (Expert)</H1>

<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/ExpertMode.png?raw=true">

<H1>Expert Functions:</H1>
<H2>Choose limit mode (on/off/auto)</H2>
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/auto_functions.png?raw=true">

<ul>
      <li>always on: limitations are always set </li>
      <li>always off: the program doesnt do any limitations</li>
      <li>auto-limit by POE2 Logfile: detects if a level is changed. And changes limitations back if loading is done</li>
      
</ul>

<H2>Auto Mode: Log file values to set limits:</H2>
<ul>
      <li>"Got Instance Details from login server" => set limits</li>
      <li>"Delay: ON" => set limits</li>
      <li>"Delay: OFF" => reset limits</li>
</ul>
<br>

<H2>Limit Settings: (kinds are thread limiting and affinity)</H2>
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/limit_settings.png?raw=true">
<H3>Delay reset limit to normal, affects all kinds of limiting</H3>

<H3>Limit threads, Run/Pause</H3>
The limitation for POE2 threads looks like this:
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/signal_limit_threads.png?raw=true">
the app tries to open the threads of POE2 every cylce to suspend/resume them. This will slow down the thread, and the CPU usage. 
You can affect the duration of run and pause with the trackbars to set less or high cpu usage in this way. (take a look at the signal image)

Technical background:
There are several different "OpenThread-Versions". I tried implementing all of BES, but im not sure that i done them all right (BES is written in c++). I will try to compile and run BES theese days and check if the limits are equal with this app. Currently on my system only the main-thread is possible to be opend of POE2. But this seems to be enougth to get no hard freezes.
The graphs and Label will show you how many threads POE2 have, how many are active, and how many could be limited.


<br>
<H1>Bugs/Exceptions</H1>
If you get and Exception, or find any issue, please report them.
It is very helpfull if you include the report from the exception dialog:
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/exception_dialog.png?raw=true">

<br>


<h3>other functions will be explained later, but i hope they are self explaining</h3>



Use this tool without any warranty or any liability by the authors
