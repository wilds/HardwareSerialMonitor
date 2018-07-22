
    ____             _             ____  _        _
   / ___|_ __   __ _| |_          / ___|| |_ __ _| |_ ___
  | |  _| '_ \ / _` | __|  _____  \___ \| __/ _` | __/ __|
  | |_| | | | | (_| | |_  |_____|  ___) | || (_| | |_\__ \
   \____|_| |_|\__,_|\__|         |____/ \__\__,_|\__|___/

  tallmanlabs.com


  GNAT STATS PC performance monitor - Version 1+   Rupert Hirst & Colin Conway ©2016 http://tallmanlabs.com http://runawaybrainz.blogspot.com/

  Licence
  -------

  Attribution-NonCommercial-ShareAlike  CC BY-NC-SA

  This license lets others remix, tweak, and build upon your work non-commercially, as long as they credit you and license their new creations under the identical terms.
  
  https://creativecommons.org/licenses/

  

  Notes:

  I strongly suggest using this sketch with Atmel 32u4 based boards such as, the Leonardo or ProMicro, due to to its native USB support.

  The Windows "HardwareSerialMonitor" application uses the OpenHardwareMonitorLib.dll to detect the hardware.  http://openhardwaremonitor.org/
  The application will not detect "early" integrated graphics as a GPU!!!
  HardwareSerialMonitor does not like virtual Bluetooth COM ports present on the users PC!!!

  "Hardware Serial Monitor" is based on the Visual Studio project, kindly shared by psyrax see: https://github.com/psyrax/SerialMonitor


  ProMicro hookup:
  ----------------
  NeoPixel DataIn: D15 with 220r series resistor

  SSD1306 OLED Hookup:

  18.6mA on a fully lit 0.96" i2C OLED display, pull pin D5 High(5v) and D4 Low(to ground) on the ProMicro to providing the necessary  VCC / GND for the display.
  18mA is within the Atmel 32u4 maximum pin current limit of 20mA. This allows the display to be simply soldered straight to the header of the ProMicro.
  With different screens sizes and chipsets your mileage will vary, do your own tests!
  The 1.3" i2c OLED uses upward of 34mA, which is too much a I/O pin alone !!!

  Check your OLED PCB polarity!!!
    -------------------------------------------------------------
    0.96" i2C OLED:    VCC         GND          SCL        SDA

    ProMicro Pins:     D5(HIGH)    D4(LOW)      D3         D2
    -------------------------------------------------------------

    Version 1   :Initial release
    Version 1.1 : Fix intermittent screen flicker when in no activity mode "screen off" (due to inverter function?) fill the screen 128x64 black rectangle during this time.
    

   _    ___ ___ ___    _   ___ ___ ___ ___
  | |  |_ _| _ ) _ \  /_\ | _ \_ _| __/ __|
  | |__ | || _ \   / / _ \|   /| || _|\__ \
  |____|___|___/_|_\/_/ \_\_|_\___|___|___/

  Adafruit Neopixel
  https://github.com/adafruit/Adafruit_NeoPixel

  Adafruit SSD1306 library
  https://github.com/adafruit/Adafruit_SSD1306

  Adafruit library ported to the SH1106
  https://github.com/badzz/Adafruit_SH1106      Currently used library in this sketch!!!
 
  Adafruit GFX Library
  https://github.com/adafruit/Adafruit-GFX-Library

  HID-Project
  https://github.com/NicoHood/HID/wiki/Consumer-API

  IRremote
  https://github.com/z3t0/Arduino-IRremote
