#include <Arduino.h>


#include "debug.h"
#include "PublishableState.h"
#include "MeterReader.h"
#include "WebServer.h"
#include "DewController.h"
PublishableState publishableState;

void setup() {

  #if DEBUG == true
    Serial.begin(9600);
    while (!Serial) {
      ; // wait for serial port to connect. Needed for native USB port only
    }
    DEBUG_SERIAL.println("Serial port is ready");
  #endif

  //discoverOneWireDevices();

  setupMeters();

  setupDewController();

  setupWebServer();
}

void loop() {

  readMeters(publishableState);

  dealWithDewController(publishableState);  

  dealWithHTTP(publishableState);
}

