# POE2loadingPainFix
Path of Exile 2, CPU Limiting, to prevent PC full freeze while loaging maps

If you found your way here, this is what you get here:
<ul>
      <li>while loading a level in Path of Exile 2, your PC freezes complete</li>
      <li>only a hard reset is working</li>
      <li>you used other tools to limit the CPU while loading a level in POE2, but its anoying to set affinity(what logical processors are used by POE2) manually </li>
</ul>


Currently there is no patch from the game, or windows.
It occures on Amd ***X3D, Windows 11 24H2, and some other constellations

<h1>
$${\color{green}Here \space is \space your \space new \space friend \space : \space POE2loadingPainFix}$$
</h1>
<H2>this tool, sets affinity, and restores normal affinity <del>on disk/process disk load.</del> depending on entries of the POE2 logfile. On affinity set, you will have less FPS, to compensate this, the program tries to set affinity an go back to normal when its save </H2>

*functions explained on bottom*
<BR>
-----------------

Current Version: 0.2

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

<H1>Main Screen</H1>
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/1.png?raw=true" alt="mainscreen">
<H1>Functions:</H1>
<img src="https://github.com/CrimsonED1/POE2loadingPainFix/blob/main/README_Sources/images/auto_functions.png?raw=true" alt="autos">

<ul>
      <li>always on: affinity is always set to the selected processors</li>
      <li>always off: the program doesnt do any affinity</li>
      <li>auto-limit by POE2 Logfile: detects if a level is changed. And changes affinity back if loading is done</li>
      
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
