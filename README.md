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

Current Version: 0.9

<p>
<a href="https://github.com/CrimsonED1/POE2loadingPainFix/releases">
  Releases
</a>
</p>
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
<img src="https://github.com/user-attachments/assets/8afa7d58-3a56-47f7-872e-743497cc3df9">


<H1>Main Screen (Expert)</H1>

<img src="https://github.com/user-attachments/assets/00b450d9-29d4-4a40-a543-c5b1cc480390">
had no time yet...

<H1>Expert Functions:</H1>
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/auto_functions.png?raw=true" alt="autos">

<ul>
      <li>always on: limitations are always set </li>
      <li>always off: the program doesnt do any limitations</li>
      <li>auto-limit by POE2 Logfile: detects if a level is changed. And changes limitations back if loading is done</li>
      
</ul>

log file values to set limits:
<br>
"Delay: OFF"
<br>
"Got Instance Details from login server"
<br>
log file values to reset limit:
<br>
"Delay: ON"
<br>
<h3>other functions will be explained later, but i hope they are self explaining</h3>


Use this tool without any warranty or any liability by the authors
